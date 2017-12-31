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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Annotations;
using Pixelaria.Data;
using Pixelaria.Data.Exports;

using Pixelaria.Controllers;
using Pixelaria.Localization;
using Pixelaria.Properties;
using Pixelaria.Utils;
using Pixelaria.Views.Controls;
using Pixelaria.Views.Controls.PaintTools;

namespace Pixelaria.Views.ModelViews
{
    /// <summary>
    /// Form used as interface for creating a new Animation Sheet
    /// </summary>
    public partial class AnimationSheetView : ModifiableContentView
    {
        private CompositeDisposable _disposeBag = new CompositeDisposable();
        private readonly Reactive _reactive = new Reactive();

        private readonly SheetPreviewHoverMouseTool _sheetPreviewTool;

        public IReactive Rx => _reactive;

        /// <summary>
        /// The controller that owns this form
        /// </summary>
        private readonly Controller _controller;
        
        /// <summary>
        /// The current export settings
        /// </summary>
        private AnimationSheetExportSettings _sheetExportSettings;

        /// <summary>
        /// The current bundle sheet export
        /// </summary>
        private BundleSheetExport _bundleSheetExport;

        /// <summary>
        /// Whether the current sheet export was generated with data from animations that where unsaved at the time
        /// </summary>
        private bool _generatedWhileUnsaved;

        /// <summary>
        /// Cancellation token for the sheet generation routine
        /// </summary>
        private CancellationTokenSource _sheetCancellation;

        /// <summary>
        /// Whether to automatically update the preview whenever changes to animations for a sprite sheet occur
        /// </summary>
        private bool _autoUpdatePreview = true;

        /// <summary>
        /// Gets the current AnimationSheet being edited
        /// </summary>
        public AnimationSheet CurrentSheet { get; }

        /// <summary>
        /// Initializes a new instance of the AnimationSheetEditor class
        /// </summary>
        /// <param name="controller">The controller that owns this form</param>
        /// <param name="sheetToEdit">The sheet to edit on this form. Leave null to show an interface to create a new sheet</param>
        public AnimationSheetView(Controller controller, AnimationSheet sheetToEdit = null)
        {
            InitializeComponent();
            
            _controller = controller;
            CurrentSheet = sheetToEdit;

            if (sheetToEdit != null)
                _sheetExportSettings = sheetToEdit.SheetExportSettings;

            InitializeFiends();
            ValidateFields();
            
            if(sheetToEdit != null)
            {
                // TODO: Better abstract and decouple these references to AnimationView, which are, as of now, a hack.

                // Setup events
                controller.ViewModifiedChanged += OnControllerOnViewModifiedChanged;
                controller.ViewOpenedClosed += OnControllerOnViewOpenedClosed;
            }

            zpb_sheetPreview.PictureBox.SetBitmap(new Bitmap(32, 32));
            zpb_sheetPreview.PictureBox.PanMode = PictureBoxPanMode.LeftMouseDrag;
            zpb_sheetPreview.Init();

            _sheetPreviewTool = new SheetPreviewHoverMouseTool();

            zpb_sheetPreview.CurrentPaintTool = _sheetPreviewTool;

            zpb_sheetPreview.PictureBox.ZoomChanged += zpb_sheetPreview_ZoomChanged;
            _sheetPreviewTool.FrameBoundsMouseClicked += sppb_clickedFrameRect;
            
            _sheetPreviewTool.AllowMouseHover = true;

            CreateObservers();
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        [SuppressMessage("ReSharper", "UseNullPropagation")]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reactive.Dispose();

                if (components != null)
                    components.Dispose();
                
                if (_sheetCancellation != null)
                    _sheetCancellation.Dispose();

                _controller.ViewModifiedChanged -= OnControllerOnViewModifiedChanged;
                _controller.ViewOpenedClosed -= OnControllerOnViewOpenedClosed;
                
                if (_disposeBag != null)
                    _disposeBag.Dispose();
                _disposeBag = null;
            }

            base.Dispose(disposing);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _disposeBag?.Dispose();
            _disposeBag = null;
        }

