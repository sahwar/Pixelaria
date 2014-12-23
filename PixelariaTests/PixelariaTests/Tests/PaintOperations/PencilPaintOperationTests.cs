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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixelaria.Algorithms.PaintOperations;
using Pixelaria.Algorithms.PaintOperations.Interfaces;
using Pixelaria.Utils;
using PixelariaTests.PixelariaTests.Generators;
using Rhino.Mocks;

namespace PixelariaTests.PixelariaTests.Tests.PaintOperations
{
    /// <summary>
    /// Tests the pencil paint operation and related components
    /// </summary>
    [TestClass]
    public class PencilPaintOperationTests
    {
        /// <summary>
        /// Specifies the name of the operation currently being tested
        /// </summary>
        public const string OPERATION_NAME = "Pencil";

        /// <summary>
        /// Tests the actual paint operation
        /// </summary>
        [TestMethod]
        public void TestBasicPaintOperation()
        {
            Bitmap target = new Bitmap(64, 64);
            FastBitmap.ClearBitmap(target, Color.Transparent);

            PencilPaintOperation operation = new PencilPaintOperation(target) { Color = Color.Black };

            operation.StartOpertaion();
            
            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(15, 17);
            operation.DrawTo(20, 25);
            operation.DrawTo(25, 37);

            operation.FinishOperation();

            // Hash of the .png image that represents the target result of the paint operation. Generated through the 'RegisterResultBitmap' method
            byte[] goodHash = { 0xC5, 0x6B, 0x5C, 0x6B, 0xB0, 0x12, 0xBD, 0x28, 0xC4, 0x13, 0x8D, 0xAA, 0x5, 0xA1, 0x71, 0x5D, 0x1B, 0xAF, 0x9B, 0x4, 0xE7, 0x85, 0x98, 0x1E, 0xFD, 0xD4, 0x14, 0xC0, 0xB6, 0x36, 0x32, 0xA1 };
            byte[] currentHash = GetHashForBitmap(target);

            RegisterResultBitmap(target, "PencilOperation_BasicPaint");

            Assert.IsTrue(goodHash.SequenceEqual(currentHash), "The hash for the paint operation does not match the good hash stored. Verify the output image for an analysis of what went wrong");
        }

        /// <summary>
        /// Tests that the pencil operation is working correctly with a transparent color and a source copy compositing mode
        /// </summary>
        [TestMethod]
        public void TestSourceCopyTransparentOperation()
        {
            Bitmap target = FrameGenerator.GenerateRandomBitmap(64, 64, 10);

            PencilPaintOperation operation = new PencilPaintOperation(target)
            {
                Color = Color.FromArgb(127, 0, 0, 0),
                CompositingMode = CompositingMode.SourceCopy
            };

            operation.StartOpertaion();

            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(15, 17);
            operation.DrawTo(20, 25);
            operation.DrawTo(25, 37);

            operation.FinishOperation();

            // Hash of the .png image that represents the target result of the paint operation. Generated through the 'RegisterResultBitmap' method
            byte[] goodHash = { 0x7C, 0xB0, 0xE9, 0x83, 0x12, 0xC3, 0x13, 0x74, 0x20, 0xCA, 0x40, 0x8E, 0x27, 0x11, 0x8B, 0xF5, 0xE9, 0x5F, 0x33, 0x41, 0xCE, 0x7D, 0x8D, 0x74, 0x76, 0x5C, 0xA6, 0xD1, 0xAB, 0x90, 0x1C, 0x34 };
            byte[] currentHash = GetHashForBitmap(target);

            RegisterResultBitmap(target, "PencilOperation_SourceCopyTransparentPaint");

            Assert.IsTrue(goodHash.SequenceEqual(currentHash), "The hash for the paint operation does not match the good hash stored. Verify the output image for an analysis of what went wrong");
        }

