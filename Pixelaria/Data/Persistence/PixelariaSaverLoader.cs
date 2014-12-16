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
using System.Linq;
using System.IO;

using Pixelaria.Data.Persistence.PixelariaFileBlocks;
using Pixelaria.Utils;

namespace Pixelaria.Data.Persistence
{
    /// <summary>
    /// Main persistence handler for the Pixelaria save format
    /// </summary>
    public class PixelariaSaverLoader
    {
        /// <summary>
        /// The version of this Pixelaria persistence handler
        /// </summary>
        private static int version = 9;

        /// <summary>
        /// Gets the version of this Pixelaria persistence handler
        /// </summary>
        public static int Version { get { return version; } }

        #region Loading

        /// <summary>
        /// Loads a bundle from disk
        /// </summary>
        /// <param name="path">The path to load the bundle from</param>
        /// <returns>The bundle that was loaded from the given path</returns>
        public static Bundle LoadBundleFromDisk(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            // Signature Block
            if (reader.ReadByte() != 'P' || reader.ReadByte() != 'X' || reader.ReadByte() != 'L')
            {
                return null;
            }

            // Bundle Header block
            int bundleVersion = reader.ReadInt32();
            string bundleName = reader.ReadString();
            string bundlePath = "";

            // New pixelaria file format
            if (bundleVersion >= 9)
            {
                stream.Close();

                return PixelariaSaverLoader.LoadFileFromDisk(path).LoadedBundle;
            }

            ////////
            //// Legacy file formats
            ////////

            if (bundleVersion >= 3)
            {
                bundlePath = reader.ReadString();
            }

            Bundle bundle = new Bundle(bundleName);

            bundle.ExportPath = bundlePath;

            // Animation block
            int animCount = reader.ReadInt32();

            for (int i = 0; i < animCount; i++)
            {
                bundle.AddAnimation(LoadAnimationFromStream(stream, bundleVersion));
            }

            // The next block (Animation Sheets) are only available on version 2 and higher
            if (bundleVersion == 1)
                return bundle;

            // AnimationSheet block
            int sheetCount = reader.ReadInt32();

            for (int i = 0; i < sheetCount; i++)
            {
                bundle.AddAnimationSheet(LoadAnimationSheetFromStream(stream, bundle, bundleVersion));
            }

            stream.Close();

            return bundle;
        }

        /// <summary>
        /// Loads a Pixelaria (.plx) file from disk
        /// </summary>
        /// <param name="path">The path of the file to load</param>
        /// <returns>A new Pixelaria file</returns>
        public static PixelariaFile LoadFileFromDisk(string path)
        {
            FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            BinaryReader reader = new BinaryReader(stream);

            // Signature Block
            if (reader.ReadByte() != 'P' || reader.ReadByte() != 'X' || reader.ReadByte() != 'L')
            {
                return null;
            }

            // Bundle Header block
            int bundleVersion = reader.ReadInt32();
            stream.Close();

            PixelariaFile file;

            ////////
            //// Version 9 and later
            ////////
            if (bundleVersion >= 9)
            {
                file = new PixelariaFile(new Bundle("Name"), path);

                PixelariaFileLoader.Load(file);

                return file;
            }

            Bundle bundle = LoadBundleFromDisk(path);

            file = new PixelariaFile(bundle, path);
            file.PrepareBlocksWithBundle();

            return file;
        }

        #endregion

        #region Saving

        /// <summary>
        /// Saves the given bundle to disk
        /// </summary>
        /// <param name="bundle">The bundle to save</param>
        /// <param name="path">The path to save the bundle to</param>
        public static void SaveBundleToDisk(Bundle bundle, string path)
        {
            PixelariaFile file = new PixelariaFile(bundle, path);

            SaveFileToDisk(file);

            file.CurrentStream.Close();
        }

        /// <summary>
        /// Saves the given PixelariaFile object to disk
        /// </summary>
        /// <param name="file">The file to save to disk</param>
        public static void SaveFileToDisk(PixelariaFile file)
        {
            PixelariaFileSaver.Save(file);
        }

