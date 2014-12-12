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
using System.Linq;
using System.Text;

using Pixelaria.Data;
using Pixelaria.Data.Exports;
using Pixelaria.Utils;

namespace Pixelaria.Algorithms.Packers
{
    /// <summary>
    /// Defines the default texture packer for the program
    /// </summary>
    class DefaultTexturePacker : ITexturePacker
    {
        /// <summary>
        /// Packs a given atlas with a specified progress event handler
        /// </summary>
        /// <param name="atlas">The texture atlas to pack</param>
        /// <param name="handler">The event handler for the packing process</param>
        public void Pack(TextureAtlas atlas, BundleExportProgressEventHandler handler = null)
        {
            // Cache some fields as locals
            AnimationExportSettings exportSettings = atlas.ExportSettings;
            List<Frame> frameList = atlas.FrameList;

            this.progressHandler = handler;

            if (atlas.FrameCount == 0)
            {
                atlas.AtlasRectangle = new Rectangle(0, 0, 1, 1);
                return;
            }

            uint atlasWidth = 0;
            uint atlasHeight = 0;
            int maxWidthCapped = 0;
            int maxWidthReal = 0;

            int maxFrameWidth = 0;
            int maxFrameHeight = 0;

            int minFrameWidth = int.MaxValue;

            frameComparision = new FrameComparision(atlas.ExportSettings.ForceMinimumDimensions);

            // 1. (Optional) Sort the frames from largest to smallest before packing
            if (exportSettings.AllowUnorderedFrames)
            {
                // Use a stable sort
                frameList.AddRange(frameList.OrderBy(frame => frame, frameComparision).ToList());
                frameList.RemoveRange(0, frameList.Count / 2);
            }

            // 2. (Optional) Find identical frames and pack them to use the same sheet area
            if (exportSettings.ReuseIdenticalFramesArea)
            {
                for (int i = 0; i < frameList.Count; i++)
                {
                    for (int j = i + 1; j < frameList.Count; j++)
                    {
                        if (frameList[i].Equals(frameList[j]))
                        {
                            frameComparision.RegisterSimilarFrames(frameList[i], frameList[j]);
                            break;
                        }
                    }
                }
            }

            // 3. Find the maximum possible horizontal sheet size
            foreach (Frame frame in frameList)
            {
                if (frame.Width > maxFrameWidth)
                {
                    maxFrameWidth = frame.Width;
                }
                if (frame.Height > maxFrameHeight)
                {
                    maxFrameHeight = frame.Height;
                }
                if (frame.Width < minFrameWidth)
                {
                    minFrameWidth = frame.Width;
                }

                maxWidthCapped += frame.Width;
            }

            maxWidthReal = maxWidthCapped;
            maxWidthCapped = Math.Min(4096, maxWidthCapped);

            int minAreaWidth = maxWidthCapped;
            float minRatio = 0;
            int minArea = int.MaxValue;

            // 4. Iterate through possible widths and match the smallest area to use as a maxWidth
            uint curWidth = (uint)maxFrameWidth;

            if (exportSettings.ForcePowerOfTwoDimensions)
            {
                curWidth = Utilities.SnapToNextPowerOfTwo(curWidth);
            }

            for (; curWidth < maxWidthCapped; )
            {
                atlasWidth = 0;
                atlasHeight = 0;
                InternalPack(atlas, ref atlasWidth, ref atlasHeight, (int)curWidth, exportSettings.UseUniformGrid ? maxFrameWidth : -1, exportSettings.UseUniformGrid ? maxFrameHeight : -1);

                float ratio = (float)atlasWidth / atlasHeight;

                // Round up to the closest power of two
                if (exportSettings.ForcePowerOfTwoDimensions)
                {
                    atlasWidth = Utilities.SnapToNextPowerOfTwo(atlasWidth);
                    atlasHeight = Utilities.SnapToNextPowerOfTwo(atlasHeight);
                }

                // Calculate the area now
                int area = (int)(atlasWidth * atlasHeight);

                float areaDiff = (float)area / minArea;
                float ratioDiff = (float)Math.Abs(ratio - 1) / Math.Abs(minRatio - 1);

                bool swap = false;

                // Decide whether to swap the best sheet target width with tue current one
                if (exportSettings.FavorRatioOverArea && (Math.Abs(ratio - 1) < Math.Abs(minRatio - 1)))
                {
                    swap = true;
                }
                else if (!exportSettings.FavorRatioOverArea && (area < minArea || (Math.Abs(ratio - 1) < Math.Abs(minRatio - 1))))
                {
                    swap = true;
                }

                if (swap)
                {
                    minArea = area;
                    minRatio = ratio;
                    minAreaWidth = (int)curWidth;
                }

                // Iterate the width now
                if (exportSettings.HighPrecisionAreaMatching)
                {
                    curWidth++;
                }
                else
                {
                    curWidth += (uint)minFrameWidth / 2;
                }

                // Report progress
                if (handler != null)
                {
                    int progress = (int)((float)curWidth / maxWidthCapped * 100);

                    if (exportSettings.FavorRatioOverArea)
                    {
                        progress = (int)((float)atlasWidth / atlasHeight * 100);
                    }

                    progress = Math.Min(100, progress);

                    handler.Invoke(new BundleExportProgressEventArgs(BundleExportStage.TextureAtlasGeneration, progress, progress, atlas.Name));
                }

                // Exit the loop if favoring ratio and no better ratio can be achieved
                if (exportSettings.FavorRatioOverArea && atlasWidth > atlasHeight)
                {
                    break;
                }
            }

            atlasWidth = 0;
            atlasHeight = 0;

            // 5. Pack the texture atlas
            InternalPack(atlas, ref atlasWidth, ref atlasHeight, minAreaWidth, exportSettings.UseUniformGrid ? maxFrameWidth : -1, exportSettings.UseUniformGrid ? maxFrameHeight : -1, true);

            // Round up to the closest power of two
            if (exportSettings.ForcePowerOfTwoDimensions)
            {
                atlasWidth = Utilities.SnapToNextPowerOfTwo(atlasWidth);
                atlasHeight = Utilities.SnapToNextPowerOfTwo(atlasHeight);
            }

            if (atlasWidth == 0)
                atlasWidth = 1;
            if (atlasHeight == 0)
                atlasHeight = 1;

            atlas.AtlasRectangle = new Rectangle(0, 0, (int)atlasWidth, (int)atlasHeight);

            // Assign the information on the texture atlas
            atlas.Information.ReusedFrameOriginsCount = frameComparision.CachedSimilarCount;
        }

