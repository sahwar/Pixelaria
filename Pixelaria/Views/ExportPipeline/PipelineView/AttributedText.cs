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
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

using JetBrains.Annotations;

namespace Pixelaria.Views.ExportPipeline.PipelineView
{
    public interface IAttributedText
    {
        /// <summary>
        /// Gets the length of this attribute text's text string
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Gets the entire text segment buffer on this attributed text
        /// </summary>
        string String { get; }

        /// <summary>
        /// Returns true if there are attributes on this attributed text
        /// </summary>
        bool HasAttributes { get; }

        /// <summary>
        /// Resets the properties of this attributed text and sets the given string
        /// as its contents.
        /// </summary>
        void SetText([NotNull] string text);

        /// <summary>
        /// Resets the properties of this attributed text and sets the given IAttributedText
        /// as its contents.
        /// </summary>
        void SetText([NotNull] IAttributedText attributedText);

        /// <summary>
        /// Appends a string this attributed text object
        /// </summary>
        void Append([NotNull] string text);

        /// <summary>
        /// Appends another IAttributedText with a given set of attributes to this attributed text object
        /// </summary>
        void Append([NotNull] IAttributedText text);

        /// <summary>
        /// Appends a string with a given attribute to this attributed text object
        /// </summary>
        void Append([NotNull] string text, [NotNull] ITextAttribute attribute);

        /// <summary>
        /// Appends a string with a given set of attributes to this attributed text object
        /// </summary>
        void Append([NotNull] string text, [NotNull] ITextAttribute[] attributes);
        
        /// <summary>
        /// Clears all text and attributes on this <see cref="AttributedText"/>
        /// </summary>
        void Clear();

        /// <summary>
        /// Gets the individual text segments on this attributed text
        /// </summary>
        ITextSegment[] GetTextSegments();
    }

    /// <summary>
    /// A structure to contain a text and associated attribute strings
    /// </summary>
    public sealed class AttributedText : IEquatable<AttributedText>, ICloneable, IAttributedText
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private readonly List<TextSegment> _segments = new List<TextSegment>();

        /// <summary>
        /// Called every time the string content of this attributed text changes
        /// </summary>
        public EventHandler Modified;
        
        public int Length => _stringBuilder.Length;
        
        public string String => _stringBuilder.ToString();
        
        public bool HasAttributes => _segments.Any(s => s.TextAttributes.Length > 0);

        public AttributedText()
        {

        }

        public AttributedText([NotNull] string text)
        {
            SetText(text);
        }

        public AttributedText([NotNull] string text, [NotNull] ITextAttribute[] attributes)
        {
            Append(text, attributes);
        }

        public AttributedText([NotNull] string text, [NotNull] ITextAttribute attribute)
        {
            Append(text, attribute);
        }

        public void SetText(string text)
        {
            _segments.Clear();
            _stringBuilder.Clear();
            Append(text, new ITextAttribute[0]);
        }

        public void SetText(IAttributedText attributedText)
        {
            _segments.Clear();
            _stringBuilder.Clear();

            Append(attributedText);
        }

        public void Append(string text)
        {
            Append(text, new ITextAttribute[0]);
        }

        public void Append(string text, ITextAttribute[] attributes)
        {
            var segment = new TextSegment(text, attributes, new TextRange(_stringBuilder.Length, text.Length));

            _segments.Add(segment);
            _stringBuilder.Append(text);

            CallModifiedEvent();
        }

        public void Append(string text, ITextAttribute attribute)
        {
            Append(text, new[] { attribute });
        }

        /// <summary>
        /// Appends another attributed text's contents into this attributed text.
        /// </summary>
        public void Append(IAttributedText attributedText)
        {
            // Small time saver: Avoid going through computed properties if the other
            // object is also an AttributedText instance
            if (attributedText is AttributedText attr)
            {
                // Adjust segments' text rages
                int newLimit = _stringBuilder.Length;

                _segments.AddRange(
                    attr._segments.Select(seg =>
                        new TextSegment(seg.Text, seg.TextAttributes,
                            new TextRange(seg.TextRange.Start + newLimit, seg.TextRange.Length)
                        )
                    ));

                _stringBuilder.Append(attr._stringBuilder);
            }
            else
            {
                // Adjust segments' text rages
                int newLimit = _stringBuilder.Length;

                _segments.AddRange(
                    attributedText.GetTextSegments().Select(seg =>
                        new TextSegment(seg.Text, seg.TextAttributes,
                            new TextRange(seg.TextRange.Start + newLimit, seg.TextRange.Length)
                        )
                    ));

                _stringBuilder.Append(attributedText.String);
            }
            
            CallModifiedEvent();
        }

        public void Clear()
        {
            _segments.Clear();
            _stringBuilder.Clear();

            CallModifiedEvent();
        }

        public ITextSegment[] GetTextSegments()
        {
            return _segments.OfType<ITextSegment>().ToArray();
        }

