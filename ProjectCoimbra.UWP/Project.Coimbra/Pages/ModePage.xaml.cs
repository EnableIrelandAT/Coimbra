// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using Coimbra.Model;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the mode selection page of the app.
    /// </summary>
    public sealed partial class ModePage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModePage"/> class.
        /// </summary>
        public ModePage() =>
            this.InitializeComponent();

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            _ = this.Frame.Navigate(typeof(TermsPage), null, new DrillInNavigationTransitionInfo());

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.SoloRadio.IsChecked.GetValueOrDefault())
            {
                UserData.GameMode = UserData.Mode.Solo;
                _ = this.Frame.Navigate(typeof(SongPage), null, new DrillInNavigationTransitionInfo());
            }
            else if (this.OffBandRadio.IsChecked.GetValueOrDefault())
            {
                UserData.GameMode = UserData.Mode.Offline;
                _ = this.Frame.Navigate(typeof(SongPage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                UserData.GameMode = UserData.Mode.Online;
                _ = this.Frame.Navigate(typeof(BandSongPage), null, new DrillInNavigationTransitionInfo());
            }
        }
    }
}