        /// <summary>
        /// Internal atlas packer method
        /// </summary>
        /// <param name="atlas">The atlas to pack internally</param>
        /// <param name="atlasWidth">An output atlas width uint</param>
        /// <param name="atlasHeight">At output atlas height uint</param>
        /// <param name="maxWidth">The maximum width the generated sheet can have</param>
        /// <param name="frameWidth">The uniform width to use for all the frames. -1 uses the individual frame width</param>
        /// <param name="frameHeight">The uniform height to use for all the frames. -1 uses the individual frame height</param>
        /// <param name="registerReused">Whether to register the reused frame indices in this pass</param>
        private void InternalPack(TextureAtlas atlas, ref uint atlasWidth, ref uint atlasHeight, int maxWidth, int frameWidth = -1, int frameHeight = -1, bool registerReused = false)
        {
            // Cache some fields as locals
            AnimationExportSettings exportSettings = atlas.ExportSettings;
            List<Frame> frameList = atlas.FrameList;
            List<Rectangle> originsList = atlas.OriginsList;
            List<Rectangle> boundsList = atlas.BoundsList;

            int x = exportSettings.XPadding;
            int y;
            int width = 0;
            int height = 0;

            for (int i = 0; i < frameList.Count; i++)
            {
                Frame frame = frameList[i];

                ////
                //// 1. Identical frame matching
                ////
                if (exportSettings.ReuseIdenticalFramesArea)
                {
                    Frame original = frameComparision.GetOriginalSimilarFrame(frame);

                    if (original != null)
                    {
                        int originalIndex = frameList.IndexOf(original);

                        originsList[i] = originsList[originalIndex];
                        boundsList[i] = boundsList[originalIndex];

                        continue;
                    }
                }

                ////
                //// 2. Calculate frame origin
                ////
                originsList[i] = new Rectangle(0, 0, (frameWidth == -1 ? frame.Width : frameWidth), (frameHeight == -1 ? frame.Height : frameHeight));

                if (exportSettings.ForceMinimumDimensions && !exportSettings.UseUniformGrid)
                {
                    originsList[i] = frameComparision.GetFrameArea(frame);
                }

                width = originsList[i].Width;
                height = originsList[i].Height;

                // X coordinate wrapping
                if (x + width > maxWidth)
                {
                    x = exportSettings.XPadding;
                }

                y = exportSettings.YPadding;

                // Do a little trickery to find the minimum Y for this frame
                if (x - exportSettings.XPadding < atlasWidth)
                {
                    // Intersect the current frame rectangle with all rectangles above it, and find the maximum bottom Y coordinate between all the intersections
                    int contactRectX = x - exportSettings.XPadding;
                    int contactRectWidth = width + exportSettings.XPadding * 2;

                    for (int j = 0; j < i; j++)
                    {
                        Rectangle rect = boundsList[j];

                        if (rect.X < (contactRectX + contactRectWidth) && contactRectX < (rect.X + rect.Width))
                        {
                            y = Math.Max(y, rect.Y + rect.Height + exportSettings.YPadding);
                        }
                    }
                }

                ////
                //// 3. Calculate frame area on sheet
                ////
                boundsList[i] = new Rectangle(x, y, width, height);

                atlasWidth = (uint)Math.Max(atlasWidth, boundsList[i].X + boundsList[i].Width + exportSettings.XPadding);
                atlasHeight = (uint)Math.Max(atlasHeight, boundsList[i].Y + boundsList[i].Height + exportSettings.YPadding);


                // X coordinate update
                x += boundsList[i].Width + exportSettings.XPadding;

                if (x > maxWidth)
                {
                    x = exportSettings.XPadding;
                }
            }

            atlas.ReuseCount.Clear();

            for (int i = 0; i < frameList.Count; i++)
            {
                if (exportSettings.ReuseIdenticalFramesArea)
                {
                    Rectangle frameBounds = boundsList[i];
                    int repCount = 0;

                    for (int j = 0; j < frameList.Count; j++)
                    {
                        if (i == j)
                            continue;

                        Rectangle otherFrameBounds = boundsList[j];

                        if (frameBounds == otherFrameBounds)
                        {
                            repCount++;
                        }
                    }

                    atlas.ReuseCount.Add(repCount);
                }
                else
                {
                    atlas.ReuseCount.Add(0);
                }
            }
        }

