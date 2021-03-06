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
using System.Drawing;
using System.Windows.Forms;

using Pixelaria.Views.Controls.PaintTools.Interfaces;

namespace Pixelaria.Views.Controls.PaintTools.Abstracts
{
    /// <summary>
    /// Implements basic functionality to paint operations
    /// </summary>
    public abstract class BasePaintTool : IPaintTool
    {
        /// <summary>
        /// The PictureBox owning this PaintOperation
        /// </summary>
        protected ImageEditPanel.InternalPictureBox pictureBox;

        /// <summary>
        /// The cursor to use when hovering over the InternalPictureBox while this operation is up
        /// </summary>
        protected Cursor toolCursor;

        /// <summary>
        /// Gets the cursor to use when hovering over the InternalPictureBox while this operation is up
        /// </summary>
        public Cursor ToolCursor
        {
            get { return toolCursor; }
            protected set { toolCursor = value; }
        }

        /// <summary>
        /// Gets whether this Paint Operation has resources loaded
        /// </summary>
        public virtual bool Loaded { get; protected set; }

        /// <summary>
        /// Delegate for the ColorPicked event
        /// </summary>
        /// <param name="sender">The sender for the event</param>
        /// <param name="args">The arguments for the event</param>
        public delegate void ColorPickedEventHandler(object sender, PaintToolColorPickedEventArgs args);

        /// <summary>
        /// Occurs whenever the user picks a color with this BasePaintTool
        /// </summary>
        public event ColorPickedEventHandler ColorPicked;

        /// <summary>
        /// Initializes this Paint Operation
        /// </summary>
        /// <param name="targetPictureBox">The picture box to initialize the paint operation on</param>
        public virtual void Initialize(ImageEditPanel.InternalPictureBox targetPictureBox)
        {
            pictureBox = targetPictureBox;
        }

        /// <summary>
        /// Finalizes this Paint Operation
        /// </summary>
        public virtual void Destroy(){}

        /// <summary>
        /// Changes the bitmap currently being edited
        /// </summary>
        /// <param name="newBitmap">The new bitmap being edited</param>
        public virtual void ChangeBitmap(Bitmap newBitmap) { }

