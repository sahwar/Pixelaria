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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Pixelaria.Controllers.Exporters;
using Pixelaria.Controllers.Importers;
using Pixelaria.Controllers.Validators;
using Pixelaria.Data;
using Pixelaria.Data.Exports;
using Pixelaria.Data.Factories;
using Pixelaria.Data.Persistence;
using Pixelaria.Properties;
using Pixelaria.Views;
using Pixelaria.Views.MiscViews;
using Pixelaria.Views.ModelViews;

using Pixelaria.Utils;
using Settings = Pixelaria.Utils.Settings;

namespace Pixelaria.Controllers
{
    /// <summary>
    /// Main application controller
    /// </summary>
    public partial class Controller
    {
        /// <summary>
        /// The list of currently opened files
        /// </summary>
        readonly List<PixelariaFile> _files;

        /// <summary>
        /// The main application form
        /// </summary>
        readonly MainForm _mainForm;

        /// <summary>
        /// Gets the current bundle opened on the application
        /// </summary>
        public Bundle CurrentBundle { get; private set; }

        /// <summary>
        /// Gets an array of the current files opened in the program
        /// </summary>
        public PixelariaFile[] Files => _files.ToArray();

        /// <summary>
        /// Gets the current IDefaultImporter of the program
        /// </summary>
        public IDefaultImporter DefaultImporter { get; }

        /// <summary>
        /// Gets the current IAnimationValidator of the program
        /// </summary>
        public IAnimationValidator AnimationValidator { get; }

        /// <summary>
        /// Gets the current IAnimationSheetValidator of the program
        /// </summary>
        public IAnimationSheetValidator AnimationSheetValidator { get; }

        /// <summary>
        /// Gets the current IFrameFactory of the program
        /// </summary>
        public IFrameFactory FrameFactory { get; }

        /// <summary>
        /// Gets whether the current bundle has unsaved changes
        /// </summary>
        public bool UnsavedChanges { get; private set; }

        /// <summary>
        /// Gets the current RecentFileList for the program
        /// </summary>
        public RecentFileList CurrentRecentFileList { get; }

        #region Eventing

        /// <summary>
        /// Delegate for animation-related events
        /// </summary>
        /// <param name="sender">The sender for the event</param>
        /// <param name="args">The arguments for the event</param>
        public delegate void AnimationEventHandler(object sender, AnimationEventArgs args);

        /// <summary>
        /// Delegate for animation sheet-related events
        /// </summary>
        /// <param name="sender">The sender for the event</param>
        /// <param name="args">The arguments for the event</param>
        public delegate void AnimationSheetEventHandler(object sender, AnimationSheetEventArgs args);

        /// <summary>
        /// Event fired whenever an animation has been added to a bundle
        /// </summary>
        public event AnimationEventHandler AnimationAdded;

        /// <summary>
        /// Event fired whenever an animation has been removed from a bundle
        /// </summary>
        public event AnimationEventHandler AnimationRemoved;

        /// <summary>
        /// Event fired whenever an animation sheet has been added to a bundle
        /// </summary>
        public event AnimationSheetEventHandler AnimationSheetAdded;

        /// <summary>
        /// Event fired whenever an animation sheet has been removed from a bundle
        /// </summary>
        public event AnimationSheetEventHandler AnimationSheetRemoved;

