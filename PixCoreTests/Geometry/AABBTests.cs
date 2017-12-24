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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PixCore.Geometry;

namespace PixCoreTests.Geometry
{
    [TestClass]
    // ReSharper disable once InconsistentNaming
    public class AABBTests
    {
        [TestMethod]
        public void TestIsEmpty()
        {
            var empty = new AABB();
            var nonEmpty = new AABB(0, 0, 1, 1);
            var emptyNonZero = new AABB(1, 1, 1, 1);

            Assert.IsTrue(empty.IsEmpty);
            Assert.IsFalse(nonEmpty.IsEmpty);
            Assert.IsTrue(emptyNonZero.IsEmpty);
        }
    }
}