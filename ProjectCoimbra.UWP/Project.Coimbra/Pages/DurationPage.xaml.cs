// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using Coimbra.Model;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the duration selection page of the app.
    /// </summary>
    public sealed partial class DurationPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DurationPage"/> class.
        /// </summary>
        public DurationPage() =>
            this.InitializeComponent();

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            _ = this.Frame.Navigate(UserData.GameMode == UserData.Mode.Solo ? typeof(SongPage) : typeof(BandSongPage), null, new DrillInNavigationTransitionInfo());

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.Long.IsChecked.GetValueOrDefault())
            {
                UserData.ActiveDuration = UserData.Duration.LongDuration;
            }
            else if (this.Medium.IsChecked.GetValueOrDefault())
            {
                UserData.ActiveDuration = UserData.Duration.MediumDuration;
            }
            else
            {
                UserData.ActiveDuration = UserData.Duration.ShortDuration;
            }

            _ = this.Frame.Navigate(typeof(InstrumentsPage), null, new DrillInNavigationTransitionInfo());
        }
    }
}