        #endregion

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="mainForm">The form to use as the main form of the application</param>
        public Controller(MainForm mainForm)
        {
            _files = new List<PixelariaFile>();

            // Initialize the factories
            FrameFactory = new DefaultFrameFactory(this);

            // Initialize the validators and exporters
            DefaultValidator defValidator = new DefaultValidator(this);

            AnimationValidator = defValidator;
            AnimationSheetValidator = defValidator;

            DefaultImporter = new DefaultPngImporter();

            // Initialize the Settings singleton
            Settings.GetSettings(Path.GetDirectoryName(Application.LocalUserAppDataPath) + "\\settings.ini");

            CurrentRecentFileList = new RecentFileList(10);

            if (mainForm != null)
            {
                mainForm.Controller = this;
                // Initialize the basic fields
                _mainForm = mainForm;
                _mainForm.UpdateRecentFilesList();

                // Start with a new empty bundle
                ShowNewBundle();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //////////
        ////////// Bundle Related Methods
        //////////
        /////
        ///// Methods that change the state of the program by manipulating the Bundle
        ///// directly.
        /////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Saves the currently loaded bundle to the given path on disk
        /// </summary>
        /// <param name="savePath">The path to save the currently bundle to</param>
        public void SaveBundle(string savePath)
        {
            CurrentBundle.SaveFile = savePath;
            PixelariaSaverLoader.SaveBundleToDisk(CurrentBundle, savePath);

            MarkUnsavedChanges(false);
        }

        /// <summary>
        /// Opens a loaded bundle from the given path on disk
        /// </summary>
        /// <param name="savePath">The path to load the bundle from</param>
        public void LoadBundleFromFile(string savePath)
        {
            PixelariaFile file = PixelariaSaverLoader.LoadFileFromDisk(savePath);

            if (file == null)
            {
                MessageBox.Show(Resources.ErrorLoadingFile, Resources.Error_AlertTile, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Dispose of the current bundle if it's present
            if (CurrentBundle != null)
            {
                CloseBundle(CurrentBundle);
            }

            Bundle newBundle = file.LoadedBundle;

            newBundle.SaveFile = savePath;

            LoadBundle(newBundle);

            // Store the file now
            CurrentRecentFileList.StoreFile(savePath);
            _mainForm.UpdateRecentFilesList();
        }

        /// <summary>
        /// Loads the given bundle into the interface.
        /// This method disposes of the current bundle
        /// </summary>
        /// <param name="newBundle">The new bundle to load</param>
        public void LoadBundle(Bundle newBundle)
        {
            CurrentBundle = newBundle;

            _mainForm.LoadBundle(CurrentBundle);

            // Update the Unsaved Changes flag to false
            MarkUnsavedChanges(false);
        }

        /// <summary>
        /// Loads a bundle from the list of recent files list
        /// </summary>
        /// <param name="index">The index to get the file path from</param>
        public void LoadBundleFromRecentFileList(int index)
        {
            if (!File.Exists(CurrentRecentFileList[index]))
            {
                if (MessageBox.Show(Resources.UnexistingFileInFileList_RemoveQuestion, Resources.Question_AlertTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    CurrentRecentFileList.RemoveFromList(index);
                    _mainForm.UpdateRecentFilesList();
                }

                return;
            }

            // Cancel on changes saving confirmation quits the method
            if (ShowConfirmSaveChanges() == DialogResult.Cancel)
                return;

            LoadBundleFromFile(CurrentRecentFileList[index]);
        }

        /// <summary>
        /// Closes the given bundle from the controller
        /// </summary>
        /// <param name="bundle">The bundle to close</param>
        public void CloseBundle(Bundle bundle)
        {
            PixelariaFile file = GetPixelariaFileByBundle(bundle);

            file?.Dispose();

            bundle.Dispose();
        }

        /// <summary>
        /// Marks whether or not the current bundle has unsaved changes.
        /// This method alters the interface to display unsaved changes accordingly.
        /// This method does not change anything if the new unsaved changes flag is the same
        /// as the current one
        /// </summary>
        /// <param name="isUnsaved">The new value for the Unsaved Changes flag</param>
        public void MarkUnsavedChanges(bool isUnsaved)
        {
            if (CurrentBundle == null || isUnsaved == UnsavedChanges)
                return;

            UnsavedChanges = isUnsaved;

            _mainForm.UnsavedChangesUpdated(isUnsaved);
        }

        /// <summary>
        /// Creates and returns a new Animation.
        /// This method also adds the newly created animation to the currently loaded bundle
        /// </summary>
        /// <param name="name">The name of the new animation</param>
        /// <param name="width">The width of the animation</param>
        /// <param name="height">The height of the animation</param>
        /// <param name="fps">The FPS for the animation</param>
        /// <param name="frameskip">Whether the animation should frameskip</param>
        /// <param name="openOnForm">Whether to open the newly added animation on the main form</param>
        /// <param name="parentSheet">Optional AnimationSheet that will own the newly created animation</param>
        /// <returns>The newly created animation</returns>
        public Animation CreateAnimation(string name, int width, int height, int fps, bool frameskip, bool openOnForm, AnimationSheet parentSheet = null)
        {
            Animation anim = new Animation(name, width, height)
            {
                PlaybackSettings = new AnimationPlaybackSettings { FPS = fps, FrameSkip = frameskip }
            };

            // Create a dummy frame
            anim.CreateFrame();

            AddAnimation(anim, openOnForm, parentSheet);

            return anim;
        }

        /// <summary>
        /// Adds the given Animation into the current bundle
        /// </summary>
        /// <param name="anim">The animation to add to the bundle</param>
        /// <param name="openOnForm">Whether to open the newly added animation on the main form</param>
        /// <param name="parentSheet">Optional AnimationSheet that will own the newly created animation</param>
        public void AddAnimation(Animation anim, bool openOnForm, AnimationSheet parentSheet = null)
        {
            CurrentBundle.AddAnimation(anim, parentSheet);

            if (openOnForm)
            {
                _mainForm.AddAnimation(anim, true);
                _mainForm.OpenViewForAnimation(anim);
            }
            else
            {
                _mainForm.AddAnimation(anim);
            }

            AnimationAdded?.Invoke(this, new AnimationEventArgs(anim));

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Removes the given Animation from the current bundle
        /// </summary>
        /// <param name="anim">The Animation to remove from the bundle</param>
        public void RemoveAnimation(Animation anim)
        {
            CurrentBundle.RemoveAnimation(anim);

            _mainForm.RemoveAnimation(anim);

            AnimationRemoved?.Invoke(this, new AnimationEventArgs(anim));

            MarkUnsavedChanges(true);

            // Dispose of the animation
            anim.Dispose();
        }

        /// <summary>
        /// Method to be called whenever changes have been made to the fields of an Animation
        /// </summary>
        /// <param name="anim">The Animation that was modified</param>
        public void UpdatedAnimation(Animation anim)
        {
            _mainForm.UpdateAnimation(anim);

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Gets the index of the given Animation object inside its current parent container
        /// </summary>
        /// <param name="anim">The animation to get the index of</param>
        /// <returns>The index of the animation in its current parent container</returns>
        public int GetAnimationIndex(Animation anim)
        {
            return CurrentBundle.GetAnimationIndex(anim);
        }

        /// <summary>
        /// Rearranges the index of an Animation in the animation's current storing container
        /// </summary>
        /// <param name="anim">The animation to rearrange</param>
        /// <param name="newIndex">The new index to place the animation at</param>
        public void RearrangeAnimationsPosition(Animation anim, int newIndex)
        {
            CurrentBundle.RearrangeAnimationsPosition(anim, newIndex);

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Creates and returns a new Animation Sheet
        /// </summary>
        /// <param name="name">The name for the animation sheet</param>
        /// <param name="openOnForm">Whether to open the newly added animation sheet on the main form</param>
        public AnimationSheet CreateAnimationSheet(string name, bool openOnForm)
        {
            AnimationSheet sheet = new AnimationSheet(name);

            AddAnimationSheet(sheet, openOnForm);

            return sheet;
        }

        /// <summary>
        /// Adds the given Animation Sheet into the current bundle
        /// </summary>
        /// <param name="sheet">The sheet to load into the current bundle</param>
        /// <param name="openOnForm">Whether to open the newly added animation sheet on the main form</param>
        public void AddAnimationSheet(AnimationSheet sheet, bool openOnForm)
        {
            CurrentBundle.AddAnimationSheet(sheet);

            if (openOnForm)
            {
                _mainForm.AddAnimationSheet(sheet, true);
                _mainForm.OpenViewForAnimationSheet(sheet);
            }
            else
            {
                _mainForm.AddAnimationSheet(sheet);
            }

            AnimationSheetAdded?.Invoke(this, new AnimationSheetEventArgs(sheet));

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Removes the given AnimationSeet from the current bundle
        /// </summary>
        /// <param name="sheet">The sheet to remove from the bundle</param>
        /// <param name="deleteAnimations">Whether to delete the nested animations as well. If set to false, the animations will be moved to the bundle's root</param>
        public void RemoveAnimationSheet(AnimationSheet sheet, bool deleteAnimations)
        {
            // Remove/relocate animations
            if (deleteAnimations)
            {
                foreach (Animation anim in sheet.Animations)
                {
                    RemoveAnimation(anim);
                }
            }

            // Remove the sheet
            CurrentBundle.RemoveAnimationSheet(sheet, false);

            _mainForm.RemoveAnimationSheet(sheet);

            AnimationSheetRemoved?.Invoke(this, new AnimationSheetEventArgs(sheet));

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Method to be called whenever changes have been made to the fields of an AnimationSheet
        /// </summary>
        /// <param name="sheet">The AnimationSheet that was modified</param>
        public void UpdatedAnimationSheet(AnimationSheet sheet)
        {
            _mainForm.UpdateAnimationSheet(sheet);

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Gets the index of the given AnimationSheet object inside its current parent container
        /// </summary>
        /// <param name="sheet">The sheet to get the index of</param>
        /// <returns>The index of the sheet in its current parent container</returns>
        public int GetAnimationSheetIndex(AnimationSheet sheet)
        {
            return CurrentBundle.GetAnimationSheetIndex(sheet);
        }

        /// <summary>
        /// Rearranges the index of an AnimationSheets in the sheets's current storing container
        /// </summary>
        /// <param name="sheet">The sheet to rearrange</param>
        /// <param name="newIndex">The new index to place the sheet at</param>
        public void RearrangeAnimationSheetsPosition(AnimationSheet sheet, int newIndex)
        {
            CurrentBundle.RearrangeAnimationSheetsPosition(sheet, newIndex);

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Adds the given Animation object into the given AnimationSheet object
        /// If null is provided as animation sheet, the animation is removed from it's current animation sheet, if it's inside one
        /// </summary>
        /// <param name="anim">The animation to add to the animation sheet</param>
        /// <param name="sheet">The AnimationSheet to add the animation to</param>
        public void AddAnimationToAnimationSheet(Animation anim, AnimationSheet sheet)
        {
            CurrentBundle.AddAnimationToAnimationSheet(anim, sheet);

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Gets the AnimationSheet that currently owns the given Animation object.
        /// If the Animation is not inside any AnimationSheet, null is returned
        /// </summary>
        /// <param name="anim">The animation object to get the animation sheet of</param>
        /// <returns>The AnimationSheet that currently owns the given Animation object. If the Animation is not inside any AnimationSheet, null is returned</returns>
        public AnimationSheet GetOwningAnimationSheet(Animation anim)
        {
            return CurrentBundle.GetOwningAnimationSheet(anim);
        }

        /// <summary>
        /// Returns a unique animation name, used for filling in default animation names
        /// </summary>
        /// <returns>A unique animation name to use as a default name</returns>
        public string GetUniqueUntitledAnimationName()
        {
            string prefix = "Untitled-";
            int postfix = 1;

            while (CurrentBundle.GetAnimationByName(prefix + postfix) != null)
            {
                postfix++;
            }

            return prefix + postfix;
        }

        /// <summary>
        /// Returns a unique animation sheet name, used for filling in default animation sheet names
        /// </summary>
        /// <returns>A unique animation sheet name to use as a default name</returns>
        public string GetUniqueUntitledAnimationSheetName()
        {
            string prefix = "Untitled-";
            int postfix = 1;

            while (CurrentBundle.GetAnimationSheetByName(prefix + postfix) != null)
            {
                postfix++;
            }

            return prefix + postfix;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //////////
        ////////// PixelariaFile Related Methods
        //////////
        /////
        ///// Methods that interact with PixelariaFile objects, by creating, updating
        ///// and removing the files. May end up interacting with bundle controllers
        ///// as well.
        /////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a PixelariaFile object which matches the given Bundle object.
        /// If none of the files currently opened match the bundle, null is returned.
        /// </summary>
        /// <param name="bundle">The bundle to get the pixelaria file from</param>
        /// <returns>A PixelariaFile that has the given bundle loaded into it</returns>
        public PixelariaFile GetPixelariaFileByBundle(Bundle bundle)
        {
            return _files.FirstOrDefault(file => ReferenceEquals(file.LoadedBundle, bundle));
        }

        ////////////////////////////////////////////////////////////////////////////////
        //////////
        ////////// Interface Related Methods
        //////////
        /////
        ///// Methods that rely on interface output to change the state of the program.
        ///// These methods will usually use interface to confirm interactions with the
        ///// user before calling the direct bundle manipulation methods.
        /////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Shows an interface for creating a new Bundle to the user. If there are unsaved changes on the current bundle,
        /// an interface for saving changes is shown.
        /// </summary>
        public void ShowNewBundle()
        {
            // Cancel on changes saving confirmation quits the method
            if (ShowConfirmSaveChanges() == DialogResult.Cancel)
                return;

            // Create a new bundle
            LoadBundle(new Bundle("Untitled Bundle"));
        }

        /// <summary>
        /// Shows an interface for Bundle loading to the user. If there are unsaved changes on the current bundle,
        /// an interface for saving changes is shown.
        /// </summary>
        public void ShowLoadBundle()
        {
            // Cancel on changes saving confirmation quits the method
            if (ShowConfirmSaveChanges() == DialogResult.Cancel)
                return;

            OpenFileDialog ofd = new OpenFileDialog { Filter = @"Pixelaria Bundle (*.pxl)|*.pxl" };

            if (ofd.ShowDialog(_mainForm) == DialogResult.OK)
            {
                LoadBundleFromFile(ofd.FileName);
            }
        }

        /// <summary>
        /// Shows an interface for Bundle exporting to the user. If the bundle has no export path set, an interface for editing the
        /// bundle is shown.
        /// </summary>
        public void ShowExportBundle()
        {
            // The bundle needs at least one valid animation sheet with one animation in it before exporting
            if (CurrentBundle.AnimationSheets.Count == 0)
            {
                MessageBox.Show(Resources.NoAnimationSheetsToExportInfo, Resources.Information_AlertTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Whether there are any animation sheets with at least one animation in it
            bool validSheet = CurrentBundle.AnimationSheets.Any(sheet => sheet.Animations.Length != 0);

            if (!validSheet)
            {
                MessageBox.Show(Resources.NoAnimationsInSheetsAlert, Resources.Information_AlertTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // The bundle path must be valid
            if (CurrentBundle.ExportPath.Trim() == "" || !Directory.Exists(CurrentBundle.ExportPath))
            {
                if (MessageBox.Show(Resources.InvalidBundleExportPathAlert_AskEdit, Resources.Question_AlertTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _mainForm.OpenBundleSettings(CurrentBundle);

                    if (CurrentBundle.ExportPath.Trim() == "" || !Directory.Exists(CurrentBundle.ExportPath))
                        return;
                }
                else
                {
                    return;
                }
            }

            var progressForm = new BundleExportProgressView(CurrentBundle, GetExporter());

            progressForm.ShowDialog(_mainForm);
        }

        /// <summary>
        /// Displays an interface for saving the currently opened bundle.
        /// The method returns the DialogResult of the SaveFileDialog that was opened. If no dialog was opened (because the file was already saved on disk),
        /// DialogResult.OK is returned anyways.
        /// </summary>
        /// <param name="forceNew">Whether to show an interface to choose a new save location even if the current bundle already has been saved to disk</param>
        /// <returns>The DialogResult of the SaveFileDialog</returns>
        public DialogResult ShowSaveBundle(bool forceNew = false)
        {
            string savePath = CurrentBundle.SaveFile;

            if (savePath == "" || forceNew)
            {
                SaveFileDialog svd = new SaveFileDialog { Filter = @"Pixelaria Bundle (*.pxl)|*.pxl" };

                if (svd.ShowDialog(_mainForm) == DialogResult.OK)
                {
                    savePath = svd.FileName;
                }
                else
                {
                    return DialogResult.Cancel;
                }

                // Store the file now
                CurrentRecentFileList.StoreFile(savePath);
                _mainForm.UpdateRecentFilesList();
            }

            SaveBundle(savePath);

            return DialogResult.OK;
        }

        /// <summary>
        /// Shows a confirmation of changes save interface to the user if changes have been made to the bundle.
        /// If no changes have been made to the bundle, DialogResult.Yes is returned anyways.
        /// This method saves the changes made to disk as well.
        /// </summary>
        /// <returns>The DialogResult of the confirmation MessageBox. If no changes have been made to the bundle, DialogResult.Yes is returned anyways</returns>
        public DialogResult ShowConfirmSaveChanges()
        {
            if (!UnsavedChanges)
            {
                return DialogResult.Yes;
            }

            DialogResult saveResult = MessageBox.Show(Resources.UnsavedChangesAlert_AskSave, Resources.SaveConfirmation_AlertTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (saveResult == DialogResult.Yes)
            {
                return ShowSaveBundle();
            }

            return saveResult;
        }

        /// <summary>
        /// Shows the interface for new Animation creation
        /// </summary>
        /// <param name="parentSheet">Optional AnimationSheet that will own the newly created Animation</param>
        public void ShowCreateAnimation(AnimationSheet parentSheet = null)
        {
            NewAnimationView nav = new NewAnimationView(this, parentSheet);

            nav.ShowDialog(_mainForm);
        }

        /// <summary>
        /// Shows an interface to duplicate the given animation
        /// </summary>
        /// <param name="animation">The animation to duplicate</param>
        public void ShowDuplicateAnimation(Animation animation)
        {
            Animation dup = CurrentBundle.DuplicateAnimation(animation, null);

            _mainForm.AddAnimation(dup, true);
            _mainForm.OpenViewForAnimation(dup);

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Shows the interface for Animation import
        /// </summary>
        /// <param name="parentSheet">Optional AnimationSheet that will own the newly imported Animation</param>
        public void ShowImportAnimation(AnimationSheet parentSheet = null)
        {
            ImportAnimationView imp = new ImportAnimationView(this, parentSheet);

            imp.ShowDialog(_mainForm);
        }

        /// <summary>
        /// Shows the interface for a new Animation Sheet creation
        /// </summary>
        public void ShowCreateAnimationSheet()
        {
            var ed = new AnimationSheetView(this);

            if (ed.ShowDialog(_mainForm) == DialogResult.OK)
            {
                AddAnimationSheet(ed.GenerateAnimationSheet(), true);
            }
        }

        /// <summary>
        /// Shows an interface to duplicate the given AnimationSheet object
        /// </summary>
        /// <param name="sheet">The animation sheet to duplicate</param>
        public void ShowDuplicateAnimationSheet(AnimationSheet sheet)
        {
            AnimationSheet dup = CurrentBundle.DuplicateAnimationSheet(sheet);

            _mainForm.AddAnimationSheet(dup, true);
            _mainForm.OpenViewForAnimationSheet(dup);

            // Add the cloned animations as well
            foreach (Animation anim in dup.Animations)
            {
                _mainForm.AddAnimation(anim);
            }

            MarkUnsavedChanges(true);
        }

        /// <summary>
        /// Shows an interface to save an animation sheet's generated texture to disk
        /// </summary>
        /// <param name="sheet">The animation sheet to save to disk</param>
        public void ShowExportAnimationSheetImage(AnimationSheet sheet)
        {
            if (sheet.AnimationCount == 0)
            {
                MessageBox.Show(Resources.ExportSheetImage_NoAnimationsInSheet, Resources.Information_AlertTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Get a file name
            string saveName = ShowSaveImage(null, sheet.Name, _mainForm);

            if (saveName == "") return;

            var exportView = new SheetExportProgressView(sheet, saveName, GetExporter());

            exportView.ShowDialog(_mainForm);
        }

        /// <summary>
        /// Shows a dialog to save an image to disk, and returns the selected path.
        /// Returns string.Empty if the user has canceled
        /// </summary>
        /// <param name="imageFormat">The ImageFormat associated with the file format chosen by the user</param>
        /// <param name="imageToSave">The image to save to disk</param>
        /// <param name="fileName">An optional file name to display as default name when the dialog shows up</param>
        /// <param name="owner">An optional owner for the file dialog</param>
        /// <returns>The selected save path, or an empty string if the user has not chosen a save path</returns>
        public string ShowSaveImage(out ImageFormat imageFormat, Image imageToSave = null, string fileName = "", IWin32Window owner = null)
        {
            imageFormat = ImageFormat.Png;

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = @"PNG Image (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp|GIF Image (*.gif)|*.gif|JPEG Image (*.jpg)|*.jpg|TIFF Image (*.tiff)|*.tiff",
                FileName = fileName
            };

            if (sfd.ShowDialog(owner) != DialogResult.OK)
                return string.Empty;

            string savePath = sfd.FileName;

            imageFormat = ImageFormatForExtension(Path.GetExtension(fileName), imageFormat);

            imageToSave?.Save(savePath, imageFormat);

            return savePath;
        }

        /// <summary>
        /// Shows a dialog to save an image to disk, and returns the selected path.
        /// Returns string.Empty if the user has canceled
        /// </summary>
        /// <param name="imageToSave">The image to save to disk</param>
        /// <param name="fileName">An optional file name to display as default name when the dialog shows up</param>
        /// <param name="owner">An optional owner for the file dialog</param>
        /// <returns>The selected save path, or an empty string if the user has not chosen a save path</returns>
        public string ShowSaveImage(Image imageToSave = null, string fileName = "", IWin32Window owner = null)
        {
            ImageFormat format;
            return ShowSaveImage(out format, imageToSave, fileName, owner);
        }

        /// <summary>
        /// Returns the ImageFormat associated with a given file extension
        /// </summary>
        /// <param name="extension">The extension of the file format, with or without the precending '.'</param>
        /// <param name="defaultFormat">The default format, if the extension is not valid</param>
        /// <returns>The ImageFormat associated with a given file extension</returns>
        public ImageFormat ImageFormatForExtension(string extension, ImageFormat defaultFormat)
        {
            if (extension.StartsWith("."))
                extension = extension.Substring(1);
            extension = extension.ToLower();

            switch (extension.ToLower())
            {
                case @"bmp":
                    return ImageFormat.Bmp;

                case @"gif":
                    return ImageFormat.Gif;

                case @"ico":
                    return ImageFormat.Icon;

                case @"jpg":
                case @"jpeg":
                    return ImageFormat.Jpeg;

                case @"png":
                    return ImageFormat.Png;

                case @"tif":
                case @"tiff":
                    return ImageFormat.Tiff;

                case @"wmf":
                    return ImageFormat.Wmf;

                default:
                    return defaultFormat;
            }
        }

        /// <summary>
        /// Shows a dialog to load an image from disk, and returns the loaded image file.
        /// Returns null if the user has canceled
        /// </summary>
        /// <param name="fileName">An optional file name to display as default name when the dialog shows up</param>
        /// <param name="owner">An optional owner for the file dialog</param>
        /// <returns>The selected image, or null if the user has not chosen an image</returns>
        public Image ShowLoadImage(string fileName = "", IWin32Window owner = null)
        {
            string filePath;
            return ShowLoadImage(out filePath, fileName, owner);
        }

        /// <summary>
        /// Shows a dialog to load an image from disk, and returns the loaded image file.
        /// Returns null if the user has canceled.
        /// The image loaded is automatically converted into a 32bpp transparent bitmap image format
        /// </summary>
        /// <param name="filePath">The file path that was chosen for the file. Returned as an empty string when no file was chosen</param>
        /// <param name="fileName">An optional file name to display as default name when the dialog shows up</param>
        /// <param name="owner">An optional owner for the file dialog</param>
        /// <returns>>The selected image, or null if the user has not chosen an image</returns>
        public Image ShowLoadImage(out string filePath, string fileName = "", IWin32Window owner = null)
        {
            filePath = string.Empty;

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = @"PNG Image (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp|GIF Image (*.gif)|*.gif|JPEG Image (*.jpg)|*.jpg|TIFF Image (*.tiff)|*.tiff|All image formats (*.png, *.jpg, *.gif, *.tiff, *.bmp)|*.png;*.jpg;*.gif;*.tiff;*.bmp",
                FileName = fileName
            };

            if (ofd.ShowDialog(owner) == DialogResult.OK)
            {
                filePath = ofd.FileName;

                try
                {
                    using (var img = Image.FromFile(ofd.FileName))
                    {
                        return PreparedImage(img);
                    }
                }
                catch (Exception)
                {
                    filePath = "";
                    MessageBox.Show(@"Error loading selected image. It may not be in a valid image file format.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return null;
        }

        /// <summary>
        /// Shows a dialog to load multiple images from disk, and returns the loaded image files.
        /// Returns null if the user has canceled
        /// </summary>
        /// <param name="owner">An optional owner for the file dialog</param>
        /// <returns>The images the user opened, or null, if no images were chosen</returns>
        public Image[] ShowLoadImages(IWin32Window owner = null)
        {
            string[] filePaths;
            return ShowLoadImages(out filePaths, owner);
        }

        /// <summary>
        /// Shows a dialog to load multiple images from disk, and returns the loaded image files.
        /// Returns null if the user has canceled.
        /// The images loaded are automatically converted into 32bpp transparent bitmap image format
        /// </summary>
        /// <param name="filePaths">The file paths that were chosen for the files. Returned as an empty array when no files were chosen</param>
        /// <param name="owner">An optional owner for the file dialog</param>
        /// <returns>The images the user opened, or null, if no images were chosen</returns>
        public Image[] ShowLoadImages(out string[] filePaths, IWin32Window owner = null)
        {
            filePaths = new string[0];

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = @"PNG Image (*.png)|*.png|Bitmap Image (*.bmp)|*.bmp|GIF Image (*.gif)|*.gif|JPEG Image (*.jpg)|*.jpg|TIFF Image (*.tiff)|*.tiff|All image formats (*.png, *.jpg, *.gif, *.tiff, *.bmp)|*.png;*.jpg;*.gif;*.tiff;*.bmp",
                Multiselect = true
            };

            if (ofd.ShowDialog(owner) == DialogResult.OK)
            {
                filePaths = ofd.FileNames;

                try
                {
                    var sources = filePaths.Select(Image.FromFile).ToArray();
                    var baked = sources.Select(PreparedImage);
                    
                    // Dispose of the images
                    Array.ForEach(sources.ToArray(), image => image.Dispose());

                    return baked.ToArray();
                }
                catch (Exception)
                {
                    filePaths = new string[0];
                    MessageBox.Show(@"Error loading selected images. They may not all be valid image file formats.", @"Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns an Image object that contains the contents of a given image baked into a 32bpp bitmap image
        /// </summary>
        /// <param name="image">The image to prepare</param>
        /// <returns>The image that was prepared from the given image</returns>
        private Image PreparedImage(Image image)
        {
            var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(image, 0, 0, image.Width, image.Height);
                g.Flush();
            }

            return bitmap;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //////////
        ////////// Misc Methods
        //////////
        /////
        ///// Miscelaneous methods not strictly related to bundles or interface
        /////
        ////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or generates a new exporter that is fit to be used during new fresh export operations
        /// </summary>
        public IBundleExporter GetExporter()
        {
            return new DefaultPngExporter();
        }

        /// <summary>
        /// Generates an export image for the given AnimationSheet
        /// </summary>
        /// <param name="sheet">The animation sheet to generate the export of</param>
        /// <returns>An Image that represents the exported image for the animation sheet</returns>
        public Task<BundleSheetExport> GenerateExportForAnimationSheet(AnimationSheet sheet)
        {
            return GetExporter().ExportBundleSheet(sheet);
        }
        
        /// <summary>
        /// Generates a BundleSheetExport object that contains information about the export of a sheet
        /// </summary>
        /// <param name="exportSettings">The export settings for the sheet</param>
        /// <param name="anims">The list of animations to export</param>
        /// <returns>A BundleSheetExport object that contains information about the export of the sheet</returns>
        public Task<BundleSheetExport> GenerateBundleSheet(AnimationExportSettings exportSettings, params Animation[] anims)
        {
            return GetExporter().ExportBundleSheet(exportSettings, anims);
        }

        /// <summary>
        /// Generates a BundleSheetExport object that contains information about the export of a sheet, using a custom event handler
        /// for export progress callback
        /// </summary>
        /// <param name="exportSettings">The export settings for the sheet</param>
        /// <param name="cancellationToken">A cancelation token that can be used to cancel the process mid-way</param>
        /// <param name="callback">The callback delegate to be used during the generation process</param>
        /// <param name="anims">The list of animations to export</param>
        /// <returns>A BundleSheetExport object that contains information about the export of the sheet</returns>
        public Task<BundleSheetExport> GenerateBundleSheet(AnimationExportSettings exportSettings, CancellationToken cancellationToken, BundleExportProgressEventHandler callback, params Animation[] anims)
        {
            return GetExporter().ExportBundleSheet(exportSettings, anims, cancellationToken, callback);
        }

        /// <summary>
        /// Shows an interface for saving a sprite strip version of the specified animation
        /// </summary>
        /// <param name="animation">The animation to save a sprite strip out of</param>
        public void ShowSaveAnimationStrip(Animation animation)
        {
            using (var stripImage = GetExporter().GenerateSpriteStrip(animation))
            {
                ShowSaveImage(stripImage, animation.Name);
            }
        }
    }

    /// <summary>
    /// IFrameIdGenerator implementation
    /// </summary>
    public partial class Controller : IFrameIdGenerator
    {
        public int GetNextUniqueFrameId()
        {
            if(CurrentBundle == null)
                throw new InvalidOperationException(@"No bundle setup - cannot generate unique IDs");

            return CurrentBundle.GetNextUniqueFrameId();
        }
    }

    /// <summary>
    /// Event arguments for an animation-related event
    /// </summary>
    public class AnimationEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the animation binded to this event
        /// </summary>
        public Animation Animation { get; private set; }

        /// <summary>
        /// Initializes a new instance of the AnimationEventArgs class with an animation to attach to this event argument
        /// </summary>
        public AnimationEventArgs(Animation animation)
        {
            Animation = animation;
        }
    }

    /// <summary>
    /// Event arguments for an animation sheet-related event
    /// </summary>
    public class AnimationSheetEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the animation sheet binded to this event
        /// </summary>
        public AnimationSheet AnimationSheet { get; private set; }

        /// <summary>
        /// Initializes a new instance of the AnimationSheetEventArgs class with an animation sheet to attach to this event argument
        /// </summary>
        public AnimationSheetEventArgs(AnimationSheet animationSheet)
        {
            AnimationSheet = animationSheet;
        }
    }
}