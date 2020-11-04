// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using System;
    using System.Collections.Generic;
    using Coimbra.Helpers;
    using Coimbra.Model;
    using Windows.ApplicationModel.Resources;
    using Windows.ApplicationModel.DataTransfer;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the song page of the app.
    /// </summary>
    public sealed partial class SongPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SongPage"/> class.
        /// </summary>
        public SongPage()
        {
            this.InitializeComponent();
            _ = SongPagesHelper.FillListBoxAsync(this.SongsListBox, null, this.Next).Id;
        }

        private async void FilePicker_Click(object sender, RoutedEventArgs e)
        {
            this.ErrorBox.Text = string.Empty;
            var songFilePath = await SongPagesHelper.UploadSongAsync().ConfigureAwait(true);
            if (songFilePath != null)
            {
                await SongPagesHelper.FillListBoxAsync(this.SongsListBox, songFilePath, this.Next).ConfigureAwait(false);
            }
        }

        private async void File_Drop(object sender, DragEventArgs e)
        {
            var songFilePath = await SongPagesHelper.UploadSongDropAsync(e).ConfigureAwait(true);
            if (songFilePath != null)
            {
                await SongPagesHelper.FillListBoxAsync(this.SongsListBox, songFilePath, this.Next).ConfigureAwait(true);
            }
        }

        private void File_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.ErrorBox.Text = string.Empty;
            var selectedValue = this.SongsListBox.SelectedValue;
            if (selectedValue != null)
            {
                UserData.Song = selectedValue.ToString();
                await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
                _ = this.Frame.Navigate(typeof(DurationPage), null, new DrillInNavigationTransitionInfo());
            }

            this.Next.IsEnabled = false;
        }

        private async void Remove_Item(object sender, RoutedEventArgs e)
        {
            var res = ResourceLoader.GetForCurrentView();
            this.ErrorBox.Text = string.Empty;

            ContentDialog dialog = new ContentDialog
            {
                Title = res.GetString("SongPage/DeleteDialog/Title"),
                Content = res.GetString("SongPage/DeleteDialog/Content"),
                PrimaryButtonText = res.GetString("SongPage/DeleteDialog/Confirm"),
                CloseButtonText = res.GetString("SongPage/DeleteDialog/Cancel"),
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                var item = (KeyValuePair<string, string>)((FrameworkElement)sender).DataContext;

                bool fileRemoved = await SongPagesHelper.RemoveFileAsync(item.Key).ConfigureAwait(true);
                if (!fileRemoved)
                {
                    this.ErrorBox.Text = res.GetString("SongPage/DeleteSong/Error");
                    return;
                }

                await SongPagesHelper.FillListBoxAsync(this.SongsListBox, null, this.Next).ConfigureAwait(false);
            }
        }
    }
}
