// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using Coimbra.Helpers;
    using Coimbra.Model;
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
            var songFilePath = await SongPagesHelper.UploadSongAsync().ConfigureAwait(true);
            if (songFilePath != null)
            {
                await SongPagesHelper.FillListBoxAsync(this.SongsListBox, songFilePath, this.Next).ConfigureAwait(true);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedValue = this.SongsListBox.SelectedValue;
            if (selectedValue != null)
            {
                UserData.Song = selectedValue.ToString();
                await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
                _ = this.Frame.Navigate(typeof(DurationPage), null, new DrillInNavigationTransitionInfo());
            }

            this.Next.IsEnabled = false;
        }
    }
}