        /// <summary>
        /// Tests that the pencil operation is working correctly with a transparent color and a source over compositing mode
        /// </summary>
        [TestMethod]
        public void TestSourceOverTransparentOperation()
        {
            Bitmap target = FrameGenerator.GenerateRandomBitmap(64, 64, 10);

            PencilPaintOperation operation = new PencilPaintOperation(target)
            {
                Color = Color.FromArgb(127, 0, 0, 0),
                CompositingMode = CompositingMode.SourceOver
            };

            operation.StartOpertaion();

            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(15, 17);
            operation.DrawTo(20, 25);
            operation.DrawTo(25, 37);
            operation.DrawTo(5, 5);

            operation.FinishOperation();

            // Hash of the .png image that represents the target result of the paint operation. Generated through the 'RegisterResultBitmap' method
            byte[] goodHash = { 0x7D, 0xBB, 0x8A, 0xAE, 0x0, 0x75, 0x55, 0x54, 0x67, 0xE1, 0x35, 0x90, 0xE2, 0x77, 0xD3, 0xF1, 0xE4, 0xAD, 0xE2, 0xD6, 0xB, 0xDA, 0xCA, 0xB9, 0xDD, 0x64, 0x99, 0x70, 0xFF, 0x69, 0x6D, 0x52 };
            byte[] currentHash = GetHashForBitmap(target);

            RegisterResultBitmap(target, "PencilOperation_SourceOverTransparentPaint");

            Assert.IsTrue(goodHash.SequenceEqual(currentHash), "The hash for the paint operation must match the good hash stored");
        }

        /// <summary>
        /// Tests that the pencil operation is working correctly with a transparent color, a source over compositing mode, and accumulate alpha set to false
        /// </summary>
        [TestMethod]
        public void TestSourceOverAlphaAccumulationOffTransparentOperation()
        {
            Bitmap target = FrameGenerator.GenerateRandomBitmap(64, 64, 10);

            PencilPaintOperation operation = new PencilPaintOperation(target)
            {
                Color = Color.FromArgb(127, 0, 0, 0),
                CompositingMode = CompositingMode.SourceOver
            };

            operation.StartOpertaion(false);

            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(15, 17);
            operation.DrawTo(20, 25);
            operation.DrawTo(25, 37);
            operation.DrawTo(5, 5);

            operation.FinishOperation();

            // Hash of the .png image that represents the target result of the paint operation. Generated through the 'RegisterResultBitmap' method
            byte[] goodHash = { 0x7E, 0xCD, 0xAB, 0x2D, 0xB, 0x48, 0x83, 0x2B, 0x1E, 0xCA, 0xA, 0x98, 0x68, 0x58, 0x86, 0x66, 0x67, 0x15, 0x62, 0xDA, 0xC4, 0xB5, 0xE2, 0x8, 0x12, 0xBD, 0x3C, 0x7A, 0xF2, 0x92, 0x80, 0x9 };
            byte[] currentHash = GetHashForBitmap(target);

            RegisterResultBitmap(target, "PencilOperation_SourceOverTransparentPaint_AccumulateAlphaOff");

            Assert.IsTrue(goodHash.SequenceEqual(currentHash), "The hash for the paint operation must match the good hash stored");
        }

        /// <summary>
        /// Tests whether the notification system is working properly
        /// </summary>
        [TestMethod]
        public void TestOperationNotification()
        {
            // Setup the test by performing a few pencil strokes
            Bitmap target = new Bitmap(64, 64);
            FastBitmap.ClearBitmap(target, Color.Transparent);

            // Stub a notifier
            IPlottingOperationNotifier stubNotifier = MockRepository.GenerateStub<IPlottingOperationNotifier>(null);

            // Create the operation and perform it
            PencilPaintOperation operation = new PencilPaintOperation(target)
            {
                Color = Color.Black,
                Notifier = stubNotifier
            };

            // Perform the operation
            operation.StartOpertaion();

            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(5, 5);

            operation.FinishOperation();

            // Test the results
            // Test line from 5x5 -> 10x10
            for (int i = 5; i <= 10; i++)
            {
                stubNotifier.AssertWasCalled(x => x.PlottedPixel(new Point(i, i), Color.Transparent.ToArgb(), Color.Black.ToArgb()));
            }
            // Test line that goes back from 9x9 -> 5x5, in which the black pixels due to the previous DrawTo() are being drawn over again
            for (int i = 5; i < 10; i++)
            {
                stubNotifier.AssertWasCalled(x => x.PlottedPixel(new Point(i, i), Color.Black.ToArgb(), Color.Black.ToArgb()));
            }
        }

