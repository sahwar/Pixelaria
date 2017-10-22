﻿/*
    Pixelaria
    Copyright (C) 2013 Luiz Fernando Silva

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.

    The full license may be found on the License.txt file attached to the
    base directory of this project.
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;

using SharpDX;
using SharpDX.Direct2D1;

using JetBrains.Annotations;

using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Bitmap = System.Drawing.Bitmap;
using Color = System.Drawing.Color;
using CombineMode = SharpDX.Direct2D1.CombineMode;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

using Pixelaria.Utils;
using Pixelaria.Views.ModelViews.PipelineView;
using SharpDX.DXGI;
using AlphaMode = SharpDX.Direct2D1.AlphaMode;
using PixelFormat = SharpDX.Direct2D1.PixelFormat;
using RectangleF = System.Drawing.RectangleF;

namespace Pixelaria.Views.ModelViews
{
    public partial class ExportPipelineControl
    {
        /// <summary>
        /// Renders a pipeline export view
        /// </summary>
        public sealed class Direct2DRenderer : IDisposable, IDirect2DRenderer
        {
            private readonly Dictionary<string, SharpDX.Direct2D1.Bitmap> _bitmapResources = new Dictionary<string, SharpDX.Direct2D1.Bitmap>();

            /// <summary>
            /// For relative position calculations
            /// </summary>
            private readonly IPipelineContainer _container;

            private readonly Control _control;

            private readonly List<IRenderingDecorator> _decorators = new List<IRenderingDecorator>();

            /// <summary>
            /// List of decorators that is removed after paint operations complete
            /// </summary>
            private readonly List<IRenderingDecorator> _temporaryDecorators = new List<IRenderingDecorator>();

            /// <summary>
            /// For rendering title of pipeline nodes
            /// </summary>
            private TextFormat _nodeTitlesTextFormat;

            /// <summary>
            /// Control-space clip rectangle for current draw operation.
            /// </summary>
            private Rectangle ClipRectangle { get; set; }

            public Direct2DRenderer(IPipelineContainer container, Control control)
            {
                _container = container;
                _control = control;
            }

            public void Dispose()
            {
                _nodeTitlesTextFormat.Dispose();
            }

            public void Initialize([NotNull] Direct2DRenderingState state)
            {
                _nodeTitlesTextFormat = new TextFormat(state.DirectWriteFactory, "Microsoft Sans Serif", 11)
                {
                    TextAlignment = TextAlignment.Leading,
                    ParagraphAlignment = ParagraphAlignment.Center
                };
            }

            #region Image Resources
            
            public void AddImageResource(Direct2DRenderingState state, Bitmap bitmap, string resourceName)
            {
                if(_bitmapResources.ContainsKey(resourceName))
                    throw new ArgumentException($@"An image resource named '{resourceName}' already exists.", nameof(resourceName));

                _bitmapResources[resourceName] = CreateSharpDxBitmap(state.D2DRenderTarget, bitmap);
            }

            public void RemoveImageResources()
            {
                foreach (var value in _bitmapResources.Values)
                {
                    value.Dispose();
                }

                _bitmapResources.Clear();
            }

            public void RemoveImageResource(string resourceName)
            {
                if (_bitmapResources.ContainsKey(resourceName))
                {
                    _bitmapResources.Remove(resourceName);
                }
            }

            public PipelineNodeView.ImageResource AddPipelineNodeImageResource(Direct2DRenderingState state, Bitmap bitmap, string resourceName)
            {
                var res = new PipelineNodeView.ImageResource(resourceName, bitmap.Width, bitmap.Height);

                AddImageResource(state, bitmap, resourceName);

                return res;
            }

            public PipelineNodeView.ImageResource? PipelineNodeImageResource(string resourceName)
            {
                var res = ImageResource(resourceName);
                if (res != null)
                    return new PipelineNodeView.ImageResource(resourceName, res.PixelSize.Width, res.PixelSize.Height);

                return null;
            }
            
            [CanBeNull]
            private SharpDX.Direct2D1.Bitmap ImageResource([NotNull] string named)
            {
                return _bitmapResources.TryGetValue(named, out SharpDX.Direct2D1.Bitmap bitmap) ? bitmap : null;
            }

            #endregion

            public void Render([NotNull] Direct2DRenderingState state)
            {
                var decorators = _decorators.Concat(_temporaryDecorators).ToList();

                ClipRectangle = new Rectangle(Point.Empty, _control.Size);

                foreach (var nodeView in _container.NodeViews)
                {
                    RenderStepView(nodeView, state, _decorators.ToArray());
                }

                // Draw background across visible region
                RenderBackground(state);
                
                // Render bezier paths
                var labels = _container.Root.Children.OfType<LabelView>().ToArray();
                var beziers = _container.Root.Children.OfType<BezierPathView>().ToArray();
                var beziersLow = beziers.Where(b => !b.RenderOnTop);
                var beziersOver = beziers.Where(b => b.RenderOnTop);
                foreach (var bezier in beziersLow)
                {
                    RenderBezierView(bezier, state, decorators.ToArray());
                }

                foreach (var stepView in _container.NodeViews)
                {
                    RenderStepView(stepView, state, decorators.ToArray());
                }

                foreach (var bezier in beziersOver)
                {
                    RenderBezierView(bezier, state, decorators.ToArray());
                }

                foreach (var label in labels.Where(l => l.Visible))
                {
                    RenderLabelView(label, state, decorators.ToArray());
                }
            }

            public void AddDecorator(IRenderingDecorator decorator)
            {
                _decorators.Add(decorator);
            }

            public void RemoveDecorator(IRenderingDecorator decorator)
            {
                _decorators.Remove(decorator);
            }

            public void PushTemporaryDecorator(IRenderingDecorator decorator)
            {
                _temporaryDecorators.Add(decorator);
            }

            private void RenderStepView([NotNull] PipelineNodeView nodeView, [NotNull] Direct2DRenderingState state, [ItemNotNull, NotNull] IRenderingDecorator[] decorators)
            {
                state.PushingTransform(() =>
                {
                    state.D2DRenderTarget.Transform = new Matrix3x2(nodeView.GetAbsoluteTransform().Elements);
                    
                    var visibleArea = nodeView.GetFullBounds().Corners.Transform(nodeView.GetAbsoluteTransform()).Area();
                    
                    if (!ClipRectangle.IntersectsWith((Rectangle)visibleArea))
                        return;
                    
                    // Create rendering states for decorators
                    var stepViewState = new PipelineStepViewState
                    {
                        FillColor = nodeView.Color,
                        TitleFillColor = nodeView.Color.Fade(Color.Black, 0.8f),
                        StrokeColor = nodeView.StrokeColor,
                        StrokeWidth = nodeView.StrokeWidth,
                        FontColor = Color.White
                    };

                    // Decorate
                    foreach (var decorator in decorators)
                        decorator.DecoratePipelineStep(nodeView, ref stepViewState);

                    var bounds = nodeView.Bounds;

                    var roundedRectArea = new RoundedRectangleGeometry(state.D2DFactory,
                        new RoundedRectangle
                        {
                            RadiusX = 5,
                            RadiusY = 5,
                            Rect = new RawRectangleF(0, 0, bounds.Width, bounds.Height)
                        });
                        
                    // Draw body fill
                    using (var stopCollection = new GradientStopCollection(state.D2DRenderTarget, new[]
                    {
                        new GradientStop {Color = stepViewState.FillColor.ToColor4(), Position = 0},
                        new GradientStop {Color = stepViewState.FillColor.Fade(Color.Black, 0.1f).ToColor4(), Position = 1}
                    }))
                    using (var gradientBrush = new LinearGradientBrush(
                        state.D2DRenderTarget,
                        new LinearGradientBrushProperties
                        {
                            StartPoint = new RawVector2(0, 0),
                            EndPoint = new RawVector2(0, bounds.Height)
                        },
                        stopCollection))

                    {
                        state.D2DRenderTarget.FillGeometry(roundedRectArea, gradientBrush);
                    }

                    var titleArea = nodeView.GetTitleArea();
                    
                    using (var clipPath = new PathGeometry(state.D2DFactory))
                    {
                        var sink = clipPath.Open();

                        var titleRect = new RawRectangleF(0, 0, titleArea.Width, titleArea.Height);
                        var titleClip = new RectangleGeometry(state.D2DFactory, titleRect);
                        titleClip.Combine(roundedRectArea, CombineMode.Intersect, sink);

                        sink.Close();
                        sink.Dispose();

                        titleClip.Dispose();
                            
                        // Fill BG
                        using (var solidColorBrush = new SolidColorBrush(state.D2DRenderTarget, stepViewState.TitleFillColor.ToColor4()))
                        {
                            state.D2DRenderTarget.FillGeometry(clipPath, solidColorBrush);
                        }
                            
                        int titleX = 4;

                        // Draw icon, if available
                        if (nodeView.Icon != null)
                        {
                            var icon = nodeView.Icon.Value;

                            titleX += icon.Width + 5;
                                
                            float imgY = titleArea.Height / 2 - (float)icon.Height / 2;

                            var imgBounds = (AABB)new RectangleF(imgY, imgY, icon.Width, icon.Height);

                            var bitmap = ImageResource(icon.ResourceName);
                            if(bitmap != null)
                            {
                                var mode = BitmapInterpolationMode.Linear;

                                // Draw with high quality only when zoomed out
                                if (new AABB(Vector.Zero, Vector.Unit).TransformedBounds(_container.Root.LocalTransform).Size >=
                                    Vector.Unit)
                                {
                                    mode = BitmapInterpolationMode.NearestNeighbor;
                                }

                                state.D2DRenderTarget.DrawBitmap(bitmap, imgBounds, 1f, mode);
                            }
                        }

                        using (var textLayout = new TextLayout(state.DirectWriteFactory, nodeView.Name, _nodeTitlesTextFormat, titleArea.Width, titleArea.Height))
                        using (var whiteBrush = new SolidColorBrush(state.D2DRenderTarget, stepViewState.FontColor.ToColor4()))
                        {
                            state.D2DRenderTarget.DrawTextLayout(new RawVector2(titleX, 0), textLayout, whiteBrush, DrawTextOptions.EnableColorFont);
                        }
                    }

                    // Draw outline now
                    using (var penBrush = new SolidColorBrush(state.D2DRenderTarget, stepViewState.StrokeColor.ToColor4()))
                    {
                        state.D2DRenderTarget.DrawGeometry(roundedRectArea, penBrush, stepViewState.StrokeWidth);
                    }
    
                    roundedRectArea.Dispose();

                    // Draw in-going and out-going links
                    var inLinks = nodeView.GetInputViews();
                    var outLinks = nodeView.GetOutputViews();

                    // Draw inputs
                    foreach (var link in inLinks)
                    {
                        state.PushingTransform(() =>
                        {
                            state.D2DRenderTarget.Transform = new Matrix3x2(link.GetAbsoluteTransform().Elements);

                            var rectangle = link.Bounds;

                            var linkState = new PipelineStepViewLinkState
                            {
                                FillColor = Color.White,
                                StrokeColor = link.StrokeColor,
                                StrokeWidth = link.StrokeWidth
                            };

                            // Decorate
                            foreach (var decorator in decorators)
                                decorator.DecoratePipelineStepInput(nodeView, link, ref linkState);
                            
                            using (var pen = new SolidColorBrush(state.D2DRenderTarget, linkState.StrokeColor.ToColor4()))
                            using (var brush = new SolidColorBrush(state.D2DRenderTarget, linkState.FillColor.ToColor4()))
                            {
                                state.D2DRenderTarget.FillRectangle(rectangle, brush);
                                state.D2DRenderTarget.DrawRectangle(rectangle, pen, linkState.StrokeWidth);
                            }
                        });
                    }

                    // Draw outputs
                    foreach (var link in outLinks)
                    {
                        state.PushingTransform(() =>
                        {
                            state.D2DRenderTarget.Transform = new Matrix3x2(link.GetAbsoluteTransform().Elements);

                            var rectangle = link.Bounds;

                            var linkState = new PipelineStepViewLinkState
                            {
                                FillColor = Color.White,
                                StrokeColor = link.StrokeColor,
                                StrokeWidth = link.StrokeWidth
                            };

                            // Decorate
                            foreach (var decorator in decorators)
                                decorator.DecoratePipelineStepOutput(nodeView, link, ref linkState);
                                
                            using (var pen = new SolidColorBrush(state.D2DRenderTarget, linkState.StrokeColor.ToColor4()))
                            using (var brush = new SolidColorBrush(state.D2DRenderTarget, linkState.FillColor.ToColor4()))
                            {
                                state.D2DRenderTarget.FillRectangle(rectangle, brush);
                                state.D2DRenderTarget.DrawRectangle(rectangle, pen, linkState.StrokeWidth);
                            }
                        });
                    }
                });
            }
            
            private void RenderBezierView([NotNull] BezierPathView bezierView, [NotNull] Direct2DRenderingState renderingState, [ItemNotNull, NotNull] IRenderingDecorator[] decorators)
            {
                renderingState.PushingTransform(() =>
                {
                    renderingState.D2DRenderTarget.Transform = new Matrix3x2(bezierView.GetAbsoluteTransform().Elements);
                    
                    var visibleArea = bezierView.GetFullBounds().Corners.Transform(bezierView.GetAbsoluteTransform()).Area();

                    if (!ClipRectangle.IntersectsWith((Rectangle)visibleArea))
                        return;
                    
                    var state = new BezierPathViewState
                    {
                        StrokeColor = bezierView.StrokeColor,
                        StrokeWidth = bezierView.StrokeWidth,
                        FillColor = bezierView.FillColor
                    };
                        
                    var geom = new PathGeometry(renderingState.D2DRenderTarget.Factory);
                        
                    var sink = geom.Open();
                    
                    foreach (var input in bezierView.GetPathInputs())
                    {
                        if (input is BezierPathView.RectanglePathInput recInput)
                        {
                            var rec = recInput.Rectangle;

                            sink.BeginFigure(rec.Minimum, FigureBegin.Filled);
                            sink.AddLine(new Vector(rec.Right, rec.Top));
                            sink.AddLine(new Vector(rec.Right, rec.Bottom));
                            sink.AddLine(new Vector(rec.Left, rec.Bottom));
                            sink.EndFigure(FigureEnd.Closed);
                        }
                        else if (input is BezierPathView.BezierPathInput bezInput)
                        {
                            sink.BeginFigure(bezInput.Start, FigureBegin.Filled);

                            sink.AddBezier(new BezierSegment
                            {
                                Point1 = bezInput.ControlPoint1,
                                Point2 = bezInput.ControlPoint2,
                                Point3 = bezInput.End
                            });

                            sink.EndFigure(FigureEnd.Open);
                        }
                    }

                    sink.Close();

                    // Decorate
                    foreach (var decorator in decorators)
                        decorator.DecorateBezierPathView(bezierView, ref state);

                    if (state.FillColor != Color.Transparent)
                    {
                        using (var brush = new SolidColorBrush(renderingState.D2DRenderTarget, state.FillColor.ToColor4()))
                        {
                            renderingState.D2DRenderTarget.FillGeometry(geom, brush);
                        }
                    }

                    using (var brush = new SolidColorBrush(renderingState.D2DRenderTarget, state.StrokeColor.ToColor4()))
                    {
                        renderingState.D2DRenderTarget.DrawGeometry(geom, brush, state.StrokeWidth);
                    }
                    
                    sink.Dispose();
                    geom.Dispose();
                });
            }

            private void RenderLabelView([NotNull] LabelView labelView, [NotNull] Direct2DRenderingState renderingState, [ItemNotNull, NotNull] IRenderingDecorator[] decorators)
            {
                renderingState.PushingTransform(() =>
                {
                    renderingState.D2DRenderTarget.Transform = new Matrix3x2(labelView.GetAbsoluteTransform().Elements);

                    var visibleArea =
                        labelView
                            .GetFullBounds().Corners
                            .Transform(labelView.GetAbsoluteTransform()).Area();

                    if (!ClipRectangle.IntersectsWith((Rectangle)visibleArea))
                        return;

                    var state = new LabelViewState
                    {
                        StrokeColor = labelView.StrokeColor,
                        StrokeWidth = labelView.StrokeWidth,
                        TextColor = labelView.TextColor,
                        BackgroundColor = labelView.BackgroundColor
                    };

                    // Decorate
                    foreach (var decorator in decorators)
                        decorator.DecorateLabelView(labelView, ref state);

                    var roundedRect = new RoundedRectangle
                    {
                        RadiusX = 5, RadiusY = 5,
                        Rect = new RawRectangleF(0, 0, labelView.Bounds.Width, labelView.Bounds.Height)
                    };

                    using (var pen = new SolidColorBrush(renderingState.D2DRenderTarget, state.StrokeColor.ToColor4()))
                    using (var brush = new SolidColorBrush(renderingState.D2DRenderTarget, state.BackgroundColor.ToColor4()))
                    {
                        renderingState.D2DRenderTarget.FillRoundedRectangle(roundedRect, brush);
                        renderingState.D2DRenderTarget.DrawRoundedRectangle(roundedRect, pen);
                    }

                    var textBounds = labelView.TextBounds;

                    if (state.TextColor != Color.Transparent)
                    {
                        using (var brush = new SolidColorBrush(renderingState.D2DRenderTarget, state.TextColor.ToColor4()))
                        using (var textFormat = new TextFormat(renderingState.DirectWriteFactory, labelView.TextFont.Name, labelView.TextFont.SizeInPoints) { TextAlignment = TextAlignment.Leading, ParagraphAlignment = ParagraphAlignment.Near })
                        using (var textLayout = new TextLayout(renderingState.DirectWriteFactory, labelView.Text, textFormat, textBounds.Width, textBounds.Height))
                        {
                            renderingState.D2DRenderTarget.DrawTextLayout(textBounds.Minimum, textLayout, brush);
                        }
                    }
                });
            }

            private void RenderBackground([NotNull] Direct2DRenderingState renderingState)
            {
                var backColor = Color.FromArgb(255, 25, 25, 25);
                renderingState.D2DRenderTarget.Clear(backColor.ToColor4());

                var scale = _container.Root.Scale;
                var gridOffset = _container.Root.Location * _container.Root.Scale;

                // Raw, non-transformed target grid separation.
                var baseGridSize = new Vector(100, 100);

                // Scale grid to increments of baseGridSize over zoom step.
                var largeGridSize = Vector.Round(baseGridSize * scale);
                var smallGridSize = largeGridSize / 10;

                var reg = new System.Drawing.RectangleF(PointF.Empty, _control.Size);

                float startX = gridOffset.X % largeGridSize.X - largeGridSize.X;
                float endX = reg.Right;

                float startY = gridOffset.Y % largeGridSize.Y - largeGridSize.Y;
                float endY = reg.Bottom;

                var smallGridColor = Color.FromArgb(40, 40, 40).ToColor4();
                var largeGridColor = Color.FromArgb(50, 50, 50).ToColor4();

                // Draw small grid (when zoomed in enough)
                if (scale > new Vector(1.5f, 1.5f))
                {
                    using (var gridPen = new SolidColorBrush(renderingState.D2DRenderTarget, smallGridColor))
                    {
                        for (float x = startX - reg.Left % smallGridSize.X; x <= endX; x += smallGridSize.X)
                        {
                            renderingState.D2DRenderTarget.DrawLine(new RawVector2((int) x, (int) reg.Top),
                                new RawVector2((int) x, (int) reg.Bottom), gridPen);
                        }

                        for (float y = startY - reg.Top % smallGridSize.Y; y <= endY; y += smallGridSize.Y)
                        {
                            renderingState.D2DRenderTarget.DrawLine(new RawVector2((int)reg.Left, (int)y),
                                new RawVector2((int)reg.Right, (int)y), gridPen);
                        }
                    }
                }

                // Draw large grid on top
                using (var gridPen = new SolidColorBrush(renderingState.D2DRenderTarget, largeGridColor))
                {
                    for (float x = startX - reg.Left % largeGridSize.X; x <= endX; x += largeGridSize.X)
                    {
                        renderingState.D2DRenderTarget.DrawLine(new RawVector2((int)x, (int)reg.Top),
                            new RawVector2((int)x, (int)reg.Bottom), gridPen);
                    }

                    for (float y = startY - reg.Top % largeGridSize.Y; y <= endY; y += largeGridSize.Y)
                    {
                        renderingState.D2DRenderTarget.DrawLine(new RawVector2((int)reg.Left, (int)y),
                            new RawVector2((int)reg.Right, (int)y), gridPen);
                    }
                }
            }
            
            private static unsafe SharpDX.Direct2D1.Bitmap CreateSharpDxBitmap([NotNull] RenderTarget renderTarget, [NotNull] Bitmap bitmap)
            {
                var bitmapProperties =
                    new BitmapProperties(new PixelFormat(Format.R8G8B8A8_UNorm, AlphaMode.Premultiplied));

                var size = new Size2(bitmap.Width, bitmap.Height);

                // Transform pixels from BGRA to RGBA
                int stride = bitmap.Width * sizeof(int);
                using (var tempStream = new DataStream(bitmap.Height * stride, true, true))
                {
                    // Lock System.Drawing.Bitmap
                    var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
                    var data = (byte*)bitmapData.Scan0;

                    // Convert all pixels 
                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        int offset = bitmapData.Stride * y;
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            byte b = data[offset++];
                            byte g = data[offset++];
                            byte r = data[offset++];
                            byte a = data[offset++];
                            int rgba = r | (g << 8) | (b << 16) | (a << 24);
                            tempStream.Write(rgba);
                        }
                    }
                    bitmap.UnlockBits(bitmapData);
                    tempStream.Position = 0;

                    return new SharpDX.Direct2D1.Bitmap(renderTarget, size, tempStream, stride, bitmapProperties);
                }
            }
        }
    }
}