        /// <summary>
        /// Initialize the reactive components for this form
        /// </summary>
        public void CreateObservers()
        {
            // Listen to changes to animations for this sheet view

            // Listen to animation add/remove from the sheet we are displaying
            var onUpdateSheet =
                _controller
                    .Rx.AnimationSheetUpdate.Select(_ => Unit.Default);

            // Listen to non persisted updates of children Animation views
            // that handle animations that belong to this sheet view
            var onUpdateAnimationViews =
                _controller
                    .MainForm.Rx
                    .MdiChildrenChanged
                    // Listen to all animation views- every time one is opened
                    // or closed. We then select their respective OnChange listeners.
                    .Select(children => children.OfType<AnimationView>())
                    .SelectMany(children =>
                    {
                        return children.Select(
                            view => view.Rx.Change.Select(_ => view)
                        ).Merge();
                    })
                    // Select only animations that belong to the animation sheet view being edited.
                    .Where(form => _controller.GetOwningAnimationSheet(form.CurrentAnimation)?.ID == CurrentSheet?.ID)
                    // Don't care about animation views, only that one of them was updated.
                    .Select(_ => Unit.Default);

            Observable
                .Merge(onUpdateSheet, onUpdateAnimationViews, Rx.Change)
                .Throttle(TimeSpan.FromMilliseconds(16))
                .ObserveOn(this)
                .Subscribe(next =>
                {
                    _sheetCancellation?.Cancel();

                    if (_autoUpdatePreview)
                        GeneratePreview();
                }).AddToDisposable(_disposeBag);
        }

        /// <summary>
        /// Initializes the fields of this form
        /// </summary>
        public void InitializeFiends()
        {
            // If no sheet is present, disable sheet preview
            if (CurrentSheet == null)
            {
                btn_generatePreview.Visible = false;
                lbl_sheetPreview.Visible = false;
                zpb_sheetPreview.Visible = false;

                gb_sheetInfo.Visible = false;
                gb_exportSummary.Visible = false;
                
                anud_zoom.Visible = false;
                pb_zoomIcon.Visible = false;

                cb_showFrameBounds.Visible = false;
                cb_showReuseCount.Visible = false;
                btn_apply.Enabled = false;

                Text = AnimationMessages.TextNewAnimationSheet;
                
                txt_sheetName.Text = _controller.GetUniqueUntitledAnimationSheetName();

                return;
            }

            txt_sheetName.Text = CurrentSheet.Name;

            cb_favorRatioOverArea.Checked = CurrentSheet.SheetExportSettings.FavorRatioOverArea;
            cb_forcePowerOfTwoDimensions.Checked = CurrentSheet.SheetExportSettings.ForcePowerOfTwoDimensions;
            cb_forceMinimumDimensions.Checked = CurrentSheet.SheetExportSettings.ForceMinimumDimensions;
            cb_reuseIdenticalFrames.Checked = CurrentSheet.SheetExportSettings.ReuseIdenticalFramesArea;
            cb_highPrecision.Checked = CurrentSheet.SheetExportSettings.HighPrecisionAreaMatching;
            cb_allowUordering.Checked = CurrentSheet.SheetExportSettings.AllowUnorderedFrames;
            cb_useUniformGrid.Checked = CurrentSheet.SheetExportSettings.UseUniformGrid;
            cb_padFramesOnJson.Checked = CurrentSheet.SheetExportSettings.UsePaddingOnJson;
            cb_exportJson.Checked = CurrentSheet.SheetExportSettings.ExportJson;
            nud_xPadding.Value = CurrentSheet.SheetExportSettings.XPadding;
            nud_yPadding.Value = CurrentSheet.SheetExportSettings.YPadding;

            zpb_sheetPreview.PictureBox.MaximumZoom = new PointF(100, 100);

            modified = false;

            Text = AnimationMessages.TextAnimationSheet + @" [" + CurrentSheet.Name + @"]";

            UpdateCountLabels();
        }

        /// <summary>
        /// Updates the animation and frame count labels 
        /// </summary>
        public void UpdateCountLabels()
        {
            lbl_animCount.Text = CurrentSheet.AnimationCount + "";
            lbl_frameCount.Text = CurrentSheet.GetFrameCount() + "";
        }

