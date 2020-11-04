// Licensed under the MIT License.

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
        private readonly TimeSpan noteDuration = TimeSpan.FromSeconds(5);

        private List<long> noteTimes;
        private int lastPlayedNoteTimeIndex = 0;
        private int dotCounter = 0;
        private long previousTimeToNote = 0;

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
            noteTimes = this.midiEngine.RetrieveNoteTimesForInstrument(this.midiEngine.SelectedTrack);

            this.lastPlayedNoteTimeIndex = 0;
            this.dotCounter = 0;
            this.previousTimeToNote = 0;

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


            // Reset the flag
            UserData.IsOptionChangeMode = false;
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
                this.Frame.Navigate(typeof(OptionsPage), null, new DrillInNavigationTransitionInfo()));
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        private void RemoveOldNotes()
        {
            foreach (var (key, value) in this.notesOnScreen.Where(pair => (DateTime.UtcNow - pair.Value.FirstDisplayed).TotalSeconds > 20))
            {
                this.InputControl.RemoveNote(value.NoteControl);
                _ = this.notesOnScreen.Remove(key, out _);
            }
        }

        private void RenderTimeToNextNote(TimeSpan currentTime)
        {
            MarkablePlaybackEvent lastNoteOnScreen = null;
            if (this.notesOnScreen.Values.Count > 0)
            {
                lastNoteOnScreen = this.notesOnScreen.Values.Aggregate((a, b) => a.MarkablePlaybackEvent.RawTime > b.MarkablePlaybackEvent.RawTime ? a : b).MarkablePlaybackEvent;
            }

            TimeSpan lastNoteOffScreenTime = default(TimeSpan);

            if (lastNoteOnScreen != null && lastNoteOnScreen.Time.TotalMilliseconds > 0)
            {
                lastNoteOffScreenTime = lastNoteOnScreen.Time + noteDuration;
            }

            long nextNoteTime = GetNextNoteTime((long)currentTime.TotalMilliseconds);
            if (nextNoteTime < 0)
            {
                return;
            }

            long timeToNextNote = nextNoteTime - (long)currentTime.TotalMilliseconds;

            long timeToNextNoteAfterLastNote = timeToNextNote -
                // Substract note duration because it will show up earlier
                (long)noteDuration.TotalMilliseconds +
                // Substract currentTime to get the difference
                (long)currentTime.TotalMilliseconds;

            // No point of showing this if the next note is less than 3 second away
            if ((lastNoteOffScreenTime.TotalMilliseconds == 0 || currentTime.TotalMilliseconds - lastNoteOffScreenTime.TotalMilliseconds > 1000) &&
                (timeToNextNoteAfterLastNote > 3000 || previousTimeToNote >= 1000) &&
                timeToNextNote >= 1000 &&
                (lastPlayedNoteTimeIndex < this.noteTimes.Count - 1 ||
                (lastPlayedNoteTimeIndex == this.noteTimes.Count - 1 && nextNoteTime > currentTime.TotalMilliseconds)))
            {
                previousTimeToNote = timeToNextNote;
                this.dotCounter = 0;
                var seconds = timeToNextNote / 1000;
                this.InputControl.SetTimeToNextNote($"Next note in: {seconds} seconds");
            }
            else
            {
                this.InputControl.SetTimeToNextNote($"Playing{new string('.', (this.dotCounter / 5) + 1)}");
                this.dotCounter++;
                if (this.dotCounter == 15)
                {
                    this.dotCounter = 0;
                }
            }
        }

        private long GetNextNoteTime(long currentTime)
        {
            if (this.noteTimes != null)
            {
                for (int i = this.lastPlayedNoteTimeIndex; i < this.noteTimes.Count; i++)
                {
                    // add noteDuration to get the playing note
                    if (this.noteTimes[i] + noteDuration.TotalMilliseconds > currentTime)
                    {
                        this.lastPlayedNoteTimeIndex = i;
                        return this.noteTimes[i];
                    }
                }
            }

            return -1;
        }

        private long GetNoteLengthInRawTime(MarkablePlaybackEvent markablePlaybackEvent)
        {
            if (markablePlaybackEvent != null && markablePlaybackEvent.RelatedEvents.Count > 0)
            {
                var noteOff = markablePlaybackEvent.RelatedEvents.Find(note =>
                    note?.Event?.EventType == MidiEventType.NoteOff);
                var noteOn = markablePlaybackEvent.RelatedEvents.Find(note =>
                    note?.Event?.EventType == MidiEventType.NoteOn);
                long noteRawTime = -1;
                if (noteOff != null && noteOn != null)
                {
                    noteRawTime = noteOff.RawTime < noteOn.RawTime ? noteOff.RawTime : noteOn.RawTime;
                }
                else if (noteOff != null)
                {
                    noteRawTime = noteOff.RawTime;
                }
                else if (noteOn != null)
                {
                    noteRawTime = noteOn.RawTime;
                }

                return noteRawTime - markablePlaybackEvent.RawTime;
            }

            return -1;
        }

        private async Task MidiEngine_RenderCurrentNotesEventAsync(ConcurrentQueue<MarkablePlaybackEvent>[] markablePlaybackEvents, TimeSpan currentTime)
        {
            this.RemoveOldNotes();
            this.RenderTimeToNextNote(currentTime);

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
                    long firstNoteLength = this.GetNoteLengthInRawTime(currentMarkablePlaybackEvent);
                    if (firstNoteLength != -1)
                    {
                        noteLengthInPercent = 1 * firstNoteLength / 50D;
                    }

                    var lane = ConvertToLane(currentNoteOnEvent);
                    if (lane.HasValue)
                    {
                        await this.Dispatcher.RunAsync(
                            CoreDispatcherPriority.Normal,
                            () => noteControlNote = this.InputControl.PlayNote(
                                lane.Value,
                                noteDuration,
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
    }
}