        /// <summary>
        /// Called to notify this PaintOperation that the control is being redrawn
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void Paint(PaintEventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperation that the control is being redrawn
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void PaintForeground(PaintEventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperation that the mouse is being held down
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void MouseDown(MouseEventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperation that the mouse is being moved
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void MouseMove(MouseEventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperation that the mouse is being released
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void MouseUp(MouseEventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperaiton that the mouse left the image area
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void MouseLeave(EventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperaiton that the mouse entered the image area
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void MouseEnter(EventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperation that a keyboard key was pressed down
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void KeyDown(KeyEventArgs e) { }

        /// <summary>
        /// Called to notify this PaintOperation that a keyboard key was released
        /// </summary>
        /// <param name="e">The event args for this event</param>
        public virtual void KeyUp(KeyEventArgs e) { }

        /// <summary>
        /// Returns whether the given Point is within the image bounds.
        /// The point must be in absolute image position
        /// </summary>
        /// <param name="point">The point to get whether or not it's within the image</param>
        /// <returns>Whether the given point is within the image bounds</returns>
        protected virtual bool WithinBounds(Point point)
        {
            return point.X >= 0 && point.Y >= 0 && point.X < pictureBox.Image.Width && point.Y < pictureBox.Image.Height;
        }

        /// <summary>
        /// Performs a color pick at the specified image coordinates.
        /// If the coordinates at not within the image area, nothing is done
        /// </summary>
        /// <param name="point">The point ot color pick at</param>
        /// <param name="colorIndex">The color index to pick for</param>
        protected void ColorPickAtPoint(Point point, ColorIndex colorIndex)
        {
            if (!WithinBounds(point))
                return;

            ColorPicked?.Invoke(this, new PaintToolColorPickedEventArgs(point, colorIndex));
        }

        /// <summary>
        /// Invalidates a region of the control, with the given coordinates and size.
        /// The region must be in absolute pixels in relation to the image being edited
        /// </summary>
        /// <param name="point">The point to invalidate</param>
        /// <param name="width">The width of the area to invalidate</param>
        /// <param name="height">The height of the area to invalidate</param>
        /// <returns>The rectangle that was invalidated</returns>
        protected virtual Rectangle InvalidateRect(PointF point, float width, float height)
        {
            point = GetRelativePoint(GetAbsolutePoint(point));

            point.X -= 1;
            point.Y -= 1;

            Rectangle rec = new Rectangle((int)point.X, (int)point.Y, (int)(width * pictureBox.Zoom.Y), (int)(height * pictureBox.Zoom.Y));

            pictureBox.Invalidate(rec);

            return rec;
        }

        /// <summary>
        /// Invalidates a region of the control, with the given coordinates and size.
        /// The region must be in absolute pixels in relation to the image being edited
        /// </summary>
        /// <param name="rectangle">The rectangle to invalidate</param>
        /// <returns>The rectangle region of the control that was invalidated</returns>
        protected virtual Rectangle InvalidateRect(Rectangle rectangle)
        {
            // Get the top-left and bottom-right spots of the rectangle, in screen coordinates
            PointF topPoint = GetRelativePoint(rectangle.Location);
            PointF bottomPoint = GetRelativePoint(new Point(rectangle.Right, rectangle.Bottom));

            // Transform the points into rectangles, and create a rectangle that encloses them
            // This effectively transforms the rectangle area from image to control space
            RectangleF topRect = new RectangleF(topPoint, new SizeF(1, 1));
            RectangleF bottomRect = new RectangleF(bottomPoint, new SizeF(1, 1));
            
            RectangleF controlRect = RectangleF.Union(topRect, bottomRect);

            pictureBox.Invalidate(Rectangle.Truncate(controlRect));

            return Rectangle.Truncate(controlRect);
        }

        /// <summary>
        /// Returns the absolute position of the given control point on the canvas image
        /// </summary>
        /// <returns>The absolute position of the given control point on the canvas image</returns>
        protected virtual Point GetAbsolutePoint(PointF point)
        {
            return Point.Truncate(pictureBox.GetAbsolutePoint(point));
        }

        /// <summary>
        /// Returns the relative position of the given canvas point on the control bounds
        /// </summary>
        /// <returns>The relative position of the given canvas point on the control bounds</returns>
        protected virtual PointF GetRelativePoint(Point point)
        {
            return pictureBox.GetRelativePoint(point);
        }

        /// <summary>
        /// Returns a rectangle relative to the control bounds that represents the absolute circle relative to the bitmap
        /// </summary>
        /// <param name="point">The midpoint of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        /// <returns>A rectangle that represents the given circle, relative to the control bounds</returns>
        protected Rectangle GetRelativeCircleBounds(Point point, int radius)
        {
            // Get a rectangle composed of two points that are offset by the size
            Point topLeft = Point.Truncate(GetRelativePoint(Point.Subtract(point, new Size(radius, radius))));
            Point botRight = Point.Truncate(GetRelativePoint(Point.Add(point, new Size(radius, radius))));

            Rectangle rec = new Rectangle(topLeft, (Size)Point.Subtract(botRight, (Size)topLeft));
            return rec;
        }
    }

    /// <summary>
    /// The color index for a paint tool color pick event
    /// </summary>
    public enum ColorIndex
    {
        /// <summary>
        /// The first color
        /// </summary>
        FirstColor,
        /// <summary>
        /// The second color
        /// </summary>
        SecondColor,
        /// <summary>
        /// Indiferent, let the implementer decide the color
        /// </summary>
        Indiferent
    }

    /// <summary>
    /// Event arguments for a BasePaintTool.ColorPicked event
    /// </summary>
    public class PaintToolColorPickedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the point at which the user picked the color at
        /// </summary>
        public Point ImagePoint { get; private set; }

        /// <summary>
        /// Gets the color index for the color pick
        /// </summary>
        public ColorIndex ColorIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the PaintToolColorPickedEventArgs class
        /// </summary>
        /// <param name="imagePoint">The point at which the user picked the color at</param>
        /// <param name="colorIndex">The color index to pick at</param>
        public PaintToolColorPickedEventArgs(Point imagePoint, ColorIndex colorIndex)
        {
            ImagePoint = imagePoint;
            ColorIndex = colorIndex;
        }
    }
}