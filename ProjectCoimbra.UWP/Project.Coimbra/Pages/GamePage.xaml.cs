// Licensed under the MIT License.

using Windows.ApplicationModel.Resources;
using Windows.UI;

namespace Coimbra.Pages
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Coimbra.Controls;
    using Coimbra.DryWetMidiIntegration;
    using Coimbra.Midi;
    using Coimbra.Model;
    using Melanchall.DryWetMidi.Core;
    using Microsoft.Toolkit.Uwp.Input.GazeInteraction;
    using Windows.ApplicationModel.Core;
    using Windows.UI.Core;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the game page of the app.
    /// </summary>
    public sealed partial class GamePage : Page
    {
        private readonly MidiEngine midiEngine = MidiEngine.Instance;

        private readonly ConcurrentDictionary<Guid, RenderedMarkablePlaybackEvent> notesOnScreen =
            new ConcurrentDictionary<Guid, RenderedMarkablePlaybackEvent>();

        private readonly DispatcherTimer timer = new DispatcherTimer();

        /// <summary>
        /// Initializes a new instance of the <see cref="GamePage"/> class.
        /// </summary>
        public GamePage()
        {
            if (UserData.ActiveDuration == UserData.Duration.UnknownDuration)
            {
                UserData.ActiveDuration = UserData.Duration.MediumDuration;
            }

            this.InitializeComponent();
            this.midiEngine.PlaybackFinished += this.MidiEngine_PlaybackFinished;
            this.midiEngine.RenderCurrentNotesAsyncEvent += this.MidiEngine_RenderCurrentNotesEventAsync;
            this.InputControl.LaneButtonClicked += this.InputControl_LaneButtonClicked;
            this.InputControl.EyeGazeInteracted += this.InputControl_EyeGazeInteracted;
            this.InputControl.SongTitle = this.midiEngine.TrackDisplayName;
            this.InputControl.SecondsDuringWhichNoteIsActive = (int)UserData.ActiveDuration;

            this.midiEngine.Initialize();
            if (UserData.GameMode == UserData.Mode.Offline || UserData.GameMode == UserData.Mode.Online)
            {
                this.timer.Tick += this.Timer_Tick;
                this.timer.Interval = TimeSpan.FromSeconds(1);
                this.timer.Start();
            }
            else
            {
                this.InputControl.Pitches = UserData.PitchMap.Pitches;
                this.midiEngine.Start();
            }
        }

        private static int? ConvertToLane(NoteEvent currentNoteOnEvent)
        {
            var noteName = currentNoteOnEvent.GetNoteName().ToString();
            return Array.Find(UserData.PitchMap.Pitches, pitch => pitch.NoteNames.Any(
                    (pitchNoteName) => string.Equals(pitchNoteName, noteName, StringComparison.Ordinal)))?.Index;
        }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        private void MidiEngine_PlaybackFinished(object sender, EventArgs e) =>
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo()));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        private void RemoveOldNotes()
        {
            foreach (var (key, value) in this.notesOnScreen.Where(pair => (DateTime.UtcNow - pair.Value.FirstDisplayed).TotalSeconds > 20))
            {
                this.InputControl.RemoveNote(value.NoteControl);
                _ = this.notesOnScreen.Remove(key, out _);
            }
        }

        private async Task MidiEngine_RenderCurrentNotesEventAsync(ConcurrentQueue<MarkablePlaybackEvent>[] markablePlaybackEvents)
        {
            this.RemoveOldNotes();
            foreach (var markablePlaybackEvent in markablePlaybackEvents)
            {
                foreach (var currentMarkablePlaybackEvent in markablePlaybackEvent)
                {
                    if (this.notesOnScreen.ContainsKey(currentMarkablePlaybackEvent.Id))
                    {
                        continue;
                    }

                    var currentNoteOnEvent = currentMarkablePlaybackEvent.Event as NoteOnEvent;
                    NoteControl noteControlNote = null;

                    double noteLengthInPercent = 16;
                    if (currentMarkablePlaybackEvent.RelatedEvents.Count > 0)
                    {
                        var noteOff = currentMarkablePlaybackEvent.RelatedEvents.Find(note =>
                            note?.Event?.EventType == MidiEventType.NoteOff);
                        var noteOn = currentMarkablePlaybackEvent.RelatedEvents.Find(note =>
                            note?.Event?.EventType == MidiEventType.NoteOn);
                        long firstNoteRawTime = -1;
                        if (noteOff != null && noteOn != null)
                        {
                            firstNoteRawTime = noteOff.RawTime < noteOn.RawTime ? noteOff.RawTime : noteOn.RawTime;
                        }
                        else if (noteOff != null)
                        {
                            firstNoteRawTime = noteOff.RawTime;
                        }
                        else if (noteOn != null)
                        {
                            firstNoteRawTime = noteOn.RawTime;
                        }

                        if (firstNoteRawTime != -1)
                        {
                            noteLengthInPercent = 1 * (firstNoteRawTime - currentMarkablePlaybackEvent.RawTime) / 50D;
                        }
                    }

                    var lane = ConvertToLane(currentNoteOnEvent);
                    if (lane.HasValue)
                    {
                        await this.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            () => noteControlNote = this.InputControl.PlayNote(
                                lane.Value,
                                TimeSpan.FromSeconds(5),
                                noteLengthInPercent));
                        this.notesOnScreen[currentMarkablePlaybackEvent.Id] =
                            new RenderedMarkablePlaybackEvent(currentMarkablePlaybackEvent, noteControlNote);
                    }
                }
            }
        }

        private void InputControl_LaneButtonClicked(object sender, int lane)
        {
            // In the formula below, note that each note is displayed for a total of 5 seconds.
            var renderedMarkablePlaybackEvents =
                this.notesOnScreen.Values.Where(note => note.MarkablePlaybackEvent.Event is NoteOnEvent noteOnEvent
                                                        && ConvertToLane(noteOnEvent) == lane
                                                        && DateTime.UtcNow.Subtract(note.FirstDisplayed).TotalSeconds > 5 - (int)UserData.ActiveDuration);

            foreach (var renderedMarkablePlaybackEvent in renderedMarkablePlaybackEvents)
            {
                renderedMarkablePlaybackEvent.MarkablePlaybackEvent.IsMarked = true;
                renderedMarkablePlaybackEvent.NoteControl.Mark();
            }
        }

        private void InputControl_EyeGazeInteracted(object sender, StateChangedEventArgs e)
        {
            var senderNoteControl = sender as NoteControl;
            var renderedMarkablePlaybackEvent =
                this.notesOnScreen.Values.FirstOrDefault(playbackEvent => playbackEvent.NoteControl == senderNoteControl);

            // In the formula below, note that each note is displayed for a total of 5 seconds.
            if (renderedMarkablePlaybackEvent != null
                && DateTime.UtcNow.Subtract(renderedMarkablePlaybackEvent.FirstDisplayed).TotalSeconds > 5 - (int)UserData.ActiveDuration)
            {
                renderedMarkablePlaybackEvent.MarkablePlaybackEvent.IsMarked = true;
                renderedMarkablePlaybackEvent.NoteControl.Mark();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            var nowTime = DateTime.Now.TimeOfDay;
            var currentTime = new TimeSpan(nowTime.Hours, nowTime.Minutes, nowTime.Seconds);

            if (UserData.GameMode == UserData.Mode.Offline)
            {
                if (UserData.StartTime == null || UserData.StartTime < currentTime)
                {
                    this.timer.Stop();
                    this.Clockface.Text = string.Empty;
                    this.InputControl.Pitches = UserData.PitchMap.Pitches;
                    this.midiEngine.Start();
                    return;
                }

                var difference = UserData.StartTime.Value - currentTime;
                this.Clockface.Text = difference.ToString("T", CultureInfo.CurrentCulture);
            }
            else
            {
                if (MultiPlayerData.StartTime == DateTime.MinValue)
                {
                    return;
                }

                if (MultiPlayerData.StartTime > DateTime.UtcNow)
                {
                    var difference = MultiPlayerData.StartTime - currentTime;
                    this.Clockface.Text = difference.ToString(@"mm\:ss", CultureInfo.CurrentCulture);
                }
                else
                {
                    this.timer.Stop();
                    this.Clockface.Text = string.Empty;
                    this.InputControl.Pitches = UserData.PitchMap.Pitches;
                    this.midiEngine.Start();
                }
            }
        }

        /// <summary>
        /// Event handler when clicking Pause button.
        /// For Solo mode, the playback will be paused.
        /// For Offline mode or LAN mode, the playback will continue playing because other players will not be affected while they are playing.
        /// </summary>
        /// <param name="sender">A sender of this event</param>
        /// <param name="e">Contains state information and event data associated with a routed event</param>
        private async void Pause_Click(object sender, RoutedEventArgs e)
        {
            if (UserData.GameMode == UserData.Mode.Solo)
            {
                midiEngine.Pause();
            }

            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

            ContentDialog dialog = new ContentDialog
            {
                Title = resourceLoader.GetString("GamePage/PauseDialog/Title"),
                Content = resourceLoader.GetString("GamePage/PauseDialog/Content"),
                PrimaryButtonText = resourceLoader.GetString("GamePage/PauseDialog/PrimaryButton"),
                CloseButtonText = resourceLoader.GetString("GamePage/PauseDialog/CloseButton"),
                DefaultButton = ContentDialogButton.Primary
            };

            var result = await dialog.ShowAsync();
            
            if (result != ContentDialogResult.Primary)
            {
                midiEngine.Dispose();
                this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                if (UserData.GameMode == UserData.Mode.Solo)
                {
                    midiEngine.Resume();
                }
            }
        }
    }
}