        /// <summary>
        /// Validates the fields from this form, and disables the saving of changes if one or more of the fields is invalid
        /// </summary>
        /// <returns>Whether the validation was successful</returns>
        public bool ValidateFields()
        {
            bool valid = true;
            const bool alert = false;

            // Animation name
            var validation = _controller.AnimationSheetValidator.ValidateAnimationSheetName(txt_sheetName.Text, CurrentSheet);
            if (validation != "")
            {
                txt_sheetName.BackColor = Color.LightPink;
                lbl_error.Text = validation;
                valid = false;
            }
            else
            {
                txt_sheetName.BackColor = Color.White;
            }

            pnl_errorPanel.Visible = !valid;
            pnl_alertPanel.Visible = alert;

            btn_ok.Enabled = valid;
            btn_apply.Enabled = (valid && modified && CurrentSheet != null);

            return valid;
        }

        /// <summary>
        /// Applies the changes made by this form to the affected objects
        /// </summary>
        public override void ApplyChanges()
        {
            if(!ValidateFields())
                return;

            CurrentSheet.Name = txt_sheetName.Text;
            CurrentSheet.SheetExportSettings = RepopulateExportSettings();

            _controller.UpdatedAnimationSheet(CurrentSheet);

            Text = AnimationMessages.TextAnimationSheet + @" [" + CurrentSheet.Name + @"]";
            btn_apply.Enabled = false;

            base.ApplyChanges();
        }

        /// <summary>
        /// Displays a confirmation to the user when changes have been made to the animation sheet.
        /// If the view is opened as a creation view, no confirmation is displayed to the user in any way
        /// </summary>
        /// <returns>The DialogResult of the confirmation MessageBox displayed to the user</returns>
        public override DialogResult ConfirmChanges()
        {
            if (CurrentSheet != null)
            {
                return base.ConfirmChanges();
            }

            return DialogResult.OK;
        }

        /// <summary>
        /// Marks the contents of this view as Modified
        /// </summary>
        public override void MarkModified()
        {
            if (CurrentSheet != null)
            {
                Text = AnimationMessages.TextAnimationSheet + @" [" + CurrentSheet.Name + @"]*";
                btn_apply.Enabled = true;
            }

            base.MarkModified();
        }

        /// <summary>
        /// Repopulates the AnimationExportSettings field of this form with the form's fields and returns it
        /// </summary>
        /// <returns>The newly repopulated AnimationExportSettings</returns>
        public AnimationSheetExportSettings RepopulateExportSettings()
        {
            _sheetExportSettings.FavorRatioOverArea = cb_favorRatioOverArea.Checked;
            _sheetExportSettings.ForcePowerOfTwoDimensions = cb_forcePowerOfTwoDimensions.Checked;
            _sheetExportSettings.ForceMinimumDimensions = cb_forceMinimumDimensions.Checked;
            _sheetExportSettings.ReuseIdenticalFramesArea = cb_reuseIdenticalFrames.Checked;
            _sheetExportSettings.HighPrecisionAreaMatching = cb_highPrecision.Checked;
            _sheetExportSettings.AllowUnorderedFrames = cb_allowUordering.Checked;
            _sheetExportSettings.UseUniformGrid = cb_useUniformGrid.Checked;
            _sheetExportSettings.UsePaddingOnJson = cb_padFramesOnJson.Checked;
            _sheetExportSettings.ExportJson = cb_exportJson.Checked;
            _sheetExportSettings.XPadding = (int)nud_xPadding.Value;
            _sheetExportSettings.YPadding = (int)nud_yPadding.Value;

            return _sheetExportSettings;
        }
        
        private void OnControllerOnViewModifiedChanged(object sender, EventArgs args)
        {
            UpdateUnsavedAnimationsIconState();
        }