        #endregion

        #region Version 8 and prior loader

        /// <summary>
        /// Loads an Animation from the given stream, using the specified version
        /// number when reading properties
        /// </summary>
        /// <param name="stream">The stream to load the animation from</param>
        /// <param name="version">The version that the stream was written on</param>
        /// <returns>The Animation object loaded</returns>
        public static Animation LoadAnimationFromStream(Stream stream, int version)
        {
            BinaryReader reader = new BinaryReader(stream);

            int id = -1;
            if (version >= 2)
            {
                id = reader.ReadInt32();
            }
            string name = reader.ReadString();
            int width = reader.ReadInt32();
            int height = reader.ReadInt32();
            int fps = reader.ReadInt32();
            bool frameskip = reader.ReadBoolean();

            Animation anim = new Animation(name, width, height);

            anim.ID = id;
            anim.PlaybackSettings.FPS = fps;
            anim.PlaybackSettings.FrameSkip = frameskip;

            int frameCount = reader.ReadInt32();

            for (int i = 0; i < frameCount; i++)
            {
                anim.AddFrame(LoadFrameFromStream(stream, anim, version));
            }

            return anim;
        }

        /// <summary>
        /// Loads a Frame from the given stream, using the specified version
        /// number when reading properties
        /// </summary>
        /// <param name="stream">The stream to load the frame from</param>
        /// <param name="owningAnimation">The Animation object that will be used to create the Frame with</param>
        /// <param name="version">The version that the stream was written on</param>
        /// <returns>The Frame object loaded</returns>
        public static Frame LoadFrameFromStream(Stream stream, Animation owningAnimation, int version)
        {
            BinaryReader reader = new BinaryReader(stream);

            // Read the size of the frame texture
            long textSize = reader.ReadInt64();

            Frame frame = new Frame(owningAnimation, owningAnimation.Width, owningAnimation.Height, false);

            MemoryStream memStream = new MemoryStream();

            long pos = stream.Position;

            byte[] buff = new byte[textSize];
            stream.Read(buff, 0, buff.Length);
            stream.Position = pos + textSize;

            memStream.Write(buff, 0, buff.Length);

            Image img = Image.FromStream(memStream);

            // The Bitmap constructor is used here because images loaded from streams are read-only and cannot be directly edited
            Bitmap bitmap = new Bitmap(img);

            img.Dispose();

            if (version >= 8)
            {
                frame.ID = reader.ReadInt32();
            }

            // Get the hash now
            byte[] hash = null;

            if (version >= 6)
            {
                int length = reader.ReadInt32();
                hash = new byte[length];
                stream.Read(hash, 0, length);
            }
            else
            {
                memStream.Position = 0;
                hash = Utilities.GetHashForStream(memStream);
            }

            memStream.Dispose();

            frame.SetFrameBitmap(bitmap, false);
            frame.SetHash(hash);

            return frame;
        }

        /// <summary>
        /// Loads an AnimationSheet from the given stream, using the specified version
        /// number when reading properties
        /// </summary>
        /// <param name="stream">The stream to load the animation sheet from</param>
        /// <param name="parentBundle">The bundle that will contain this AnimationSheet</param>
        /// <param name="version">The version that the stream was written on</param>
        /// <returns>The Animation object loaded</returns>
        public static AnimationSheet LoadAnimationSheetFromStream(Stream stream, Bundle parentBundle, int version)
        {
            BinaryReader reader = new BinaryReader(stream);

            // Load the animation sheet data
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            AnimationExportSettings settings = LoadExportSettingsFromStream(stream, version);

            // Create the animation sheet
            AnimationSheet sheet = new AnimationSheet(name);

            sheet.ID = id;
            sheet.ExportSettings = settings;

            // Load the animation indices
            int animCount = reader.ReadInt32();

            for (int i = 0; i < animCount; i++)
            {
                Animation anim = parentBundle.GetAnimationByID(reader.ReadInt32());

                if (anim != null)
                {
                    sheet.AddAnimation(anim);
                }
            }

            return sheet;
        }

