// Licensed under the MIT License.

namespace Coimbra
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Coimbra.Communication;
    using Coimbra.Helpers;
    using Coimbra.Model;
    using Coimbra.Pages;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A page to select song and nickname for multi playing.
    /// </summary>
    public sealed partial class BandSongPage : Page
    {
        private bool sendTheSongToOtherPlayers;

        /// <summary>
        /// Initializes a new instance of the <see cref="BandSongPage"/> class.
        /// </summary>
        public BandSongPage()
        {
            this.InitializeComponent();

            this.SetCustomEventHandlers();

            //TODO: Display a loading bar when initializing this BandSongPage
            Task.Run(NetworkDataSender.ConnectToAllServersAsync).Wait();

            this.ShowOrHideBandControls();
        }

        private static void NoIpAddressFound()
        {
        }

        private void ShowOrHideBandControls()
        {
            this.pnlNickNameInfo.Visibility = Visibility.Collapsed;
            this.pnlSelectSong.Visibility = Visibility.Collapsed;
            this.pnlAllPlayersAreReady.Visibility = Visibility.Collapsed;
            this.lblReceivedSongName.Visibility = Visibility.Collapsed;
            this.lblReceivedSongNameText.Visibility = Visibility.Collapsed;
            this.btnStartMultiplayerGame.Visibility = Visibility.Collapsed;
            this.pnlOtherPlayers.Visibility = Visibility.Collapsed;
            this.sendTheSongToOtherPlayers = true;
            this.ShowOrHideSongSelection(false);
        }

        private void ShowOrHideSongSelection(bool show)
        {
            var visibility = show ? Visibility.Visible : Visibility.Collapsed;
            this.pnlNickNameInfo.Visibility = visibility;
            this.pnlSelectSong.Visibility = visibility;
            this.lblSelectSong.Visibility = visibility;
            this.FilePicker.Visibility = visibility;
            this.lblSelectSong.Visibility = visibility;
            this.btnUseSelectedSong.Visibility = visibility;
            this.SongsListBox.Visibility = visibility;
        }

        private void ShowHideNickNameEntered()
        {
            this.lblNickname.Visibility = Visibility.Visible;
            this.lblEnterNickname.Visibility = Visibility.Collapsed;
            this.txtNickName.Visibility = Visibility.Collapsed;
            this.btnJoin.Visibility = Visibility.Collapsed;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) =>
            _ = this.Frame.Navigate(typeof(DurationPage), null, new DrillInNavigationTransitionInfo());

        private void BackButton_Click(object sender, RoutedEventArgs e) => _ = this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());

        private async void FilePicker_Click(object sender, RoutedEventArgs e)
        {
            var songFilePath = await SongPagesHelper.UploadSongAsync().ConfigureAwait(true);
            UserData.Song = this.SongsListBox.SelectedValue.ToString();

            Thread.Sleep(1000);
            if (songFilePath != null)
            {
                SongPagesHelper.FillListBoxAsync(this.SongsListBox, songFilePath, null).GetAwaiter().GetResult();
            }
        }

        private void SongsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UserData.Song = this.SongsListBox.SelectedValue.ToString();

        private void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(this.txtNickName.Text)
                && !MultiPlayerData.OtherPlayers.ContainsKey(this.txtNickName.Text))
            {
                UserData.NickName = this.txtNickName.Text;
                this.lblNickname.Text = UserData.NickName;
                this.ShowHideNickNameEntered();
                this.ShowOrHideSongSelection(true);
                NetworkDataSender.SendPlayerInfo();
                _ = SongPagesHelper.FillListBoxAsync(this.SongsListBox, null, null).Id;
            }
        }

        private void BtnUseSelectedSong_Click(object sender, RoutedEventArgs e)
        {
            UserData.Song = this.SongsListBox.SelectedValue.ToString();
            _ = SongPagesHelper.AddMidiAsync(this.sendTheSongToOtherPlayers).ConfigureAwait(true);
            this.lblReceivedSongName.Text = Path.GetFileNameWithoutExtension(UserData.Song);
            this.lblReceivedSongName.Visibility = Visibility.Visible;
            this.lblReceivedSongNameText.Visibility = Visibility.Visible;
            this.ShowOrHideSongSelection(false);
            this.btnStartMultiplayerGame.Visibility = Visibility.Visible;
        }

        private void BtnStartMultiplayerGame_Click(object sender, RoutedEventArgs e)
        {
            NetworkDataSender.SendPlayerReadyInfo();
            this.btnStartMultiplayerGame.Visibility = Visibility.Collapsed;
            if (MultiPlayerData.OtherPlayers.Values.All(x => x.ReadyToStart))
            {
                this.pnlAllPlayersAreReady.Visibility = Visibility.Visible;
                this.NextButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// SetCustomEventHandlers.
        /// </summary>
        private void SetCustomEventHandlers()
        {
            NetworkDataSender.OnNoIpAddressFound += BandSongPage.NoIpAddressFound;
            NetworkListener.OnMidiFileReceived += this.SetSongFromNetwork;
            NetworkListener.OnPlayerInfoReceived += this.NewPlayerJoined;
            NetworkListener.OnPlayerReadyInfoReceived += this.PlayerReadyInfoRecieved;
        }

        /// <summary>
        /// SetSongFromNetwork.
        /// </summary>
        /// <param name="args">MidiFileReceivedEventArguments.</param>
        private async void SetSongFromNetwork(MidiFileReceivedEventArguments args) => await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal,
            async () =>
            {
                this.ShowOrHideSongSelection(false);
                this.lblReceivedSongName.Text = Path.GetFileNameWithoutExtension(args.FilePath);
                this.lblReceivedSongName.Visibility = Visibility.Visible;
                this.lblReceivedSongNameText.Visibility = Visibility.Visible;

                UserData.Song = args.FilePath;
                await SongPagesHelper.AddMidiAsync().ConfigureAwait(true);
                this.sendTheSongToOtherPlayers = false;
                this.btnStartMultiplayerGame.Visibility = Visibility.Visible;
            });

        /// <summary>
        /// NewPlayerJoined.
        /// </summary>
        /// <param name="args">PlayerInfoReceivedEventArguments.</param>
        private async void NewPlayerJoined(PlayerInfoReceivedEventArguments args) => await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal,
            () =>
            {
                this.pnlOtherPlayers.Visibility = Visibility.Visible;
                var otherPlayerNumber = 0;
                this.lblOtherPlayerNames.Text = string.Join(Environment.NewLine, MultiPlayerData.OtherPlayers.Values
                    .Select(x => (++otherPlayerNumber).ToString(CultureInfo.InvariantCulture) + ". " + x.NickName).ToArray());
            });

        /// <summary>
        /// PlayerReadyInfoRecieved.
        /// </summary>
        /// <param name="args">PlayerInfoReceivedEventArguments.</param>
        private async void PlayerReadyInfoRecieved(PlayerInfoReceivedEventArguments args) => await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
            CoreDispatcherPriority.Normal,
            () =>
            {
                var otherPlayerNumber = 0;
                this.lblOtherPlayerNames.Text =
                string.Join(
                    Environment.NewLine,
                    MultiPlayerData.OtherPlayers.Values.Select(x => (++otherPlayerNumber).ToString(CultureInfo.InvariantCulture) + ". " + x.NickName + (x.ReadyToStart ? " - Ready" : string.Empty))
                    .ToArray());

                if (MultiPlayerData.OtherPlayers.Values.All(x => x.ReadyToStart))
                {
                    this.pnlAllPlayersAreReady.Visibility = Visibility.Visible;
                    this.NextButton.IsEnabled = true;
                }
            });
    }
}
