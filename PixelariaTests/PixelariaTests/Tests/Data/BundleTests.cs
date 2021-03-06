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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pixelaria.Data;
using PixelariaTests.PixelariaTests.Generators;

namespace PixelariaTests.PixelariaTests.Tests.Data
{
    /// <summary>
    /// Tests the Bundle class functionalities and their respective components
    /// </summary>
    [TestClass]
    public class BundleTests
    {
        [TestMethod]
        public void TestBundleEquality()
        {
            Bundle bundle1 = BundleGenerator.GenerateTestBundle(0);
            Bundle bundle2 = bundle1.Clone();

            Assert.AreEqual(bundle1, bundle2, "After a Clone() operation, both Bundles must be equal");

            // Modify the new bundle
            bundle2.RemoveAnimationFromAnimationSheet(bundle2.Animations[0], bundle2.AnimationSheets[0]);

            Assert.AreNotEqual(bundle1, bundle2, "Equal bundles after a Clone() operation must not be equal after a successful call to RemoveAnimationFromAnimationSheet()");
        }

        [TestMethod]
        public void TestBundleAnimationFetchById()
        {
            Bundle bundle = new Bundle("TestBundle");
            bundle.AddAnimation(AnimationGenerator.GenerateAnimation("TestAnimation1", 16, 16, 5, 0));
            bundle.AddAnimation(AnimationGenerator.GenerateAnimation("TestAnimation2", 16, 16, 5, 0));

            Animation firstAnimation = bundle.Animations[0];
            Animation secondAnimation = bundle.Animations[0];
            const int nonExistingId = 10;

            // Existing
            Assert.AreEqual(firstAnimation, bundle.GetAnimationByID(firstAnimation.ID),
                "Getting an animation by ID should always return an animation matching a specified ID on the bundle when it exists");
            Assert.AreEqual(secondAnimation, bundle.GetAnimationByID(secondAnimation.ID),
                "Getting an animation by ID should always return an animation matching a specified ID on the bundle when it exists");

            // Non-existing
            Assert.IsNull(bundle.GetAnimationByID(nonExistingId),
                "When trying to fetch an unexisting animation ID, null should be returned");
        }

        [TestMethod]
        public void TestBundleAnimationFetchByName()
        {
            Bundle bundle = new Bundle("TestBundle");
            bundle.AddAnimation(AnimationGenerator.GenerateAnimation("TestAnimation1", 16, 16, 5, 0));
            bundle.AddAnimation(AnimationGenerator.GenerateAnimation("TestAnimation2", 16, 16, 5, 0));

            Animation firstAnimation = bundle.Animations[0];
            Animation secondAnimation = bundle.Animations[0];
            const string nonExistingName = "B4DF00D";

            // Existing
            Assert.AreEqual(firstAnimation, bundle.GetAnimationByName(firstAnimation.Name),
                "Getting an animation by name should always return an animation matching a specified name on the bundle when it exists");
            Assert.AreEqual(secondAnimation, bundle.GetAnimationByName(secondAnimation.Name),
                "Getting an animation by name should always return an animation matching a specified name on the bundle when it exists");

            // Non-existing
            Assert.IsNull(bundle.GetAnimationByName(nonExistingName),
                "When trying to fetch an unexisting animation name, null should be returned");
        }

        [TestMethod]
        public void TestBundleAnimationSheetFetchById()
        {
            Bundle bundle = new Bundle("TestBundle");
            bundle.AddAnimationSheet(AnimationSheetGenerator.GenerateAnimationSheet("TestSheet1", 5, 16, 16, 5, 0));
            bundle.AddAnimationSheet(AnimationSheetGenerator.GenerateAnimationSheet("TestSheet2", 5, 16, 16, 5, 0));

            AnimationSheet firstSheet = bundle.AnimationSheets[0];
            AnimationSheet secondSheet = bundle.AnimationSheets[0];
            const int nonExistingId = 10;

            // Existing
            Assert.AreEqual(firstSheet, bundle.GetAnimationSheetByID(firstSheet.ID),
                "Getting an animation sheet by ID should always return an animation sheet matching a specified ID on the bundle when it exists");
            Assert.AreEqual(secondSheet, bundle.GetAnimationSheetByID(secondSheet.ID),
                "Getting an animation sheet by ID should always return an animation sheet matching a specified ID on the bundle when it exists");

            // Non-existing
            Assert.IsNull(bundle.GetAnimationSheetByID(nonExistingId),
                "When trying to fetch an unexisting animation sheet ID, null should be returned");
        }

        [TestMethod]
        public void TestBundleAnimationSheetFetchByName()
        {
            Bundle bundle = new Bundle("TestBundle");
            bundle.AddAnimationSheet(AnimationSheetGenerator.GenerateAnimationSheet("TestSheet1", 5, 16, 16, 5, 0));
            bundle.AddAnimationSheet(AnimationSheetGenerator.GenerateAnimationSheet("TestSheet2", 5, 16, 16, 5, 0));

            AnimationSheet firstSheet = bundle.AnimationSheets[0];
            AnimationSheet secondSheet = bundle.AnimationSheets[0];
            const string nonExistingName = "B4DF00D";

            // Existing
            Assert.AreEqual(firstSheet, bundle.GetAnimationSheetByName(firstSheet.Name),
                "Getting an animation sheet by name should always return an animation sheet matching a specified name on the bundle when it exists");
            Assert.AreEqual(secondSheet, bundle.GetAnimationSheetByName(secondSheet.Name),
                "Getting an animation sheet by name should always return an animation sheet matching a specified name on the bundle when it exists");

            // Non-existing
            Assert.IsNull(bundle.GetAnimationSheetByName(nonExistingName),
                "When trying to fetch an unexisting animation sheet name, null should be returned");
        }

        [TestMethod]
        public void TestBundleFrameIdGeneration()
        {
            Bundle bundle1 = BundleGenerator.GenerateTestBundle(0);
            Bundle bundle2 = new Bundle("TestBundle2");

            // Remove animation from one bundle and add to another
            Animation anim = bundle1.Animations[0];

            bundle1.RemoveAnimation(anim);
            bundle2.AddAnimation(anim);

            // Create a frame on the animation on bundle2
            Frame newFrame = bundle2.Animations[0].CreateFrame();

            // Test if the frame's ID matches the previos frame's ID + 1
            Assert.AreEqual(newFrame.ID, bundle2.Animations[0][bundle2.Animations[0].FrameCount - 2].ID + 1,
                "When adding an animation to a bundle, the frame ID index should be bumped up to match the highest frame ID available + 1");
        }
    }
}