        /// <summary>
        /// Loads an AnimationExportSettings from the given stream, using the specified
        /// version number when reading properties
        /// </summary>
        /// <param name="stream">The stream to load the export settings from</param>
        /// <param name="version">The version that the stream was writter on</param>
        /// <returns>The AnimationExportSettings object loaded</returns>
        public static AnimationExportSettings LoadExportSettingsFromStream(Stream stream, int version)
        {
            BinaryReader reader = new BinaryReader(stream);

            AnimationExportSettings settings = new AnimationExportSettings();

            settings.FavorRatioOverArea = reader.ReadBoolean();
            settings.ForcePowerOfTwoDimensions = reader.ReadBoolean();
            settings.ForceMinimumDimensions = reader.ReadBoolean();
            settings.ReuseIdenticalFramesArea = reader.ReadBoolean();
            if (version >= 4)
            {
                settings.HighPrecisionAreaMatching = reader.ReadBoolean();
            }

            settings.AllowUnorderedFrames = reader.ReadBoolean();

            if (version >= 7)
            {
                settings.UseUniformGrid = reader.ReadBoolean();
            }
            else
            {
                settings.UseUniformGrid = false;
            }

            settings.UsePaddingOnXml = reader.ReadBoolean();

            if (version >= 5)
            {
                settings.ExportXml = reader.ReadBoolean();
            }
            else
            {
                settings.ExportXml = true;
            }
            settings.XPadding = reader.ReadInt32();
            settings.YPadding = reader.ReadInt32();

            return settings;
        }

        #endregion
    }

    /// <summary>
    /// Encapsulates a Pixelaria .plx file
    /// </summary>
    public class PixelariaFile : IDisposable
    {
        /// <summary>
        /// The version of this Pixelaria file
        /// </summary>
        protected int version = 9;

        /// <summary>
        /// The Bundle binded to this PixelariaFile
        /// </summary>
        protected Bundle bundle;

        /// <summary>
        /// The stream containing this file
        /// </summary>
        protected Stream stream;

        /// <summary>
        /// The path to the .plx file to manipulate
        /// </summary>
        protected string filePath;

        /// <summary>
        /// The list of blocks currently on the file
        /// </summary>
        protected List<FileBlock> blockList;

        /// <summary>
        /// Gets or sets the version of this PixelariaFile
        /// </summary>
        public int Version { get { return version; } set { version = value; } }

        /// <summary>
        /// Gets the Bundle binded to this PixelariaFile
        /// </summary>
        public Bundle LoadedBundle
        {
            get { return bundle; }
        }

        /// <summary>
        /// Gets the current stream containing the file
        /// </summary>
        public Stream CurrentStream
        {
            get { return stream; }
            set { stream = value; }
        }

        /// <summary>
        /// The path to the .plx file to manipulate
        /// </summary>
        public string FilePath
        {
            get { return filePath; }
        }

        /// <summary>
        /// Gets the list of blocks currently in this PixelariaFile
        /// </summary>
        public FileBlock[] Blocks
        {
            get { return blockList.ToArray(); }
        }

        /// <summary>
        /// Gets the number of blocks inside this PixelariaFile
        /// </summary>
        public int BlockCount { get { return blockList.Count; } }

        /// <summary>
        /// Initializes a new instance of the PixelariaFile class
        /// </summary>
        /// <param name="bundle">The bundle to bind to this PixelariaFile</param>
        /// <param name="filePath">The path to the .plx file to manipulate</param>
        public PixelariaFile(Bundle bundle, string filePath)
        {
            this.filePath = filePath;
            this.bundle = bundle;
            blockList = new List<FileBlock>();

            AddDefaultBlocks();
        }

