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
    using Coimbra.Midi.Models;
    using Coimbra.Model;
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Standards;
    using Windows.ApplicationModel.Resources;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the instrument selection page of the app.
    /// </summary>
    public sealed partial class InstrumentsPage : Page
    {

        private static readonly List<InstrumentInfo> Instruments = new List<InstrumentInfo>();

        private readonly MidiEngine midiEngine = MidiEngine.Instance;

        private readonly ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
        private string waitingIndicatorResource = string.Empty;

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

            // Get the strings from resources
            waitingIndicatorResource = resourceLoader.GetString("InstrumentsPage/Waiting/Text");
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

        private void RenderInstruments(IDictionary<FourBitNumber, InstrumentInfo> dictionary)
        {
            Instruments.Clear();
            Instruments
                .AddRange(dictionary.OrderByDescending(x => x.Value.NoteCount)
                .Where(dict => dict.Value.NoteCount > 0)
                .Select(dict => dict.Value));

            this.InstrumentsBox.ItemsSource = Instruments;
            if (Instruments.Count != 0)
            {
                this.InstrumentsBox.SelectedItem = Instruments[0];
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            this.midiEngine.SelectTrack(((InstrumentInfo)this.InstrumentsBox.SelectedItem).Channel);

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
        /// Set the event handlers.
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
                            : $" - {waitingIndicatorResource}"))
                    .ToArray());

            this.CheckAndStartTheGame();
        }

        private void BtnInstrumentSelected_OnClick(object sender, RoutedEventArgs e)
        {
            this.midiEngine.SelectTrack(((InstrumentInfo)this.InstrumentsBox.SelectedItem).Channel);
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