        /// <summary>
        /// The default FrameComparision object that will sort frames by size and cache information about the frames' areas
        /// </summary>
        private FrameComparision frameComparision;

        /// <summary>
        /// The current event handler to report progress to
        /// </summary>
        private BundleExportProgressEventHandler progressHandler;

        /// <summary>
        /// Default IComparer used to sort and store information about frames
        /// </summary>
        public class FrameComparision : IComparer<Frame>
        {
            /// <summary>
            /// Dictionary of internal compare fragments
            /// </summary>
            Dictionary<Frame, CompareFrag> fragDictionary;

            /// <summary>
            /// List of internal similar fragments
            /// </summary>
            Dictionary<Frame, SimilarFrag> similarDictionary;

            /// <summary>
            /// Whether to compute the minimum areas of the frames before comparing them
            /// </summary>
            bool useMinimumTextureArea;

            /// <summary>
            /// Gets the number of cached compare fragments currently stores
            /// </summary>
            public int CachedCompareCount { get { return fragDictionary.Count; } }

            /// <summary>
            /// Gets the number of cached similar fragments currently stores
            /// </summary>
            public int CachedSimilarCount { get { return similarDictionary.Count; } }

            /// <summary>
            /// Creates a new instance of the FrameComparision class
            /// </summary>
            /// <param name="useMinimumTextureArea">Whether to compute the minimum areas of the frames before comparing them</param>
            public FrameComparision(bool useMinimumTextureArea)
            {
                this.fragDictionary = new Dictionary<Frame, CompareFrag>();
                this.similarDictionary = new Dictionary<Frame, SimilarFrag>();

                this.useMinimumTextureArea = useMinimumTextureArea;
            }

