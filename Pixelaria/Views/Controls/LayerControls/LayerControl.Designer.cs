﻿namespace Pixelaria.Views.Controls.LayerControls
{
    partial class LayerControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LayerControl));
            this.btn_visible = new System.Windows.Forms.Button();
            this.btn_duplicate = new System.Windows.Forms.Button();
            this.btn_locked = new System.Windows.Forms.Button();
            this.btn_remove = new System.Windows.Forms.Button();
            this.lbl_layerName = new System.Windows.Forms.Label();
            this.pb_layerImage = new Pixelaria.Views.Controls.ZoomablePictureBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pb_layerImage)).BeginInit();
            this.SuspendLayout();
            // 
            // btn_visible
            // 
            this.btn_visible.FlatAppearance.BorderSize = 0;
            this.btn_visible.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ScrollBar;
            this.btn_visible.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
            this.btn_visible.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_visible.Image = global::Pixelaria.Properties.Resources.filter_enable_icon;
            this.btn_visible.Location = new System.Drawing.Point(3, 19);
            this.btn_visible.Name = "btn_visible";
            this.btn_visible.Size = new System.Drawing.Size(18, 18);
            this.btn_visible.TabIndex = 4;
            this.toolTip1.SetToolTip(this.btn_visible, "Switch visibility");
            this.btn_visible.UseVisualStyleBackColor = true;
            this.btn_visible.Click += new System.EventHandler(this.btn_visible_Click);
            // 
            // btn_duplicate
            // 
            this.btn_duplicate.FlatAppearance.BorderSize = 0;
            this.btn_duplicate.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ScrollBar;
            this.btn_duplicate.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
            this.btn_duplicate.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_duplicate.Image = global::Pixelaria.Properties.Resources.edit_copy;
            this.btn_duplicate.Location = new System.Drawing.Point(3, 61);
            this.btn_duplicate.Name = "btn_duplicate";
            this.btn_duplicate.Size = new System.Drawing.Size(18, 18);
            this.btn_duplicate.TabIndex = 3;
            this.toolTip1.SetToolTip(this.btn_duplicate, "Duplicate layer");
            this.btn_duplicate.UseVisualStyleBackColor = true;
            this.btn_duplicate.Click += new System.EventHandler(this.btn_duplicate_Click);
            // 
            // btn_locked
            // 
            this.btn_locked.FlatAppearance.BorderSize = 0;
            this.btn_locked.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ScrollBar;
            this.btn_locked.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
            this.btn_locked.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_locked.Image = global::Pixelaria.Properties.Resources.padlock_closed;
            this.btn_locked.Location = new System.Drawing.Point(3, 40);
            this.btn_locked.Name = "btn_locked";
            this.btn_locked.Size = new System.Drawing.Size(18, 18);
            this.btn_locked.TabIndex = 5;
            this.toolTip1.SetToolTip(this.btn_locked, "Lock/unlock layer");
            this.btn_locked.UseVisualStyleBackColor = true;
            this.btn_locked.Click += new System.EventHandler(this.btn_locked_Click);
            // 
            // btn_remove
            // 
            this.btn_remove.FlatAppearance.BorderSize = 0;
            this.btn_remove.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ScrollBar;
            this.btn_remove.FlatAppearance.MouseOverBackColor = System.Drawing.Color.White;
            this.btn_remove.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btn_remove.Image = global::Pixelaria.Properties.Resources.action_delete;
            this.btn_remove.Location = new System.Drawing.Point(3, 82);
            this.btn_remove.Name = "btn_remove";
            this.btn_remove.Size = new System.Drawing.Size(18, 18);
            this.btn_remove.TabIndex = 6;
            this.toolTip1.SetToolTip(this.btn_remove, "Remove layer");
            this.btn_remove.UseVisualStyleBackColor = true;
            this.btn_remove.Click += new System.EventHandler(this.btn_remove_Click);
            // 
            // lbl_layerName
            // 
            this.lbl_layerName.Location = new System.Drawing.Point(24, 3);
            this.lbl_layerName.Name = "lbl_layerName";
            this.lbl_layerName.Size = new System.Drawing.Size(96, 13);
            this.lbl_layerName.TabIndex = 8;
            this.lbl_layerName.Text = "Layer 1";
            this.lbl_layerName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pb_layerImage
            // 
            this.pb_layerImage.AllowDrag = false;
            this.pb_layerImage.AllowScrollbars = false;
            this.pb_layerImage.BackgroundImage = global::Pixelaria.Properties.Resources.checkers_pattern;
            this.pb_layerImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pb_layerImage.ClipBackgroundToImage = true;
            this.pb_layerImage.ImageInterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            this.pb_layerImage.ImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pb_layerImage.Location = new System.Drawing.Point(24, 19);
            this.pb_layerImage.MaximumZoom = ((System.Drawing.PointF)(resources.GetObject("pb_layerImage.MaximumZoom")));
            this.pb_layerImage.MinimumZoom = ((System.Drawing.PointF)(resources.GetObject("pb_layerImage.MinimumZoom")));
            this.pb_layerImage.Name = "pb_layerImage";
            this.pb_layerImage.Size = new System.Drawing.Size(96, 81);
            this.pb_layerImage.TabIndex = 7;
            this.pb_layerImage.TabStop = false;
            this.pb_layerImage.Zoom = ((System.Drawing.PointF)(resources.GetObject("pb_layerImage.Zoom")));
            this.pb_layerImage.ZoomFactor = 1.414214F;
            this.pb_layerImage.Click += new System.EventHandler(this.pb_layerImage_Click);
            this.pb_layerImage.MouseDown += new System.Windows.Forms.MouseEventHandler(this.pb_layerImage_MouseDown);
            this.pb_layerImage.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pb_layerImage_MouseMove);
            this.pb_layerImage.MouseUp += new System.Windows.Forms.MouseEventHandler(this.pb_layerImage_MouseUp);
            // 
            // LayerControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lbl_layerName);
            this.Controls.Add(this.pb_layerImage);
            this.Controls.Add(this.btn_remove);
            this.Controls.Add(this.btn_locked);
            this.Controls.Add(this.btn_visible);
            this.Controls.Add(this.btn_duplicate);
            this.DoubleBuffered = true;
            this.Name = "LayerControl";
            this.Size = new System.Drawing.Size(125, 105);
            ((System.ComponentModel.ISupportInitialize)(this.pb_layerImage)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btn_visible;
        private System.Windows.Forms.Button btn_duplicate;
        private System.Windows.Forms.Button btn_locked;
        private System.Windows.Forms.Button btn_remove;
        private ZoomablePictureBox pb_layerImage;
        private System.Windows.Forms.Label lbl_layerName;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}