        private void OnControllerOnViewOpenedClosed(object sender, ViewOpenCloseEventArgs args)
        {
            // Erase current bundle sheet export, in case the animation closed was previously from this view
            // This is a work-around 
            var animView = sender as AnimationView;
            if (animView != null && _controller.GetOwningAnimationSheet(animView.CurrentAnimation)?.ID == CurrentSheet.ID && _generatedWhileUnsaved)
            {
                _sheetPreviewTool.SheetExport = null;
                _bundleSheetExport = null;
            }

            UpdateUnsavedAnimationsIconState();
        }

        /// <summary>
        /// Updates state of unsaved animations icon
        /// </summary>
        private void UpdateUnsavedAnimationsIconState()
        {
            // Turn warn icon on if any animation from this sheet has unsaved changes
            pb_unsavedAnimWarning.Visible = HasUnsavedAnimations();
        }

        /// <summary>
        /// Returns whether any of the animations on this sheet is currently opened, with unsaved changes associated with them
        /// </summary>
        private bool HasUnsavedAnimations()
        {
            return CurrentSheet.Animations.Any(_controller.InterfaceStateProvider.HasUnsavedChangesForAnimation);
        }

        /// <summary>
        /// Generates a preview for the AnimationSheet currently loaded into this form
        /// </summary>
        public void GeneratePreview()
        {
            _generatedWhileUnsaved = HasUnsavedAnimations();

            RepopulateExportSettings();
            UpdateCountLabels();

            if (CurrentSheet.Animations.Length <= 0)
            {
                lbl_alertLabel.Text = AnimationMessages.TextNoAnimationInSheetToGeneratePreview;
                pnl_alertPanel.Visible = true;
                return;
            }

            _sheetCancellation?.Cancel();
            var cancellation = new CancellationTokenSource();

            _sheetCancellation = cancellation;

            // Time the bundle export
            pb_exportProgress.Visible = true;

            void Handler(BundleExportProgressEventArgs args)
            {
                Invoke(new Action(() =>
                {
                    if (cancellation.IsCancellationRequested)
                        return;

                    pb_exportProgress.Value = args.StageProgress;
                }));
            }

            var form = FindForm();
            if (form != null)
                form.Cursor = Cursors.WaitCursor;

            btn_generatePreview.Enabled = false;

            var sw = Stopwatch.StartNew();

            // Get a dynamic provider for better accuracy of animations to export
            var provider = _controller.GetDynamicProviderForSheet(CurrentSheet, _sheetExportSettings);

            // Export the bundle
            var t = _controller.GenerateBundleSheet(provider, cancellation.Token, Handler);

            t.ContinueWith(task =>
            {
                Invoke(new Action(() =>
                {
                    btn_generatePreview.Enabled = true;

                    // Dispose of current preview
                    RemovePreview();

                    if (task.IsCanceled || cancellation.IsCancellationRequested)
                    {
                        _sheetCancellation = null;
                        Close();
                        return;
                    }

                    _sheetCancellation = null;

                    _bundleSheetExport = task.Result;

                    var img = _bundleSheetExport.Sheet;

                    sw.Stop();

                    if (form != null)
                        form.Cursor = Cursors.Default;

                    zpb_sheetPreview.PictureBox.SetBitmap((Bitmap)img);

                    pb_exportProgress.Visible = false;

                    // Update labels
                    lbl_sheetPreview.Text = AnimationMessages.TextSheetPreviewGenerated + sw.ElapsedMilliseconds + @"ms)";

                    lbl_dimensions.Text = img.Width + @"x" + img.Height;
                    lbl_pixelCount.Text = (img.Width * img.Height).ToString("N0");
                    lbl_framesOnSheet.Text = (_bundleSheetExport.FrameCount - _bundleSheetExport.ReusedFrameCount) + "";
                    lbl_reusedFrames.Text = (_bundleSheetExport.ReusedFrameCount) + "";
                    lbl_memoryUsage.Text = Utilities.FormatByteSize(ImageUtilities.MemoryUsageOfImage(img));

                    if (pnl_alertPanel.Visible &&
                        lbl_alertLabel.Text == AnimationMessages.TextNoAnimationInSheetToGeneratePreview)
                    {
                        pnl_alertPanel.Visible = false;
                    }

                    if (cb_showFrameBounds.Checked)
                    {
                        ShowFrameBounds();
                    }
                }));
            }, cancellation.Token);
        }

