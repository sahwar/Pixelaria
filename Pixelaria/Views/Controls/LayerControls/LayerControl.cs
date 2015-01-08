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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Pixelaria.Data;
using Pixelaria.Utils;

namespace Pixelaria.Views.Controls.LayerControls
{
    /// <summary>
    /// Specifies a visual representation of a layer in which the user can interact with in order to manage a frame's layers
    /// </summary>
    [DefaultEvent("LayerStatusChanged")]
    public partial class LayerControl : UserControl
    {
        /// <summary>
        /// The layer this layer control is binded to
        /// </summary>
        private readonly IFrameLayer _layer;

        /// <summary>
        /// Whether the user is currently dragging the layer around
        /// </summary>
        private bool _draggingLayer;

        /// <summary>
        /// Whether the user is currently pressing down on the layer bitmap
        /// </summary>
        private bool _pressingLayer;

        /// <summary>
        /// Specifies the point where the player pressed down on the layer's image
        /// </summary>
        private Point _layerPressPoint;

        /// <summary>
        /// Whether the layer being displayed is currently visible
        /// </summary>
        private bool _layerVisible;

        /// <summary>
        /// Whether the layer being displayed is currently locked
        /// </summary>
        private bool _layerLocked;

        /// <summary>
        /// Gets or sets a value specifying whether the layer is visible
        /// </summary>
        public bool LayerVisible
        {
            get { return _layerVisible; }
            set
            {
                if (_layerVisible == value)
                    return;

                _layerVisible = value;

                UpdateDisplay();

                if (LayerStatusChanged != null)
                {
                    LayerStatusChanged(this, new LayerControlStatusChangedEventArgs(LayerStatus));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value specifying whether the layer is visible
        /// </summary>
        public bool LayerLocked
        {
            get { return _layerLocked; }
            set
            {
                if (_layerLocked == value)
                    return;

                _layerLocked = value;

                UpdateDisplay();

                if (LayerStatusChanged != null)
                {
                    LayerStatusChanged(this, new LayerControlStatusChangedEventArgs(LayerStatus));
                }
            }
        }

        /// <summary>
        /// Gets the display status for this layer control
        /// </summary>
        public LayerStatus LayerStatus
        {
            get
            {
                return new LayerStatus(_layerVisible, _layerLocked);
            }
        }

        /// <summary>
        /// Gets the layer this layer control is binded to
        /// </summary>
        public IFrameLayer Layer
        {
            get { return _layer; }
        }

        /// <summary>
        /// The delegate for the LayerStatusChanged event
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">The event args for the event</param>
        public delegate void LayerStatusChangedEventHandler(object sender, LayerControlStatusChangedEventArgs args);

        /// <summary>
        /// The event fired whenever the status of the layer currently displayed is changed by the user
        /// </summary>
        [Browsable(true)]
        [Description("The event fired whenever the status of the layer currently displayed is changed by the user")]
        public event LayerStatusChangedEventHandler LayerStatusChanged;

        /// <summary>
        /// Occurs whenever the user clicks the Duplicate Layer button
        /// </summary>
        public event EventHandler DuplicateLayerSelected;

        /// <summary>
        /// Occurs whenever the user clicks the Remove Layer button
        /// </summary>
        public event EventHandler RemoveLayerSelected;

        /// <summary>
        /// Delegate for the LayerSelected event
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="layer">The layer that was selected</param>
        public delegate void LayerSelectedEventHandler(object sender, LayerControl layer);

        /// <summary>
        /// Event called whenever the user selects the layer
        /// </summary>
        public event LayerSelectedEventHandler LayerSelected;

        /// <summary>
        /// Delegate for the LayerControlDragged event
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="args">The event arguments for the event</param>
        public delegate void LayerControlDraggedEventHandler(object sender, LayerControlDragEventArgs args);

        /// <summary>
        /// Occurs whenever the user drags the layer in order to swap it with another layer up or down
        /// </summary>
        public event LayerControlDraggedEventHandler LayerControlDragged;

        /// <summary>
        /// Occurs whenever the user presses on the layer image area
        /// </summary>
        public event MouseEventHandler LayerImagePressed;

        /// <summary>
        /// Occurs whenever the user releases the layer image area
        /// </summary>
        public event MouseEventHandler LayerImageReleased;

        /// <summary>
        /// Initializes a new instance of the LayerControl class
        /// </summary>
        /// <param name="layer">The layer this control will bind to</param>
        public LayerControl(IFrameLayer layer)
        {
            InitializeComponent();
            _layer = layer;

            // Update startup values
            _layerVisible = true;
            _layerLocked = false;

            UpdateDisplay();
        }

        /// <summary>
        /// Updates the display of the current layer
        /// </summary>
        public void UpdateDisplay()
        {
            UpdateBitmapDisplay();

            lbl_layerName.Text = @"Layer " + (_layer.Index + 1);

            btn_visible.Image = _layerVisible ? Properties.Resources.filter_enable_icon : Properties.Resources.filter_disable_icon;
            btn_locked.Image = _layerLocked ? Properties.Resources.padlock_closed : Properties.Resources.padlock_open;
        }

        /// <summary>
        /// Updates the bitmap display for the layers
        /// </summary>
        public void UpdateBitmapDisplay()
        {
            pb_layerImage.Image = _layer.LayerBitmap;
            pb_layerImage.Invalidate();
        }

        // 
        // OnPaintBackground event handler
        // 
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            
            // During a drag operation, draw a marquee around the control
            if(_draggingLayer)
            {
                Pen p = new Pen(Color.Black)
                {
                    DashStyle = DashStyle.Dash,
                    DashPattern = new[] { 2f, 2f },
                    Alignment = PenAlignment.Inset,
                    Width = 1
                };

                Rectangle rec = new Rectangle(Point.Empty, new Size(Width - 1, Height - 1));
                e.Graphics.DrawRectangle(p, rec);
            }
        }

        // 
        // Layer Visible button
        // 
        private void btn_visible_Click(object sender, EventArgs e)
        {
            LayerVisible = !LayerVisible;
        }

        // 
        // Layer Locked button
        // 
        private void btn_locked_Click(object sender, EventArgs e)
        {
            LayerLocked = !LayerLocked;
        }

        // 
        // Duplicate Layer button click
        // 
        private void btn_duplicate_Click(object sender, EventArgs e)
        {
            if (DuplicateLayerSelected != null)
            {
                DuplicateLayerSelected(this, new EventArgs());
            }
        }

        // 
        // Remove Layer button click
        // 
        private void btn_remove_Click(object sender, EventArgs e)
        {
            if (RemoveLayerSelected != null)
            {
                RemoveLayerSelected(this, new EventArgs());
            }
        }

        // 
        // Layer Image picture box click
        // 
        private void pb_layerImage_Click(object sender, EventArgs e)
        {
            if (!_draggingLayer && LayerSelected != null)
                LayerSelected(this, this);
        }

        // 
        // Layer Image picture box mouse down
        // 
        private void pb_layerImage_MouseDown(object sender, MouseEventArgs e)
        {
            _layerPressPoint = e.Location;
            _pressingLayer = true;

            if (LayerImagePressed != null)
            {
                LayerImagePressed(this, e);
            }
        }

        // 
        // Layer Image picture box mouse move
        // 
        private void pb_layerImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (_pressingLayer)
            {
                if (_layerPressPoint.Distance(e.Location) > 20)
                {
                    _draggingLayer = true;
                    Invalidate();
                }

                if (_draggingLayer)
                {
                    if (e.Location.Y < -pb_layerImage.Location.Y - 5)
                    {
                        if (LayerControlDragged != null)
                        {
                            LayerControlDragged(this, new LayerControlDragEventArgs(LayerDragDirection.Up));
                        }
                    }
                    else if (e.Location.Y - pb_layerImage.Location.Y > Height + 5)
                    {
                        if (LayerControlDragged != null)
                        {
                            LayerControlDragged(this, new LayerControlDragEventArgs(LayerDragDirection.Down));
                        }
                    }
                }

                Debug.WriteLine(e.Location);
            }
        }

        // 
        // Layer Image picture box mouse up
        // 
        private void pb_layerImage_MouseUp(object sender, MouseEventArgs e)
        {
            _draggingLayer = false;
            _pressingLayer = false;

            if (LayerImageReleased != null)
            {
                LayerImageReleased(this, e);
                Invalidate();
            }
        }
    }

