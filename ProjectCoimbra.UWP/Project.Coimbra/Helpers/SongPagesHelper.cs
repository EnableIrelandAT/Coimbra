// Licensed under the MIT License.

namespace Coimbra.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Coimbra.Communication;
    using Coimbra.Midi;
    using Coimbra.Model;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.Storage;
    using Windows.Storage.Pickers;
    using Windows.Storage.Search;
    using Windows.Storage.Streams;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Song Pages Helper.
    /// </summary>
    public static class SongPagesHelper
    {
        private const string MidiSongsFolder = "MidiSongs";

        /// <summary>
        /// GetSongsAsync.
        /// </summary>
        /// <returns>IDictionary.</returns>
        public static async Task<IDictionary<string, string>> GetSongsAsync()
        {
            var midiFolder = await GetMidiFolderAsync(false).ConfigureAwait(true);
            if (midiFolder == null)
            {
                return null;
            }

            var queryOption = new QueryOptions(CommonFileQuery.OrderByTitle, new[] { ".mid", ".midi" });
            var files = await midiFolder.GetFilesAsync(); // CreateFileQueryWithOptions(queryOption) does not load all files
            return files.ToDictionary(file => file.Path, file => file.DisplayName);
        }

        /// <summary>
        /// GetMidiFolderAsync.
        /// </summary>
        /// <param name="createIfNonExistent">createIfNonExistent.</param>
        /// <returns>StorageFolder.</returns>
        public static async Task<StorageFolder> GetMidiFolderAsync(bool createIfNonExistent)
        {
            var musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            var musicFolder = musicLibrary.Folders.FirstOrDefault();
            if (musicFolder == null)
            {
                return null;
            }

            StorageFolder midiFolder;
            try
            {
                midiFolder = await musicFolder.GetFolderAsync(MidiSongsFolder);
            }
            catch (FileNotFoundException) when (createIfNonExistent)
            {
                midiFolder = await musicFolder.CreateFolderAsync(MidiSongsFolder);
            }

            return midiFolder;
        }

        /// <summary>
        /// AddMidiAsync.
        /// </summary>
        /// <param name="sendTheSongToOtherPlayers">Send The Song To Other Players.</param>
        /// <returns>Task.</returns>
        public static async Task AddMidiAsync(bool sendTheSongToOtherPlayers = false)
        {
            var midiFolder = await GetMidiFolderAsync(false).ConfigureAwait(true);
            if (midiFolder == null)
            {
                return;
            }

            var fileName = Path.GetFileName(UserData.Song);
            var file = await midiFolder.GetFileAsync(fileName);
            var midiEngine = MidiEngine.Instance;
            midiEngine.File = file;
            await midiEngine.OnParseFileAsync().ConfigureAwait(true);

            if (sendTheSongToOtherPlayers)
            {
                NetworkDataSender.SendSelectedSong(file, file.Name);
            }
        }

        /// <summary>
        /// UploadSongAsync.
        /// </summary>
        /// <returns>string.</returns>
        public static async Task<string> UploadSongAsync()
        {
            var openPicker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
                ViewMode = PickerViewMode.Thumbnail,
            };

            openPicker.FileTypeFilter.Add(".midi");
            openPicker.FileTypeFilter.Add(".mid");

            var file = await openPicker.PickSingleFileAsync();
            if (file == null)
            {
                return null;
            }

            return await SaveFile(file);
        }

        /// <summary>
        /// Upload Song by Dropping File Async
        /// </summary>
        /// <param name="e">Drag Event</param>
        /// <returns>Uploaded File path</returns>
        public static async Task<string> UploadSongDropAsync(DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                {
                    var file = items[0] as StorageFile;
                    return await SaveFile(file);
                }
            }

            return null;
        }

        /// <summary>
        /// Save file in Midi Folder
        /// </summary>
        /// <param name="file">File to save</param>
        /// <returns>Saved File path</returns>
        private static async Task<string> SaveFile(StorageFile file)
        {
            var midiFolder = await GetMidiFolderAsync(true).ConfigureAwait(true);
            var destinationFile =
                await midiFolder.CreateFileAsync(file.Name, CreationCollisionOption.GenerateUniqueName);
            using (var readFile = await file.OpenReadAsync())
            {
                using (var readFileStream = readFile.GetInputStreamAt(0))
                {
                    using (var writeFile = await destinationFile.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        using (var writeFileStream = writeFile.GetOutputStreamAt(0))
                        {
                            _ = await RandomAccessStream.CopyAndCloseAsync(readFileStream, writeFileStream);
                        }
                    }
                }
            }
            return destinationFile.Path;
        }

        /// <summary>
        /// FillListBoxAsync.
        /// </summary>
        /// <param name="listBox">Songs ListBox.</param>
        /// <param name="selectedValue">Selected Value.</param>
        /// <param name="nextButton">Next Button.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="listBox"/> is <c>null</c>.</exception>
        public static async Task FillListBoxAsync(ListBox listBox, string selectedValue, Button nextButton)
        {
            if (listBox == null)
            {
                throw new ArgumentNullException(nameof(listBox));
            }

            var songs = await GetSongsAsync().ConfigureAwait(true);
            if (songs == null || songs.Count == 0)
            {
                listBox.Visibility = Visibility.Collapsed;
                if (nextButton != null)
                {
                    nextButton.IsEnabled = false;
                }

                return;
            }

            listBox.ItemsSource = songs;
            listBox.DisplayMemberPath = "Value";
            listBox.SelectedValuePath = "Key";
            if (!string.IsNullOrWhiteSpace(selectedValue))
            {
                listBox.SelectedValue = selectedValue;
            }

            var defaultSong = songs.First().Key;
            if (!string.IsNullOrWhiteSpace(UserData.Song))
            {
                listBox.SelectedValue = songs.ContainsKey(UserData.Song) ? UserData.Song : defaultSong;
            }
            else
            {
                listBox.SelectedValue = songs.First().Key;
            }

            listBox.Visibility = Visibility.Visible;
            if (nextButton != null)
            {
                nextButton.IsEnabled = true;
            }
        }
    }
}