        /// <summary>
        /// Shows the frame bounds for the exported image
        /// </summary>
        public void ShowFrameBounds()
        {
            if (_bundleSheetExport != null)
            {
                _sheetPreviewTool.LoadExportSheet(_bundleSheetExport);
            }

            cb_showReuseCount.Enabled = true;
        }

        /// <summary>
        /// Hides the frame bounds for the exported image
        /// </summary>
        public void HideFrameBounds()
        {
            _sheetPreviewTool.UnloadExportSheet();

            cb_showReuseCount.Enabled = false;
        }

        /// <summary>
        /// Shows the reuse count for the exported image
        /// </summary>
        public void ShowReuseCount()
        {
            _sheetPreviewTool.DisplayReusedCount = true;
        }

        /// <summary>
        /// Hides the reuse count for the exported image
        /// </summary>
        public void HideReuseCount()
        {
            if (_bundleSheetExport != null)
            {
                _sheetPreviewTool.DisplayReusedCount = false;
            }
        }

        /// <summary>
        /// Removes the currently displayed preview and disposes of the image
        /// </summary>
        public void RemovePreview()
        {
            zpb_sheetPreview.PictureBox.Image?.Dispose();
            zpb_sheetPreview.PictureBox.SetBitmap(new Bitmap(1, 1));

            _bundleSheetExport = null;
        }

        /// <summary>
        /// Generates an AnimationSheet using the settings on this form's fields
        /// </summary>
        /// <returns>The new AnimationSheet object</returns>
        public AnimationSheet GenerateAnimationSheet()
        {
            return new AnimationSheet(txt_sheetName.Text) { SheetExportSettings = RepopulateExportSettings() };
        }
        