        /// <summary>
        /// Tests the undo for the pencil paint operation
        /// </summary>
        [TestMethod]
        public void TestUndoOperation()
        {
            // Create the objects
            Bitmap target = new Bitmap(64, 64);
            FastBitmap.ClearBitmap(target, Color.Transparent);
            byte[] originalHash = GetHashForBitmap(target);

            // Create the test subjects
            PlottingPaintUndoGenerator generator = new PlottingPaintUndoGenerator(target, "Pencil");
            PencilPaintOperation operation = new PencilPaintOperation(target) { Color = Color.Black, Notifier = generator };

            operation.StartOpertaion();

            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(15, 17);
            operation.DrawTo(20, 25);
            operation.DrawTo(25, 37);

            operation.FinishOperation();

            // Undo the task
            generator.UndoTask.Undo();

            byte[] afterUndoHash = GetHashForBitmap(target);

            RegisterResultBitmap(target, "PencilOperation_AfterUndo");

            Assert.IsTrue(originalHash.SequenceEqual(afterUndoHash), "After undoing a paint operation's task, its pixels must return to their original state before the operation was applied");
        }

        /// <summary>
        /// Tests the actual paint operation
        /// </summary>
        [TestMethod]
        public void TestPaintOperation()
        {
            Bitmap target = new Bitmap(64, 64);
            FastBitmap.ClearBitmap(target, Color.Transparent);

            PencilPaintOperation operation = new PencilPaintOperation(target) { Color = Color.Black };

            operation.StartOpertaion();

            operation.MoveTo(5, 5);
            operation.DrawTo(10, 10);
            operation.DrawTo(15, 17);
            operation.DrawTo(20, 25);
            operation.DrawTo(25, 37);

            operation.FinishOperation();

            // Hash of the .png image that represents the target result of the paint operation. Generated through the 'RegisterResultBitmap' method
            byte[] goodHash = { 0xC5, 0x6B, 0x5C, 0x6B, 0xB0, 0x12, 0xBD, 0x28, 0xC4, 0x13, 0x8D, 0xAA, 0x5, 0xA1, 0x71, 0x5D, 0x1B, 0xAF, 0x9B, 0x4, 0xE7, 0x85, 0x98, 0x1E, 0xFD, 0xD4, 0x14, 0xC0, 0xB6, 0x36, 0x32, 0xA1 };
            byte[] currentHash = GetHashForBitmap(target);

            RegisterResultBitmap(target, "PencilOperation_BlendedPaint");

            Assert.IsTrue(goodHash.SequenceEqual(currentHash), "The hash for the paint operation must match the good hash stored");
        }

        /// <summary>
        /// Saves the specified bitmap on a desktop folder used to store resulting operations' bitmaps with the specified file name.
        /// The method saves both a .png format of the image, and a .txt file containing an array of bytes for the image's SHA256 hash
        /// </summary>
        /// <param name="bitmap">The bitmap to save</param>
        /// <param name="name">The file name to use on the bitmap</param>
        public void RegisterResultBitmap(Bitmap bitmap, string name)
        {
            string folder = "PaintToolResults" + Path.DirectorySeparatorChar + OPERATION_NAME;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + Path.DirectorySeparatorChar + folder;
            string file = path + Path.DirectorySeparatorChar + name;

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            bitmap.Save(file + ".png", ImageFormat.Png);

            // Also save a .txt file containing the hash
            byte[] hashBytes = GetHashForBitmap(bitmap);
            string hashString = "";
            hashBytes.ToList().ForEach(b => hashString += (hashString.Length == 0 ? "" : ",") +"0x" + b.ToString("X"));
            File.WriteAllText(file + "_hash.txt", hashString);
        }

        /// <summary>
        /// The hashing algorithm used for hashing the bitmaps
        /// </summary>
        private static readonly HashAlgorithm ShaM = new SHA256Managed();

        /// <summary>
        /// Returns a hash for the given Bitmap object
        /// </summary>
        /// <param name="bitmap">The bitmap to get the hash of</param>
        /// <returns>The hash of the given bitmap</returns>
        public static byte[] GetHashForBitmap(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Png);

                stream.Position = 0;

                // Compute a hash for the image
                byte[] hash = GetHashForStream(stream);

                return hash;
            }
        }

        /// <summary>
        /// Returns a hash for the given Stream object
        /// </summary>
        /// <param name="stream">The stream to get the hash of</param>
        /// <returns>The hash of the given stream</returns>
        public static byte[] GetHashForStream(Stream stream)
        {
            // Compute a hash for the image
            return ShaM.ComputeHash(stream);
        }
    }
}