// Licensed under the MIT License.

namespace Coimbra.Pages
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Globalization;
	using System.Linq;
	using System.Text.RegularExpressions;
	using Coimbra.Communication;
	using Coimbra.Midi;
	using Coimbra.Model;
	using Melanchall.DryWetMidi.Common;
	using Melanchall.DryWetMidi.Standards;
	using Windows.UI.Core;
	using Windows.UI.Xaml;
	using Windows.UI.Xaml.Controls;
	using Windows.UI.Xaml.Media.Animation;

	/// <summary>
	/// A class encapsulating the logic of the instrument selection page of the app.
	/// </summary>
	public sealed partial class InstrumentsPage : Page
	{
		private static readonly Regex RegularExpression = new Regex("(\\B([A-Z]|[0-9]))", RegexOptions.Compiled);

		private static readonly List<string> Instruments = new List<string>();

		private readonly MidiEngine midiEngine = MidiEngine.Instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstrumentsPage"/> class.
		/// </summary>
		public InstrumentsPage()
		{
			this.InitializeComponent();
			this.RenderInstruments(this.midiEngine.Instruments);
			this.SetCustomEventHandlers();
			this.ShowHideControlsBandSolo();
			this.ShowPlayersInstrumentInfo();
		}

		private void ShowHideControlsBandSolo()
		{
			if (UserData.GameMode == UserData.Mode.Solo || UserData.GameMode == UserData.Mode.Offline)
			{
				this.lblOtherPlayers.Visibility = Visibility.Collapsed;
				this.lblOtherPlayerNames.Visibility = Visibility.Collapsed;
				this.lblOtherPlayersText.Visibility = Visibility.Collapsed;
				this.btnInstrumentSelected.Visibility = Visibility.Collapsed;
				this.NextButton.Visibility = Visibility.Visible;
			}
			else
			{
				this.lblOtherPlayers.Visibility = Visibility.Visible;
				this.lblOtherPlayerNames.Visibility = Visibility.Visible;
				this.lblOtherPlayersText.Visibility = Visibility.Visible;
				this.btnInstrumentSelected.Visibility = Visibility.Visible;
				this.NextButton.Visibility = Visibility.Collapsed;
			}
		}

		private void RenderInstruments(IDictionary<FourBitNumber, ICollection<SevenBitNumber>> dictionary)
		{
			Instruments.Clear();
			Instruments.AddRange(dictionary.ToImmutableSortedDictionary().Select(dict => string.Join(
				", ",
				dict.Value.Select(d =>
					RegularExpression.Replace(
						Enum.GetName(typeof(GeneralMidi2Program), (int)d) ?? throw new InvalidOperationException(),
						" $1")))));

			this.InstrumentsBox.ItemsSource = Instruments;
			if (Instruments.Count != 0)
			{
				this.InstrumentsBox.SelectedValue = Instruments[0];
			}
		}

		private void InstrumentButton_Click(object sender, RoutedEventArgs e)
		{
			var content = ((Button)sender).Content;
			if (content == null)
			{
				return;
			}

			var buttonText = content.ToString();
			this.midiEngine.SelectTrack(Convert.ToInt32(buttonText.Substring(0, length: buttonText.IndexOf(":", StringComparison.OrdinalIgnoreCase)), CultureInfo.InvariantCulture));
		}

		private void NextButton_Click(object sender, RoutedEventArgs e)
		{
			this.midiEngine.SelectTrack(this.InstrumentsBox.SelectedIndex);

			if (UserData.IsOptionChangeMode)
			{
				_ = this.Frame.Navigate(typeof(GamePage), null, new DrillInNavigationTransitionInfo());
			}
			else
			{
				_ = this.Frame.Navigate(typeof(LaneSettingsPage), null, new DrillInNavigationTransitionInfo());
			}
		}

		private void BackButton_Click(object sender, RoutedEventArgs e) =>
			_ = this.Frame.Navigate(typeof(DurationPage), null, new DrillInNavigationTransitionInfo());

		/// <summary>
		/// To ADD.
		/// </summary>
		private void SetCustomEventHandlers()
		{
			NetworkListener.OnPlayerInstrumentInfoReceived += this.PlayerInstrumentInfoRecieved;
			NetworkListener.OnStartTimeInfoReceived += this.StartTimeInfoReceived;
		}

		/// <summary>
		/// PlayerInstrumentInfoRecieved.
		/// </summary>
		/// <param name="args">PlayerInfoReceivedEventArguments.</param>
		private async void PlayerInstrumentInfoRecieved(PlayerInfoReceivedEventArguments args) => await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
			CoreDispatcherPriority.Normal,
			this.ShowPlayersInstrumentInfo);

		/// <summary>
		/// StartTimeInfoReceived.
		/// </summary>
		/// <param name="args">StartTimeInfoReceivedEventArgs.</param>
		private async void StartTimeInfoReceived(StartTimeInfoReceivedEventArgs args) => await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
			CoreDispatcherPriority.Normal,
			() => _ = this.Frame.Navigate(typeof(LaneSettingsPage), null, new DrillInNavigationTransitionInfo()));

		private void ShowPlayersInstrumentInfo()
		{
			var otherPlayerNumber = 0;

			this.lblOtherPlayerNames.Text = string.Join(
				Environment.NewLine,
				MultiPlayerData.OtherPlayers.Values
					.Select(x => (++otherPlayerNumber).ToString(CultureInfo.InvariantCulture) + ". " +
					x.NickName +
						(x.Instrument > -1
							? " - " +
							Instruments[x.Instrument]
							: " - Waiting..."))
					.ToArray());

			this.CheckAndStartTheGame();
		}

		private void BtnInstrumentSelected_OnClick(object sender, RoutedEventArgs e)
		{
			this.midiEngine.SelectTrack(this.InstrumentsBox.SelectedIndex);
			NetworkDataSender.SendPlayerInstrumentInfo(this.InstrumentsBox.SelectedIndex);
			this.btnInstrumentSelected.Visibility = Visibility.Collapsed;
			this.CheckAndStartTheGame();
		}

		private void CheckAndStartTheGame()
		{
			if (!MultiPlayerData.OtherPlayers.Values.Any(x => x.Instrument < 0))
			{
				this.lblOtherPlayers.Visibility = Visibility.Collapsed;

				if (UserData.IsMultiplayerConductor)
				{
					MultiPlayerData.StartTime = DateTime.UtcNow.AddSeconds(10);
					NetworkDataSender.SendStartTimeInfo(MultiPlayerData.StartTime);
					var frame = (Frame)Window.Current.Content;
					_ = frame.Navigate(typeof(LaneSettingsPage), null, new DrillInNavigationTransitionInfo());
				}
			}
		}
	}
}