        /// <summary>
        /// Displays frame context menu for the given sheet preview click action
        /// </summary>
        public void DisplayFrameContextMenu([NotNull] SheetPreviewFrameBoundsClickEventArgs e)
        {
            var index = e.SheetBoundsIndex;
            var frameBoundsMap = _bundleSheetExport.Atlas.GetFrameBoundsMap();

            // Pull all frames found
            var frameIds = frameBoundsMap.FrameIdsAtSheetIndex(index);

            // This guy maps a list of tuples containing animations and their respective frames which are contained in the frameIds array above
            var framesPerAnimation =
                (
                    from
                        animation in _bundleSheetExport.Animations
                    let list = animation.Frames.Where(frame => frameIds.Contains(frame.ID)).ToList()
                    where
                        list.Count > 0
                    select
                        (animation, list)
                ).ToList();

            // TODO: This is a work-around for removing frames from an animation without updating the preview, leading to a
            // dangling frame that is still 'clickeable' on the sheet preview.
            if (framesPerAnimation.Count == 0)
                return;

            var menu = new ContextMenuStrip();
            
            // Summary
            var title = new ToolStripMenuItem($"Shared between {frameBoundsMap.CountOfFramesAtSheetBoundsIndex(index)} frame(s):")
            {
                Enabled = false
            };

            menu.Items.Add(title);
            menu.Items.Add(new ToolStripSeparator());

            foreach (var tuple in framesPerAnimation)
            {
                // Animation
                var animItem = new ToolStripMenuItem(tuple.Item1.Name)
                {
                    Image = Resources.anim_icon
                };
                
                foreach (var frame in tuple.Item2)
                {
                    var frameItem = new ToolStripMenuItem($"Frame {frame.Index + 1}")
                    {
                        Image = Resources.frame_icon
                    };

                    frameItem.Click += (sender, args) =>
                    {
                        // Open frame in controller
                        var view = _controller.OpenAnimationView(tuple.Item1, frame.Index);

                        view.SetAnimationControlPlayback(false);
                        view.SetAnimationControlFrameIndex(frame.Index);
                    };

                    // Add frame labels
                    animItem.DropDownItems.Add(frameItem);
                }
                
                menu.Items.Add(animItem);
            }

            // Used to fix drawing of selected frame
            menu.Closed += (sender, args) =>
            {
                _sheetPreviewTool.AllowMouseHover = false;
                _sheetPreviewTool.AllowMouseHover = true;
            };

            menu.Show(MousePosition);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_sheetCancellation != null)
            {
                _sheetCancellation.Cancel();
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

        // 
        // Animation Sheet Name textfield change
        // 
        private void txt_sheetName_TextChanged(object sender, EventArgs e)
        {
            ValidateFields();

            MarkModified();

            _reactive.OnChange.OnNext(Unit.Default);
        }

        // 
        // Common event for all checkboxes on the form
        // 
        private void checkboxes_Change(object sender, EventArgs e)
        {
            MarkModified();

            _reactive.OnChange.OnNext(Unit.Default);
        }

        // 
        // Common event for all nuds on the form
        // 
        private void nuds_Common(object sender, EventArgs e)
        {
            MarkModified();

            _reactive.OnChange.OnNext(Unit.Default);
        }

        // 
        // Zoom anud value changed
        // 
        private void anud_zoom_ValueChanged(object sender, EventArgs e)
        {
            zpb_sheetPreview.PictureBox.Zoom = new PointF((float)anud_zoom.Value, (float)anud_zoom.Value);
        }

        // 
        // Generate Preview button click
        // 
        private void btn_generatePreview_Click(object sender, EventArgs e)
        {
            GeneratePreview();
        }

        // 
        // Form Closing event handler
        // 
        private void AnimationSheetView_FormClosed(object sender, FormClosedEventArgs e)
        {
            RemovePreview();
        }

        // 
        // Show Frame Bounds checkbox check
        // 
        private void cb_showFrameBounds_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_showFrameBounds.Checked)
            {
                ShowFrameBounds();
            }
            else
            {
                HideFrameBounds();
            }
        }

        //
        // Show Reuse Count checkbox check
        //
        private void cb_showReuseCount_CheckedChanged(object sender, EventArgs e)
        {
            if (cb_showReuseCount.Checked)
            {
                ShowReuseCount();
            }
            else
            {
                HideReuseCount();
            }
        }

        // 
        // Ok button click
        // 
        private void btn_ok_Click(object sender, EventArgs e)
        {
            // Validate one more time before closing
            if (ValidateFields() == false)
            {
                return;
            }

            if (CurrentSheet != null && modified)
            {
                ApplyChanges();
            }

            Close();
        }

        // 
        // Cancel button click
        // 
        private void btn_cancel_Click(object sender, EventArgs e)
        {
            DiscardChangesAndClose();
        }

        // 
        // Apply Changes button click
        // 
        private void btn_apply_Click(object sender, EventArgs e)
        {
            ApplyChanges();
        }

        // 
        // Sheet Preview ZPB zoom changed
        // 
        private void zpb_sheetPreview_ZoomChanged(object sender, [NotNull] ZoomChangedEventArgs e)
        {
            anud_zoom.Value = (decimal)e.NewZoom;
        }

        // 
        // Sheet Preview right clicked frame rectangle event
        // 
        private void sppb_clickedFrameRect(object sender, [NotNull] SheetPreviewFrameBoundsClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                DisplayFrameContextMenu(e);
            }
        }

        //
        // Auto Update Preview checkbox check
        //
        private void cb_autoUpdatePreview_CheckedChanged(object sender, EventArgs e)
        {
            _autoUpdatePreview = cb_autoUpdatePreview.Checked;
        }

        private sealed class Reactive: IReactive, IDisposable
        {
            public readonly Subject<Unit> OnChange = new Subject<Unit>();

            public IObservable<Unit> Change => OnChange;

            public void Dispose()
            {
                OnChange?.Dispose();
            }
        }

        /// <summary>
        /// Public-facing Reactive bindings
        /// </summary>
        public interface IReactive
        {
            /// <summary>
            /// Called whenever any of the fields on the view are changed by the user.
            /// 
            /// Changes only count when they would affect the AnimationSheetView model when the
            /// user applies/saves the changes.
            /// </summary>
            IObservable<Unit> Change { get; }
        }
    }
}