        /// <summary>
        /// Initializes a new instance of the PixelariaFile class
        /// </summary>
        /// <param name="bundle">The bundle to bind to this PixelariaFile</param>
        /// <param name="stream">The stream used to load/save the PixelariaFile</param>
        public PixelariaFile(Bundle bundle, Stream stream)
        {
            filePath = "";
            this.bundle = bundle;
            this.stream = stream;
            blockList = new List<FileBlock>();

            AddDefaultBlocks();
        }

        /// <summary>
        /// Disposes of this PixelariaFile and all used resources
        /// </summary>
        public void Dispose()
        {
            foreach (BaseBlock block in blockList)
            {
                block.Dispose();
            }
            blockList.Clear();
            blockList = null;
        }

        /// <summary>
        /// Adds a block to this file's composition
        /// </summary>
        /// <param name="block">The block to add to this PixelariaFile</param>
        public void AddBlock(FileBlock block)
        {
            blockList.Add(block);
            block.OwningFile = this;
        }

        /// <summary>
        /// <para>Adds the default block definitions to this PixelariaFile.</para>
        /// <para>The default blocks added are:</para>
        /// <list type="bullet">
        /// <item><description>AnimationBlock</description></item>
        /// <item><description>AnimationSheetBlock</description></item>
        /// <item><description>ProjectTreeBlock</description></item>
        /// </list>
        /// </summary>
        public void AddDefaultBlocks()
        {
            if (GetBlocksByType(typeof(AnimationBlock)).Length == 0)
            {
                AddBlock(new AnimationBlock());
            }
            if (GetBlocksByType(typeof(AnimationSheetBlock)).Length == 0)
            {
                AddBlock(new AnimationSheetBlock());
            }
            if (GetBlocksByType(typeof(ProjectTreeBlock)).Length == 0)
            {
                AddBlock(new ProjectTreeBlock());
            }
        }

        /// <summary>
        /// Removes a block from this file's composition
        /// </summary>
        /// <param name="block">The block to remove</param>
        /// <param name="dispose">Whether to dispose of the block after its removal</param>
        public void RemoveBlock(FileBlock block, bool dispose = true)
        {
            blockList.Remove(block);
            if (dispose)
            {
                block.Dispose();
            }
        }

        /// <summary>
        /// Removes all file blocks in this file's composition
        /// </summary>
        public void ClearBlockList()
        {
            foreach (var block in blockList)
            {
                block.Dispose();
            }

            blockList.Clear();
        }

        /// <summary>
        /// Prepares the blocks with the currently loaded bundle
        /// </summary>
        public void PrepareBlocksWithBundle()
        {
            foreach (FileBlock block in blockList)
            {
                block.PrepareFromBundle(bundle);
            }
        }

        /// <summary>
        /// Returns a list of blocks that match a given type
        /// </summary>
        /// <param name="blockType">The block type to match</param>
        /// <returns>A list of all the blocks that match the given type</returns>
        public FileBlock[] GetBlocksByType(Type blockType)
        {
            List<FileBlock> blocks = new List<FileBlock>();

            foreach (FileBlock block in blockList)
            {
                if (block.GetType() == blockType)
                {
                    blocks.Add(block);
                }
            }

            return blocks.ToArray();
        }

        /// <summary>
        /// Gets all the blocks inside this PixelariaFile that match the given blockID
        /// </summary>
        /// <param name="blockID">The blockID to match</param>
        /// <returns>All the blocks that match the given ID inside this PixelariaFile</returns>
        public FileBlock[] GetBlocksByID(short blockID)
        {
            return blockList.Where<FileBlock>((FileBlock block) => block.BlockID == blockID).ToArray<FileBlock>();
        }
    }

    /// <summary>
    /// Encapsulates a Version 9 and later block-composed file loader
    /// </summary>
    public class PixelariaFileLoader
    {
        /// <summary>
        /// The file to load
        /// </summary>
        private PixelariaFile _file;

        /// <summary>
        /// Whether to reset the bundle before loading contents from the stream
        /// </summary>
        private bool _resetBundle;

