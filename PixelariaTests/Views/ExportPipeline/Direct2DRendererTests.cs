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
using System.Reactive.Subjects;
using FastBitmapLib;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PixCore.Geometry;
using PixDirectX.Rendering;
using Pixelaria.ExportPipeline;
using Pixelaria.ExportPipeline.Inputs.Abstract;
using Pixelaria.ExportPipeline.Outputs.Abstract;
using Pixelaria.Views.ExportPipeline;
using Pixelaria.Views.ExportPipeline.PipelineView;
using PixSnapshot;
using PixUI;
using PixUI.Rendering;
using SharpDX.WIC;
using Bitmap = System.Drawing.Bitmap;

namespace PixelariaTests.Views.ExportPipeline
{
    [TestClass]
    public class Direct2DRendererTests
    {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            //PipelineViewSnapshot.RecordMode = true;
        }

        [TestMethod]
        public void TestRenderEmptyPipelineNodeView()
        {
            var node = new TestPipelineStep();
            var view = new PipelineNodeView(node);

            PipelineViewSnapshot.Snapshot(view, TestContext);
        }

        [TestMethod]
        public void TestRenderPipelineNodeViewWithInput()
        {
            var node = new TestPipelineStep();
            node.InputList = new List<IPipelineInput>
            {
                new GenericPipelineInput<int>(node, "Input 1")
            };
            var view = new PipelineNodeView(node);

            PipelineViewSnapshot.Snapshot(view, TestContext);
        }

        [TestMethod]
        public void TestRenderPipelineNodeViewWithOutput()
        {
            var node = new TestPipelineStep();
            node.OutputList = new List<IPipelineOutput>
            {
                new GenericPipelineOutput<string>(node, new BehaviorSubject<string>("abc"), "Output 1"),
                new GenericPipelineOutput<string>(node, new BehaviorSubject<string>("abc"), "Output 2")
            };
            var view = new PipelineNodeView(node);

            PipelineViewSnapshot.Snapshot(view, TestContext);
        }

        [TestMethod]
        public void TestRenderPipelineNodeViewWithInputAndOutput()
        {
            var node = new TestPipelineStep();
            node.InputList = new List<IPipelineInput>
            {
                new GenericPipelineInput<int>(node, "Input 1")
            };
            node.OutputList = new List<IPipelineOutput>
            {
                new GenericPipelineOutput<string>(node, new BehaviorSubject<string>("abc"), "Output 1"),
                new GenericPipelineOutput<string>(node, new BehaviorSubject<string>("abc"), "Output 2")
            };
            var view = new PipelineNodeView(node);

            PipelineViewSnapshot.Snapshot(view, TestContext);
        }
        
        private class TestPipelineStep : IPipelineStep
        {
            public Guid Id { get; } = Guid.NewGuid();
            public string Name => "Test Pipeline Step";

            public IReadOnlyList<IPipelineInput> Input => InputList;
            public IReadOnlyList<IPipelineOutput> Output => OutputList;

            public List<IPipelineInput> InputList = new List<IPipelineInput>();
            public List<IPipelineOutput> OutputList = new List<IPipelineOutput>();

            public IPipelineMetadata GetMetadata()
            {
                return PipelineMetadata.Empty;
            }
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Helper static class to perform bitmap-based rendering comparisons of <see cref="T:Pixelaria.Views.ExportPipeline.PipelineView.PipelineNodeView" /> and related
    /// instances to assert visual and style consistency.
    /// </summary>
    public class PipelineViewSnapshot : ISnapshotProvider<BaseView>
    {
        private readonly ExportPipelineControl _control;

        /// <summary>
        /// Whether tests are currently under record mode- under record mode, results are recorded on disk to be later
        /// compared when not in record mode.
        /// 
        /// Calls to <see cref="Snapshot"/> always fail with an assertion during record mode.
        /// 
        /// Defaults to false.
        /// </summary>
        public static bool RecordMode;
        
        public static void Snapshot([NotNull] BaseView view, [NotNull] TestContext context)
        {
            BitmapSnapshotTesting.Snapshot<PipelineViewSnapshot, BaseView>(view, context, RecordMode);
        }

        public PipelineViewSnapshot()
        {
            _control = new ExportPipelineControl();
        }

        public Bitmap GenerateBitmap(BaseView view)
        {
            // Create a temporary Direct3D rendering context and render the view on it
            const BitmapCreateCacheOption bitmapCreateCacheOption = BitmapCreateCacheOption.CacheOnDemand;
            var pixelFormat = PixelFormat.Format32bppPBGRA;

            if (view is PipelineNodeView nodeView)
            {
                using (var renderManager = new Direct2DRenderLoopManager(_control))
                {
                    renderManager.InitializeDirect2D();

                    renderManager.RenderSingleFrame(state =>
                    {
                        var labelViewSizer = new DefaultLabelViewSizeProvider(new StaticDirect2DRenderingStateProvider(state));

                        var sizer = new DefaultPipelineNodeViewSizer();
                        sizer.AutoSize(nodeView, labelViewSizer);
                    });
                }
            }

            int width = (int) Math.Ceiling(view.Width);
            int height = (int) Math.Ceiling(view.Height);

            using (var imgFactory = new ImagingFactory())
            using (var wicBitmap = new SharpDX.WIC.Bitmap(imgFactory, width, height, pixelFormat, bitmapCreateCacheOption))
            using (var renderLoop = new Direct2DWicBitmapRenderManager(wicBitmap))
            using (var renderer = new Direct2DRenderer(_control.PipelineContainer, _control))
            {
                var last = LabelView.DefaultLabelViewSizeProvider;
                LabelView.DefaultLabelViewSizeProvider = renderer.LabelViewSizeProvider;

                renderLoop.InitializeDirect2D();

                renderLoop.RenderSingleFrame(state =>
                {
                    renderer.Initialize(renderLoop.RenderingState);
                    renderer.UpdateRenderingState(state, new FullClipping());

                    var parentView = new BaseView();
                    parentView.AddChild(view);

                    renderer.RenderInView(parentView, state, new IRenderingDecorator[0]);
                });

                LabelView.DefaultLabelViewSizeProvider = last;

                return BitmapFromWicBitmap(wicBitmap);
            }
        }

        private static Bitmap BitmapFromWicBitmap([NotNull] SharpDX.WIC.Bitmap wicBitmap)
        {
            var bitmap = new Bitmap(wicBitmap.Size.Width, wicBitmap.Size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var wicBitmapLock = wicBitmap.Lock(BitmapLockFlags.Read))
            using (var bitmapLock = bitmap.FastLock())
            {
                unchecked
                {
                    const int bytesPerPixel = 4; // ARGB
                    ulong length = (ulong) (wicBitmap.Size.Width * wicBitmap.Size.Height * bytesPerPixel);
                    FastBitmap.memcpy(bitmapLock.Scan0, wicBitmapLock.Data.DataPointer, length);
                }
            }

            return bitmap;
        }

        private class FullClipping : IClippingRegion
        {
            public bool IsVisibleInClippingRegion(Rectangle rectangle)
            {
                return true;
            }

            public bool IsVisibleInClippingRegion(Point point)
            {
                return true;
            }

            public bool IsVisibleInClippingRegion(AABB aabb)
            {
                return true;
            }

            public bool IsVisibleInClippingRegion(Vector point)
            {
                return true;
            }

            public bool IsVisibleInClippingRegion(AABB aabb, ISpatialReference reference)
            {
                return true;
            }

            public bool IsVisibleInClippingRegion(Vector point, ISpatialReference reference)
            {
                return true;
            }
        }
    }
}
