// Licensed under the MIT License.

namespace Coimbra.Midi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Coimbra.DryWetMidiIntegration;
    using Coimbra.Midi.Models;
    using Coimbra.OSIntegration;
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Devices;
    using Melanchall.DryWetMidi.Interaction;
    using Windows.Storage;

    /// <summary>
    /// The MIDI engine, controlling playback of the MIDI files.
    /// </summary>
    public class MidiEngine : IDisposable
    {
        /// <summary>
        /// The singleton instance of the MIDI engine.
        /// </summary>
        public static readonly MidiEngine Instance = new MidiEngine();

        private readonly MediaControls media = MediaControls.Current;

        private readonly ConcurrentQueue<MarkablePlaybackEvent>[] notesOnDisplay =
            new ConcurrentQueue<MarkablePlaybackEvent>[127];

        private bool isStopped;

        private OutputDevice outputDevice;

        private MarkablePlayback playback;

        private Thread startThread;

        private CancellationTokenSource startThreadCancellationToken;

        private Timer startTimer;

        private Thread thread;

        private CancellationTokenSource threadCancellationToken;

        private bool disposed;

        private MidiFile midi;

        private MidiEngine()
        {
            this.media.PlayPressed += this.Media_PlayPressed;
            this.media.PausePressed += this.Media_PausePressed;
        }

        /// <summary>
        /// The output device sent delegate.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public delegate void OutputDeviceEventSent(MidiEventSentEventArgs e);

        /// <summary>
        /// The playback selected track notification delegate.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        public delegate void PlaybackSelectedTrackNotification(TrackNotificationEventArgs e);

        /// <summary>
        /// The render current notes delegate.
        /// </summary>
        /// <param name="queue">The queue of markable playback events.</param>
        /// <param name="currentTime">The current time of playback.</param>
        /// <returns>An asynchronous task.</returns>
        public delegate Task RenderCurrentNotesAsync(ConcurrentQueue<MarkablePlaybackEvent>[] queue, TimeSpan currentTime);

        /// <summary>
        /// The output device sent event.
        /// </summary>
        public event OutputDeviceEventSent OutputDeviceEventSentEvent;

        /// <summary>
        /// The playback selected track notification event.
        /// </summary>
        public event PlaybackSelectedTrackNotification PlaybackSelectedTrackNotificationEvent;

        /// <summary>
        /// The render current notes event.
        /// </summary>
        public event RenderCurrentNotesAsync RenderCurrentNotesAsyncEvent;

        /// <summary>
        /// The playback finished.
        /// </summary>
        public event EventHandler<EventArgs> PlaybackFinished;

        /// <summary>
        /// Gets or sets the MIDI file.
        /// </summary>
        public StorageFile File { get; set; }

        /// <summary>
        /// Gets or sets the display name of first track.
        /// </summary>
        public string TrackDisplayName { get; set; }

        /// <summary>
        /// Gets the selected track/channel.
        /// </summary>
        public FourBitNumber SelectedTrack => this.playback.SelectedChannel;

        /// <summary>
        /// Gets the set of instruments associated with the MIDI file.
        /// </summary>
        public IDictionary<FourBitNumber, InstrumentInfo> Instruments { get; } =
            new Dictionary<FourBitNumber, InstrumentInfo>();

        /// <summary>
        /// Gets the set of pitches associated with the selected instrument.
        /// </summary>
        /// <param name="instrument">Instrument to retrieve pitches for.</param>
        /// <returns>Pitches for instrument.</returns>
        public List<string> RetrievePitchesForInstrument(FourBitNumber instrument) =>
            this.midi.GetNotes()
                .Where(note => note.Channel == instrument)
                .Select(note => note.NoteName.ToString())
                .Distinct()
                .ToList();

        /// <summary>
        /// Gets the play times of notes associated with the selected instrument.
        /// </summary>
        /// <param name="instrument">Instrument to retrieve note times for.</param>
        /// <returns>Note times for instrument.</returns>
        public List<long> RetrieveNoteTimesForInstrument(FourBitNumber instrument)
        {
            var midiMap = this.midi.GetTempoMap();
            return this.midi.GetNotes()
                .Where(note => note.Channel == instrument)
                .Select(note => ((MetricTimeSpan)note.TimeAs(TimeSpanType.Metric, midiMap)).TotalMicroseconds / 1000)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Gets how many notes are there for each instrument.
        /// </summary>
        /// <returns>Note counts of each instrument.</returns>
        public Dictionary<FourBitNumber, int> RetrieveNoteCountsOfInstruments() =>
            this.midi.GetNotes()
            .GroupBy(note => note.Channel)
            .Select(group => new
            {
                Channel = group.Key,
                Count = group.Count()
            })
            .ToDictionary(channelAndNoteCount => channelAndNoteCount.Channel, channelAndNoteCount => channelAndNoteCount.Count);

        /// <summary>
        /// Called when parsing a file.
        /// </summary>
        /// <returns>The asynchronous task.</returns>
        public async Task OnParseFileAsync()
        {
            using (var stream = await this.File.OpenStreamForReadAsync().ConfigureAwait(true))
            {
                this.midi = MidiFile.Read(stream, new ReadingSettings { NotEnoughBytesPolicy = NotEnoughBytesPolicy.Ignore });
            }

            this.TrackDisplayName = GetTrackDisplayName(this.File, this.midi);
            this.AddInstruments(this.midi);

            this.outputDevice = OutputDevice.GetByName("Microsoft GS Wavetable Synth");
            this.outputDevice.EventSent += this.OutputDevice_EventSent;
            this.playback = new MarkablePlayback(this.midi.GetTimedEvents(), this.midi.GetTempoMap(), this.outputDevice);

            this.playback.Finished += this.Playback_Finished;
            this.playback.NotesPlaybackStarted += Playback_NotesPlaybackStarted;
            this.playback.TrackNotes = true;
            this.playback.SelectedTrackNotification += this.Playback_SelectedTrackNotification;
            this.playback.NotesDisplayStarted += this.Playback_NotesDisplayStarted;
            this.playback.NotesDisplayFinished += this.Playback_NotesDisplayFinished;
        }

        /// <summary>
        /// Starts the playback of the MIDI file.
        /// </summary>
        public void Start()
        {
            this.startThreadCancellationToken = new CancellationTokenSource();
            this.startThread = new Thread(() => this.playback.Start());

            this.startTimer = new Timer(_ => this.startThread.Start(), null, TimeSpan.Zero, TimeSpan.Zero);
            this.isStopped = false;
        }

        /// <summary>
        /// Pause the playback
        /// </summary>
        public void Pause()
        {
            this.playback.Stop();
        }

        /// <summary>
        /// Resume the playback
        /// </summary>
        public void Resume()
        {
            this.playback.Start();
        }

        /// <summary>
        /// Selects a specific track.
        /// </summary>
        /// <param name="channel">The channel corresponding to the track.</param>
        public void SelectTrack(int channel)
        {
            var selectedTrack = Convert.ToByte(channel);
            this.playback.SelectedChannel = new FourBitNumber(selectedTrack);
            this.playback.ProcessInstrumentNotes();
        }

        /// <summary>
        /// Initializes the device watcher and populates MIDI message types.
        /// </summary>
        public void Initialize()
        {
            for (var i = 0; i < this.notesOnDisplay.Length; i++)
            {
                this.notesOnDisplay[i] = new ConcurrentQueue<MarkablePlaybackEvent>();
            }

            this.threadCancellationToken = new CancellationTokenSource();
            this.thread = new Thread(this.RenderCurrentNotes);
            this.thread.Start();
            this.disposed = false;
        }

        /// <summary>
        /// Disposes of the resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether the managed resources should be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.playback?.Dispose();
                this.outputDevice?.Dispose();
                this.startTimer?.Dispose();
                this.startThreadCancellationToken.Cancel();
                this.threadCancellationToken.Cancel();
                this.media?.Dispose();
            }

            this.disposed = true;
        }

        private static void Playback_NotesPlaybackStarted(object sender, MarkableNotesEventArgs e)
        {
            foreach (var note in e.Notes)
            {
                note.Length = 0;
            }
        }

        private static string GetTrackDisplayName(StorageFile file, MidiFile midi)
        {
            var events = midi.GetTrackChunks().ElementAt(0).Events;
            var titleEvent = events.FirstOrDefault(currentEvent => currentEvent.EventType == MidiEventType.SequenceTrackName);
            if (titleEvent != null)
            {
                return ((SequenceTrackNameEvent)titleEvent).Text;
            }

            return file.DisplayName;
        }

        private void AddInstruments(MidiFile midi)
        {
            this.Instruments.Clear();
            var noteCountsOfInstrument = this.RetrieveNoteCountsOfInstruments();

            var timedEvents = midi.GetTimedEvents();
            var events = timedEvents.Select(e => e.Event).OfType<ProgramChangeEvent>().ToList<ChannelEvent>();

            if (!events.Any())
            {
                events = timedEvents
                    .Select(e => e.Event)
                    .OfType<NoteOnEvent>()
                    .ToList<ChannelEvent>();
            }

            foreach (var currentEvent in events)
            {
                var programNumber = new SevenBitNumber(1);

                if (currentEvent is ProgramChangeEvent currentParsedEvent)
                {
                    programNumber = currentParsedEvent.ProgramNumber;
                }

                if (this.Instruments.ContainsKey(currentEvent.Channel))
                {
                    if (!this.Instruments[currentEvent.Channel].ProgramNumbers.Contains(programNumber))
                    {
                        this.Instruments[currentEvent.Channel].ProgramNumbers.Add(programNumber);
                    }
                }
                else
                {
                    this.Instruments[currentEvent.Channel] =
                        new InstrumentInfo(currentEvent.Channel, new List<SevenBitNumber> { programNumber },
                            noteCountsOfInstrument.ContainsKey(currentEvent.Channel) ? noteCountsOfInstrument[currentEvent.Channel] : 0);
                }
            }
        }

        /// <summary>
        /// Invoked whenever the playback is stopped.
        /// </summary>
        private void OnStop()
        {
            if (this.isStopped)
            {
                this.playback?.Start();
            }
            else
            {
                this.playback?.Stop();
            }

            this.isStopped = !this.isStopped;
        }

        private void Media_PausePressed(object sender, EventArgs e) =>
            this.OnStop();

        private void Media_PlayPressed(object sender, EventArgs e)
        {
            if (this.isStopped)
            {
                this.OnStop();
            }
        }

        private void Playback_NotesDisplayFinished(object sender, MarkablePlaybackEventArgs e)
        {
            var offTest = ((NoteOffEvent)e.PlaybackEvent.Event).NoteNumber;
            this.notesOnDisplay[offTest] = new ConcurrentQueue<MarkablePlaybackEvent>();
        }

        private void Playback_NotesDisplayStarted(object sender, MarkablePlaybackEventArgs e) =>
            this.notesOnDisplay[((NoteOnEvent)e.PlaybackEvent.Event).NoteNumber].Enqueue(e.PlaybackEvent);

        private void RenderCurrentNotes()
        {
            while (!this.isStopped)
            {
                var task = this.RenderCurrentNotesAsyncEvent?.Invoke(this.notesOnDisplay, playback.CurrentTime);
                task?.ConfigureAwait(true).GetAwaiter().GetResult();
                Thread.Sleep(TimeSpan.FromMilliseconds(200));
            }
        }

        private void Playback_SelectedTrackNotification(object sender, TrackNotificationEventArgs e) =>
            this.PlaybackSelectedTrackNotificationEvent?.Invoke(e);

        private void Playback_Finished(object sender, EventArgs e)
        {
            // The finished event fires 5 seconds before the last note finished playing,
            // due to difference between display and audio, leaving one second of buffer.
            Task.Delay(6000).ContinueWith(t =>
            {
                this.startTimer?.Dispose();
                this.threadCancellationToken.Cancel();
                this.startThreadCancellationToken.Cancel();
                this.playback?.Dispose();
                this.outputDevice?.Dispose();
                this.isStopped = true;

                PlaybackFinished?.Invoke(this, null);
            });
        }

        private void OutputDevice_EventSent(object sender, MidiEventSentEventArgs e) =>
            this.OutputDeviceEventSentEvent?.Invoke(e);
    }
}