        /// <summary>
        /// Initializes a new instance of the PixelariaFileLoader class
        /// </summary>
        /// <param name="file">The file to load from the stream</param>
        /// <param name="resetBundle">Whether to reset the bundle to a clear state before loading the new file</param>
        public PixelariaFileLoader(PixelariaFile file, bool resetBundle)
        {
            _file = file;
            _resetBundle = resetBundle;
        }

        /// <summary>
        /// Loads the contents of a PixelariaFile
        /// </summary>
        public void Load()
        {
            // Get the stream to load the file from
            Stream stream = _file.CurrentStream;
            bool closeStream = false;
            if(stream == null)
            {
                _file.CurrentStream = stream = new FileStream(_file.FilePath, FileMode.Open, FileAccess.Read);
                closeStream = true;
            }

            // Read the header
            BinaryReader reader = new BinaryReader(stream);

            // Signature Block
            if (reader.ReadByte() != 'P' || reader.ReadByte() != 'X' || reader.ReadByte() != 'L')
            {
                return;
            }

            if (_resetBundle)
            {
                // Reset the bundle beforehands
                _file.LoadedBundle.Clear();
            }

            // Bundle Header block
            _file.Version = reader.ReadInt32();
            _file.LoadedBundle.Name = reader.ReadString();
            _file.LoadedBundle.ExportPath = reader.ReadString();

            // Load the blocks
            while (stream.Position < stream.Length)
            {
                _file.AddBlock(FileBlock.FromStream(stream, _file));
            }

            if (closeStream)
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Loads a pixelaria file's contents from its stream or file path
        /// </summary>
        /// <param name="file">A valid PixelariaFile with a stream of valid file path set</param>
        /// <param name="resetBundle">Whether to reset the bundle to a clear state before loading the new file</param>
        public static void Load(PixelariaFile file, bool resetBundle = true)
        {
            // TODO: Verify correctess of clearing the pixelaria file's internal blocks list before loading the file from the stream again
            file.ClearBlockList();
            PixelariaFileLoader loader = new PixelariaFileLoader(file, resetBundle);
            loader.Load();
        }
    }

    /// <summary>
    /// Encapsulates a Version 9 and later block-composed file saver
    /// </summary>
    public class PixelariaFileSaver
    {
        /// <summary>
        /// The file to save
        /// </summary>
        private PixelariaFile file;

        /// <summary>
        /// Initializes a new instance of the PixelariaFileLoader class
        /// </summary>
        /// <param name="file">The file to save to the stream</param>
        public PixelariaFileSaver(PixelariaFile file)
        {
            this.file = file;
        }

        /// <summary>
        /// Saves the contents of a PixelariaFile
        /// </summary>
        public void Save()
        {
            // Get the stream to load the file from
            Stream stream = file.CurrentStream;
            bool closeStream = false;
            if (stream == null)
            {
                file.CurrentStream = stream = new FileStream(file.FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                closeStream = true;
            }

            stream.SetLength(0);

            // Save the header
            BinaryWriter writer = new BinaryWriter(stream);

            // Signature Block
            writer.Write((byte)'P');
            writer.Write((byte)'X');
            writer.Write((byte)'L');
            
            // Bundle Header block
            writer.Write(file.Version);
            writer.Write(file.LoadedBundle.Name);
            writer.Write(file.LoadedBundle.ExportPath);

            // Save the blocks
            foreach (FileBlock block in file.Blocks)
            {
                block.PrepareFromBundle(file.LoadedBundle);
                block.SaveToStream(stream);
            }

            // Truncate the stream so any unwanted extra data is not left pending, that can lead to potential crashes when reading the file back again
            stream.SetLength(stream.Position);

            if (closeStream)
            {
                stream.Close();
            }
        }

        /// <summary>
        /// Saves a pixelaria file's contents to its stream or file path
        /// </summary>
        /// <param name="file">A valid PixelariaFile with a stream of valid file path set</param>
        public static void Save(PixelariaFile file)
        {
            PixelariaFileSaver saver = new PixelariaFileSaver(file);
            saver.Save();
        }
    }
}