            /// <summary>
            /// Resets this comparer
            /// </summary>
            public void Reset()
            {
                // Clear the references for the GC's sake
                fragDictionary.Clear();
                similarDictionary.Clear();
            }

            // Summary:
            //     Compares two objects and returns a value indicating whether one is less than,
            //     equal to, or greater than the other.
            // 
            // Parameters:
            //   x:
            //     The first object to compare.
            // 
            //   y:
            //     The second object to compare.
            // 
            // Returns:
            //     A signed integer that indicates the relative values of x and y, as shown
            //     in the following table.
            //     Value                Meaning
            //     Less than zero       x is less than y.
            //     Zero                 x equals y.
            //     Greater than zero    x is greater than y.
            public int Compare(Frame x, Frame y)
            {
                // Get the frame areas
                Rectangle minFrameX = GetFrameArea(x);
                Rectangle minFrameY = GetFrameArea(y);

                int xArea = minFrameX.Width * minFrameX.Height;
                int yArea = minFrameY.Width * minFrameY.Height;

                return (xArea == yArea) ? 0 : ((xArea > yArea) ? -1 : 1);
            }

            /// <summary>
            /// Gets the area for the given Frame object
            /// </summary>
            /// <param name="frame">The Frame object to get the area of</param>
            /// <returns>A Rectangle representing the Frame's area</returns>
            public Rectangle GetFrameArea(Frame frame)
            {
                // Try to find the already-computed frame area first
                CompareFrag frag;

                fragDictionary.TryGetValue(frame, out frag);

                if (frag != null)
                    return frag.FrameRectangle;

                CompareFrag newFrag = new CompareFrag();

                newFrag.Frame = frame;

                if (useMinimumTextureArea)
                    newFrag.FrameRectangle = ImageUtilities.FindMinimumImageArea(frame.GetComposedBitmap());
                else
                    newFrag.FrameRectangle = new Rectangle(0, 0, frame.Width, frame.Height);

                fragDictionary[frame] = newFrag;

                return newFrag.FrameRectangle;
            }

            /// <summary>
            /// Registers two frames as being similar to the pixel-level
            /// </summary>
            /// <param name="frame1">The first frame to register</param>
            /// <param name="frame2">The second frame to register</param>
            public void RegisterSimilarFrames(Frame frame1, Frame frame2)
            {
                similarDictionary[frame2] = new SimilarFrag() { Frame1 = frame1, Frame2 = frame2 };
            }

            /// <summary>
            /// Gets the original similar frame based on the given frame.
            /// The returned frame is the first frame inserted that is similar to the given frame. If the given frame is the original similar
            /// frame, null is returned. If no similar frames were stored, null is returned.
            /// </summary>
            /// <param name="frame">The frame to seek the original similar frame from</param>
            /// <returns>The first frame inserted that is similar to the given frame. If the given frame is the original similar frame, null is returned. If no similar frames were stored, null is returned.</returns>
            public Frame GetOriginalSimilarFrame(Frame frame)
            {
                SimilarFrag similar = null;

                if (similarDictionary.TryGetValue(frame, out similar))
                    return similar.Frame1;

                return null;
            }

            /// <summary>
            /// Internal compare speed-up fragment.
            /// The fragment is composed of a Frame and a Rectangle
            /// that represents the Frame's area
            /// </summary>
            class CompareFrag
            {
                /// <summary>
                /// The frame of this fragment
                /// </summary>
                public Frame Frame;

                /// <summary>
                /// The frame's rectangle
                /// </summary>
                public Rectangle FrameRectangle;
            }

            /// <summary>
            /// Internal similar speed-up fragment.
            /// The fragment is composed of a pair of frames that
            /// are identical in the pixel level
            /// </summary>
            class SimilarFrag
            {
                /// <summary>
                /// The first similar frame
                /// </summary>
                public Frame Frame1;

                /// <summary>
                /// The second similar frame
                /// </summary>
                public Frame Frame2;
            }
        }
    }
}