    /// <summary>
    /// Represents the event arguments for the LayerStatusChanged event
    /// </summary>
    public class LayerControlStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the Status for the layer control
        /// </summary>
        public LayerStatus Status { get; private set; }

        /// <summary>
        /// Initializes a new LayerControlStatusChangedEventArgs class
        /// </summary>
        /// <param name="status">The status for this event args object</param>
        public LayerControlStatusChangedEventArgs(LayerStatus status)
        {
            Status = status;
        }
    }

    /// <summary>
    /// Represents the event arguments for the LayerControlDragged event
    /// </summary>
    public class LayerControlDragEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the direction of the drag
        /// </summary>
        public LayerDragDirection DragDirection { get; private set; }

        /// <summary>
        /// Initializes a new instance of the LayerControlDragEventArgs class
        /// </summary>
        /// <param name="dragDirection">The direction of the drag</param>
        public LayerControlDragEventArgs(LayerDragDirection dragDirection)
        {
            DragDirection = dragDirection;
        }
    }

    /// <summary>
    /// Represents the display status of a layer on a layer control
    /// </summary>
    public struct LayerStatus
    {
        /// <summary>
        /// Whether the layer is visible
        /// </summary>
        public readonly bool Visible;

        /// <summary>
        /// Whetehr the layer is locked
        /// </summary>
        public readonly bool Locked;

        /// <summary>
        /// Creates a new LayerStatus struct
        /// </summary>
        /// <param name="visible">Whether the layer is currently visible</param>
        /// <param name="locked">Whether the layer is currently locked</param>
        public LayerStatus(bool visible, bool locked)
        {
            Visible = visible;
            Locked = locked;
        }
    }

    /// <summary>
    /// Specifies the direction of the drag for a layer
    /// </summary>
    public enum LayerDragDirection
    {
        /// <summary>
        /// Specifies that the direction dragged was upwards
        /// </summary>
        Up,
        /// <summary>
        /// Specifies that the direction dragged was downards
        /// </summary>
        Down
    }
}