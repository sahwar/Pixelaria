/*
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
using System.Diagnostics.Contracts;
using System.Drawing;
using Pixelaria.Views.Controls.ColorControls;

namespace Pixelaria.Utils
{
    /// <summary>
    /// Represents an HSL color with an alpha channel
    /// </summary>
    public struct AhslColor : IEquatable<AhslColor>
    {
        /// <summary>
        /// Gets or sets the alpha component as a value ranging from 0 - 255
        /// </summary>
        public int A => (int) (_af * 255.0f);

        /// <summary>
        /// Gets or sets the hue component as a value ranging from 0 - 360
        /// </summary>
        public int H => (int) (_hf * 360.0f);

        /// <summary>
        /// Gets or sets the saturation component as a value ranging from 0 - 100
        /// </summary>
        public int S => (int) (_sf * 100.0f);

        /// <summary>
        /// Gets or sets the lightness component as a value ranging from 0 - 100
        /// </summary>
        public int L => (int) (_lf * 100.0f);

        /// <summary>
        /// Gets the Red component value for this AHSL color
        /// </summary>
        public int R => ToColor().R;

        /// <summary>
        /// Gets the Red component value for this AHSL color
        /// </summary>
        public int G => ToColor().G;

        /// <summary>
        /// Gets the Red component value for this AHSL color
        /// </summary>
        public int B => ToColor().B;

        /// <summary>
        /// Gets the Red component value for this AHSL color
        /// </summary>
        public float Rf => ColorSwatch.FloatArgbFromAhsl(_hf, _sf, _lf, _af)[1];

        /// <summary>
        /// Gets the Red component value for this AHSL color
        /// </summary>
        public float Gf => ColorSwatch.FloatArgbFromAhsl(_hf, _sf, _lf, _af)[2];

        /// <summary>
        /// Gets the Red component value for this AHSL color
        /// </summary>
        public float Bf => ColorSwatch.FloatArgbFromAhsl(_hf, _sf, _lf, _af)[3];

        /// <summary>
        /// Gets or sets the alpha component as a value ranging from 0 - 1
        /// </summary>
        public float Af => _af;

        /// <summary>
        /// Gets or sets the hue component as a value ranging from 0 - 1
        /// </summary>
        public float Hf => _hf;

        /// <summary>
        /// Gets or sets the saturation component as a value ranging from 0 - 1
        /// </summary>
        public float Sf => _sf;

        /// <summary>
        /// Gets or sets the lightness component as a value ranging from 0 - 1
        /// </summary>
        public float Lf => _lf;

        /// <summary>
        /// The alpha component as a value ranging from 0 - 1
        /// </summary>
        private readonly float _af;
        /// <summary>
        /// The hue component as a value ranging from 0 - 1
        /// </summary>
        private readonly float _hf;
        /// <summary>
        /// The saturation component as a value ranging from 0 - 1
        /// </summary>
        private readonly float _sf;
        /// <summary>
        /// The lightness component as a value ranging from 0 - 1
        /// </summary>
        private readonly float _lf;
        
        /// <summary>
        /// Creates a new AHSL color
        /// </summary>
        /// <param name="a">The Alpha component, ranging from 0-255</param>
        /// <param name="h">The Hue component, ranging from 0-360</param>
        /// <param name="s">The Saturation component, ranging from 0-100</param>
        /// <param name="l">The Lightness component, ranging from 0-100</param>
        public AhslColor(int a, int h, int s, int l)
            : this(a / 255.0f, h / 360.0f, s / 100.0f, l / 100.0f) { }

        /// <summary>
        /// Creates a new AHSL color
        /// </summary>
        /// <param name="a">The Alpha component, ranging from 0-1</param>
        /// <param name="h">The Hue component, ranging from 0-1</param>
        /// <param name="s">The Saturation component, ranging from 0-1</param>
        /// <param name="l">The Lightness component, ranging from 0-1</param>
        public AhslColor(float a, float h, float s, float l)
        {
            _af = Math.Max(0, Math.Min(1, a));
            _hf = Math.Max(0, Math.Min(1, h));
            _sf = Math.Max(0, Math.Min(1, s));
            _lf = Math.Max(0, Math.Min(1, l));
        }

        /// <summary>
        /// Tests whether two AHSL color structures are different
        /// </summary>
        /// <param name="color1">The first AHSL color to test</param>
        /// <param name="color2">The second AHSL color to test</param>
        /// <returns>Whether two AHSL color structures are different</returns>
        public static bool operator !=(AhslColor color1, AhslColor color2)
        {
            return !(color1 == color2);
        }

        /// <summary>
        /// Tests whether two AHSL color structures are the same
        /// </summary>
        /// <param name="color1">The first AHSL color to test</param>
        /// <param name="color2">The second AHSL color to test</param>
        /// <returns>Whether two AHSL color structures are the same</returns>
        public static bool operator==(AhslColor color1, AhslColor color2)
        {
            return (Math.Abs(color1._af - color2._af) < float.Epsilon &&
                    Math.Abs(color1._hf - color2._hf) < float.Epsilon &&
                    Math.Abs(color1._sf - color2._sf) < float.Epsilon &&
                    Math.Abs(color1._lf - color2._lf) < float.Epsilon);
        }

        /// <summary>
        /// Converts this AHSL color to a Color object
        /// </summary>
        /// <returns>The Color object that represents this AHSL color</returns>
        [Pure]
        public Color ToColor()
        {
            return Color.FromArgb(ToArgb());
        }

        /// <summary>
        /// Converts this AHSL color to a ARGB color
        /// </summary>
        /// <param name="revertByteOrder">Whether to revert the byte order so the alpha component is the most significant and the blue component the least</param>
        /// <returns>The ARGB color that represents this AHSL color</returns>
        [Pure]
        public int ToArgb(bool revertByteOrder = false)
        {
            return ColorSwatch.ArgbFromAhsl(_hf, _sf, _lf, _af, revertByteOrder);
        }

        /// <summary>
        /// Returns whether this AHSL color object equals another AHSL color
        /// </summary>
        /// <param name="other">The other color to test</param>
        /// <returns>Whether this AHSL color object equals another AHSL color</returns>
        public bool Equals(AhslColor other)
        {
            return _af.Equals(other._af) && _hf.Equals(other._hf) && _sf.Equals(other._sf) && _lf.Equals(other._lf);
        }

        // Override Equals
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is AhslColor && Equals((AhslColor)obj);
        }

        // Overrided GetHashCode
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _af.GetHashCode();
                hashCode = (hashCode * 397) ^ _hf.GetHashCode();
                hashCode = (hashCode * 397) ^ _sf.GetHashCode();
                hashCode = (hashCode * 397) ^ _lf.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Creates an AHSL object from the given AHSL color
        /// </summary>
        /// <param name="a">The Alpha component, ranging from 0-255</param>
        /// <param name="h">The Hue component, ranging from 0-360</param>
        /// <param name="s">The Saturation component, ranging from 0-100</param>
        /// <param name="l">The Lightness component, ranging from 0-100</param>
        /// <returns>The AHSL color representing the given AHSL value</returns>
        [Pure]
        public static AhslColor FromAhsl(int a, int h, int s, int l)
        {
            return new AhslColor(a, h, s, l);
        }

        /// <summary>
        /// Creates an AHSL object from the given ARGB color
        /// </summary>
        /// <param name="a">The Alpha component</param>
        /// <param name="r">The Red component</param>
        /// <param name="g">The Green component</param>
        /// <param name="b">The Blue component</param>
        /// <returns>The AHSL color representing the given ARGB value</returns>
        [Pure]
        public static AhslColor FromArgb(int a, int r, int g, int b)
        {
            return ToAhsl((a << 24) | (r << 16) | (g << 8) | b);
        }

        /// <summary>
        /// Creates an AHSL object from the given ARGB color
        /// </summary>
        /// <param name="a">The Alpha component, ranging from 0-1</param>
        /// <param name="r">The Red component, ranging from 0-1</param>
        /// <param name="g">The Green component, ranging from 0-1</param>
        /// <param name="b">The Blue component, ranging from 0-1</param>
        /// <returns>The AHSL color representing the given ARGB value</returns>
        [Pure]
        public static AhslColor FromArgb(float a, float r, float g, float b)
        {
            return ToAhsl(a, r, g, b);
        }

        /// <summary>
        /// Creates an AHSL object from the given ARGB color
        /// </summary>
        /// <param name="argb">The ARGB color to convert to AHSL</param>
        /// <returns>The AHSL color representing the given ARGB value</returns>
        [Pure]
        public static AhslColor FromArgb(int argb)
        {
            return ToAhsl(argb);
        }

        /// <summary>
        /// Converts the given ARGB color to an AHSL color
        /// </summary>
        /// <param name="argb">The color to convert to AHSL</param>
        /// <returns>An AHSL (alpha hue saturation and lightness) color</returns>
        [Pure]
        public static AhslColor ToAhsl(int argb)
        {
            float a = (int)((uint)argb >> 24);
            float r = (argb >> 16) & 0xFF;
            float g = (argb >> 8) & 0xFF;
            float b = argb & 0xFF;

            a /= 255;
            r /= 255;
            g /= 255;
            b /= 255;

            return ToAhsl(a, r, g, b);
        }

        /// <summary>
        /// Converts the given ARGB color to an AHSL color
        /// </summary>
        /// <param name="a">The alpha component</param>
        /// <param name="r">The red component</param>
        /// <param name="g">The green component</param>
        /// <param name="b">The blue component</param>
        /// <returns>An AHSL (alpha hue saturation and lightness) color</returns>
        [Pure]
        public static AhslColor ToAhsl(float a, float r, float g, float b)
        {
            // ReSharper disable once InconsistentNaming
            float M = b;
            float m = b;

            if (m > g)
                m = g;
            if (m > r)
                m = r;

            if (M < g)
                M = g;
            if (M < r)
                M = r;

            float d = M - m;

            float h;
            float s;

            // ReSharper disable CompareOfFloatsByEqualityOperator

            if (d == 0)
            {
                h = 0;
            }
            else if (M == r)
            {
                h = (((g - b) / d) % 6) * 60;
            }
            else if (M == g)
            {
                h = ((b - r) / d + 2) * 60;
            }
            else
            {
                h = ((r - g) / d + 4) * 60;
            }

            if (h < 0)
            {
                h += 360;
            }

            var l = (M + m) / 2;

            if (d == 0)
            {
                s = 0;
            }
            else
            {
                s = d / (1 - Math.Abs(2 * l - 1));
            }

            // ReSharper restore CompareOfFloatsByEqualityOperator

            return new AhslColor(a, h / 360, s, l);
        }
    }
}