        public bool Equals(AttributedText other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _segments.SequenceEqual(other._segments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AttributedText)obj);
        }

        public override int GetHashCode()
        {
            return _segments != null ? _segments.GetHashCode() : 0;
        }

        public object Clone()
        {
            var text = new AttributedText();

            text._segments.AddRange(_segments);
            text._stringBuilder.Append(_stringBuilder);

            return text;
        }

        public static explicit operator AttributedText([NotNull] string value)
        {
            return new AttributedText(value);
        }

        public static AttributedText operator +([NotNull] AttributedText lhs, [NotNull] AttributedText rhs)
        {
            var copyLeft = (AttributedText)lhs.Clone();
            copyLeft.Append(rhs);

            return copyLeft;
        }

        public static AttributedText operator +([NotNull] AttributedText lhs, [NotNull] string rhs)
        {
            var copyLeft = (AttributedText)lhs.Clone();
            copyLeft.Append(rhs);

            return copyLeft;
        }

        private void CallModifiedEvent()
        {
            Modified?.Invoke(this, EventArgs.Empty);
        }

        private struct TextSegment : ITextSegment, IEquatable<TextSegment>
        {
            public string Text { get; }
            public ITextAttribute[] TextAttributes { get; }
            public TextRange TextRange { get; }

            public TextSegment(string text, [NotNull] ITextAttribute[] textAttributes, TextRange textRange) : this()
            {
                Text = text;
                TextAttributes = textAttributes;
                TextRange = textRange;
            }

            public bool HasAttribute<T>() where T : ITextAttribute
            {
                return TextAttributes.Any(t => t is T);
            }

            public T GetAttribute<T>() where T : ITextAttribute
            {
                return TextAttributes.OfType<T>().FirstOrDefault();
            }

            public bool Equals(TextSegment other)
            {
                return string.Equals(Text, other.Text) && TextAttributes.Equals(other.TextAttributes) && TextRange.Equals(other.TextRange);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is TextSegment && Equals((TextSegment)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = (Text != null ? Text.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ TextAttributes.GetHashCode();
                    hashCode = (hashCode * 397) ^ TextRange.GetHashCode();
                    return hashCode;
                }
            }

            public object Clone()
            {
                return new TextSegment(Text, TextAttributes, TextRange);
            }
        }
    }

    /// <summary>
    /// Specifies attributes for a text segment
    /// </summary>
    public interface ITextAttribute : ICloneable
    {

    }

    /// <summary>
    /// A text segment with attributes
    /// </summary>
    public interface ITextSegment : ICloneable
    {
        string Text { get; }
        [NotNull]
        ITextAttribute[] TextAttributes { get; }
        TextRange TextRange { get; }

        /// <summary>
        /// Returns whether a given text attribute is applied to this text segment.
        /// </summary>
        bool HasAttribute<T>() where T : ITextAttribute;

        /// <summary>
        /// Gets an attribute with a given type on the text attributes array.
        /// Returns null, if attribute type is not present.
        /// </summary>
        [CanBeNull]
        T GetAttribute<T>() where T : ITextAttribute;
    }

    [DebuggerDisplay("{Start} , {Length}")]
    public struct TextRange : IEquatable<TextRange>
    {
        public TextRange(int start, int length) : this()
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }

        /// <summary>
        /// End of range; Equivalent to <see cref="Start"/> + <see cref="Length"/>.
        /// </summary>
        public int End => Start + Length;

        public bool Equals(TextRange other)
        {
            return Start == other.Start && Length == other.Length;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TextRange && Equals((TextRange)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Start * 397) ^ Length;
            }
        }

        public override string ToString()
        {
            return $"[ {Start} : {Length} ]";
        }

        /// <summary>
        /// Returns whether this range overlaps another range.
        /// </summary>
        public bool Intersects(TextRange other)
        {
            return Start <= other.End && other.Start <= End;
        }

        /// <summary>
        /// Returns the intersection between this text range and another text range.
        /// 
        /// Intersection between the two ranges must have at least length 1 to be valid.
        /// 
        /// Returns null, if the ranges do not intersect.
        /// </summary>
        public TextRange? Intersection(TextRange other)
        {
            // Not intersecting
            if (End <= other.Start || other.End <= Start)
                return null;

            int start = Math.Max(Start, other.Start);
            int end = Math.Min(End, other.End);

            return new TextRange(start, end - start);
        }

        /// <summary>
        /// Returns the overlap between this text range and another text range.
        /// 
        /// Overlap between the two ranges can occur with 0-length overlaps; final overlap will
        /// also have 0 <see cref="Length"/>.
        /// 
        /// Returns null, if the ranges do not overlap.
        /// </summary>
        public TextRange? Overlap(TextRange other)
        {
            // Not intersecting
            if (End < other.Start || other.End < Start)
                return null;

            int start = Math.Max(Start, other.Start);
            int end = Math.Min(End, other.End);

            return new TextRange(start, end - start);
        }

        /// <summary>
        /// Given two string offsets, returns a <see cref="TextRange"/> where <see cref="Start"/> is
        /// the minimum of the two offsets, and <see cref="Length"/> is the different between the two
        /// offsets.
        /// </summary>
        public static TextRange FromOffsets(int offset1, int offset2)
        {
            int min = Math.Min(offset1, offset2);
            int max = Math.Max(offset1, offset2);

            return new TextRange(min, max - min);
        }
    }

    public struct ForegroundColorAttribute : ITextAttribute, IEquatable<ForegroundColorAttribute>
    {
        public Color ForeColor { get; }

        public ForegroundColorAttribute(Color foreColor)
        {
            ForeColor = foreColor;
        }

        public bool Equals(ForegroundColorAttribute other)
        {
            return ForeColor.Equals(other.ForeColor);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ForegroundColorAttribute && Equals((ForegroundColorAttribute)obj);
        }

        public override int GetHashCode()
        {
            return ForeColor.GetHashCode();
        }

        public object Clone()
        {
            return new ForegroundColorAttribute(ForeColor);
        }
    }

    public struct TextFontAttribute : ITextAttribute, IEquatable<TextFontAttribute>
    {
        public Font Font { get; }

        public TextFontAttribute(Font font)
        {
            Font = font;
        }

        public bool Equals(TextFontAttribute other)
        {
            return Font.Equals(other.Font);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TextFontAttribute && Equals((TextFontAttribute)obj);
        }

        public override int GetHashCode()
        {
            return Font.GetHashCode();
        }

        public object Clone()
        {
            return new TextFontAttribute(Font);
        }
    }
}