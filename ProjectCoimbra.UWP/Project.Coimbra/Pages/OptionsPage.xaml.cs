// Licensed under the MIT License.

namespace Coimbra.Pages
{
	using Coimbra.Helpers;
	using Coimbra.Model;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the mode selection page of the app.
    /// </summary>
    public sealed partial class OptionsPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsPage"/> class.
        /// </summary>
        public OptionsPage() =>
            this.InitializeComponent();


		private void AddOptionChangeFlag() {
			UserData.IsOptionChangeMode = true;
		}

		private async void BtnPlayAgain_Click(object sender, RoutedEventArgs e)
		{
			await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
            _ = this.Frame.Navigate(typeof(GamePage), null, new DrillInNavigationTransitionInfo());
        }

		private async void BtnChangeNoteDuration_Click(object sender, RoutedEventArgs e)
		{
			this.AddOptionChangeFlag();

			await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
			_ = this.Frame.Navigate(typeof(DurationPage), null, new DrillInNavigationTransitionInfo());
		}

		private async void BtnChangeInstrument_Click(object sender, RoutedEventArgs e)
		{
			this.AddOptionChangeFlag();

			await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
			_ = this.Frame.Navigate(typeof(InstrumentsPage), null, new DrillInNavigationTransitionInfo());
		}

		private void BtnChangeSong_Click(object sender, RoutedEventArgs e)
		{
			_ = this.Frame.Navigate(typeof(SongPage), null, new DrillInNavigationTransitionInfo());
		}

		private async void BtnChangeLaneSettings_Click(object sender, RoutedEventArgs e)
		{
			await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
			_ = this.Frame.Navigate(typeof(LaneSettingsPage), null, new DrillInNavigationTransitionInfo());
		}

		private void BtnChangeMode_Click(object sender, RoutedEventArgs e)
		{
			_ = this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());
		}
	}
}
