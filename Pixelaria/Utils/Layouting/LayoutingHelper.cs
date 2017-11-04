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
using JetBrains.Annotations;

namespace Pixelaria.Utils.Layouting
{
    /// <summary>
    /// Defines set of common layouting operations.
    /// 
    /// Mostly used by the Export Pipeline's UI system to aid in controls' content layouts.
    /// </summary>
    internal static class LayoutingHelper
    {
        /// <summary>
        /// Aligns an AABB such that it sits aligned exactly midway through its container's area
        /// in the direction(s) specified by <see cref="direction"/>
        /// </summary>
        [Pure]
        public static AABB CenterWithinContainer(AABB rect, AABB container, LayoutDirection direction)
        {
            if (direction.HasFlag(LayoutDirection.Horizontal) && direction.HasFlag(LayoutDirection.Vertical))
                return rect.WithCenterOn(container.Center);

            var newRect = rect;
            if (direction.HasFlag(LayoutDirection.Horizontal))
            {
                newRect = newRect.WithCenterOn(new Vector(container.Center.X, newRect.Center.Y));
            }
            else
            {
                newRect = newRect.WithCenterOn(new Vector(newRect.Center.X, container.Center.Y));
            }

            return newRect;
        }

        /// <summary>
        /// Returns an AABB with the same size as <see cref="aabb"/>, aligned in such a way that its
        /// center lays on <see cref="newCenter"/>
        /// </summary>
        [Pure]
        public static AABB WithCenterOn(this AABB aabb, Vector newCenter)
        {
            return aabb.OffsetTo(newCenter - aabb.Size / 2);
        }
    }

    [Flags]
    public enum LayoutDirection
    {
        Horizontal = 0b1,
        Vertical = 0b10
    }
}
