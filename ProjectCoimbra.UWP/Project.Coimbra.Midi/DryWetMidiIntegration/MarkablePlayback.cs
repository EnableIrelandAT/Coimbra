// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Coimbra.Model;
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Devices;
    using Melanchall.DryWetMidi.Interaction;

    /// <summary>
    /// A class providing a way to play MIDI data through the specified output MIDI device.
    /// </summary>
    public class MarkablePlayback : IDisposable
    {
        private static readonly TimeSpan ClockInterval = TimeSpan.FromMilliseconds(1);

        private readonly IEnumerator<MarkablePlaybackEvent> eventsEnumerator;

        private readonly MidiClock clock;

        private readonly Dictionary<NoteId, Note> activeNotes = new Dictionary<NoteId, Note>();

        private readonly List<NotePlaybackEventMetadata> notesMetadata;

        private readonly SortedSet<FourBitNumber> deactivatedChannels = new SortedSet<FourBitNumber>();

        private readonly MarkablePlaybackEvent[] midiPlaybackEvents;

        private readonly RegularPrecisionTickGenerator regularPrecisionTickGenerator;

        private IEnumerable<MarkablePlaybackEvent> selectedNotes;

        private TimeSpan lastTick;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkablePlayback"/> class.
        /// </summary>
        /// <param name="timedObjects">The collection of timed objects to play.</param>
        /// <param name="tempoMap">The tempo map used to calculate events times.</param>
        /// <param name="outputDevice">The output MIDI device to play <paramref name="timedObjects"/> through.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="timedObjects" /> or <paramref name="tempoMap" /> is null.</exception>
        public MarkablePlayback(IEnumerable<ITimedObject> timedObjects, TempoMap tempoMap, OutputDevice outputDevice)
        {
            if (timedObjects == null)
            {
                throw new ArgumentNullException(nameof(timedObjects));
            }

            if (tempoMap == null)
            {
                throw new ArgumentNullException(nameof(tempoMap));
            }

            var playbackEvents = GetPlaybackEvents(timedObjects, tempoMap);
            this.OutputDevice = outputDevice;

            this.eventsEnumerator = playbackEvents.GetEnumerator();
            _ = this.eventsEnumerator.MoveNext();
            this.midiPlaybackEvents = playbackEvents.ToArray();

            this.notesMetadata = playbackEvents.Select(e => e.Metadata).Where(m => m != null).ToList();
            this.notesMetadata.Sort((m1, m2) => m1.StartTime.CompareTo(m2.StartTime));

            this.TempoMap = tempoMap;

            this.regularPrecisionTickGenerator = new RegularPrecisionTickGenerator(ClockInterval);
            this.clock = new MidiClock(true, this.regularPrecisionTickGenerator);
            this.clock.Ticked += this.OnClockTick;

            this.ProcessInstrumentNotes();
        }

        /// <summary>
        /// The event handler for add events.
        /// </summary>
        public event EventHandler<TrackNotificationEventArgs> SelectedTrackNotification;

        /// <summary>
        /// The event handler for when playback is started via the <see cref="Start"/> or <see cref="Play"/> methods.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// The event handler for when playback is stopped via <see cref="Stop"/> method.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// The event handler for when playback finishes.
        /// </summary>
        public event EventHandler Finished;

        /// <summary>
        /// The event handler for when notes start to play.
        /// </summary>
        public event EventHandler<MarkableNotesEventArgs> NotesPlaybackStarted;

        /// <summary>
        /// The event handler for when notes finish playing.
        /// </summary>
        public event EventHandler<MarkableNotesEventArgs> NotesPlaybackFinished;

        /// <summary>
        /// The event handler for when notes should be displayed.
        /// </summary>
        public event EventHandler<MarkablePlaybackEventArgs> NotesDisplayStarted;

        /// <summary>
        /// The event handler for when notes should be hidden from display.
        /// </summary>
        public event EventHandler<MarkablePlaybackEventArgs> NotesDisplayFinished;

        /// <summary>
        /// Gets or sets the select channel.
        /// </summary>
        public FourBitNumber SelectedChannel { get; set; } = new FourBitNumber(0);

        /// <summary>
        /// Gets the tempo map used to calculate events times.
        /// </summary>
        public TempoMap TempoMap { get; }

        /// <summary>
        /// Gets or sets the output MIDI device to play MIDI data through.
        /// </summary>
        public OutputDevice OutputDevice { get; set; }

        /// <summary>
        /// Gets a value indicating whether playing is currently running or not.
        /// </summary>
        public bool IsRunning => this.clock.IsRunning;

        /// <summary>
        /// Gets or sets a value indicating whether currently playing notes must be stopped on playback stop or not.
        /// </summary>
        public bool InterruptNotesOnStop { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether notes must be tracked or not. If false, notes will be treated as
        /// just Note On/Note Off events.
        /// </summary>
        public bool TrackNotes { get; set; }

        /// <summary>
        /// Processes the instrument notes.
        /// </summary>
        public void ProcessInstrumentNotes()
        {
            this.selectedNotes = this.midiPlaybackEvents.Where(
                playbackEvent =>
                    playbackEvent.Event is ChannelEvent playbackChannelEvent
                    && playbackChannelEvent.Channel == this.SelectedChannel).ToArray();

            // Finds the matching NoteOffEvents.
            foreach (var selectedNote in this.selectedNotes)
            {
                if (selectedNote.Event.EventType == MidiEventType.NoteOn)
                {
                    selectedNote.RelatedEvents.Add(this.selectedNotes.FirstOrDefault(note =>
                        note.RawTime > selectedNote.RawTime
                        && ((note.Event.EventType == MidiEventType.NoteOff
                             && ((NoteOffEvent)note.Event).NoteNumber == ((NoteOnEvent)selectedNote.Event).NoteNumber)
                            || (note.Event.EventType == MidiEventType.NoteOn
                                && ((NoteOnEvent)note.Event).NoteNumber
                                == ((NoteOnEvent)selectedNote.Event).NoteNumber))));
                }
            }
        }

        /// <summary>
        /// Starts playing the MIDI data.
        /// </summary>
        public void Start()
        {
            this.EnsureIsNotDisposed();
            if (this.clock.IsRunning)
            {
                return;
            }

            this.OutputDevice?.PrepareForEventsSending();
            this.StopStartNotes();
            this.clock.Start();

            this.OnStarted();
        }

        /// <summary>
        /// Stops playing the MIDI data.
        /// </summary>
        public void Stop()
        {
            this.EnsureIsNotDisposed();
            if (!this.IsRunning)
            {
                return;
            }

            this.clock.Stop();
            if (this.InterruptNotesOnStop)
            {
                foreach (var (key, value) in this.activeNotes)
                {
                    this.SendEvent(new NoteOffEvent(key.NoteNumber, value.OffVelocity) { Channel = key.Channel, });
                }

                this.activeNotes.Clear();
            }

            this.OnStopped();
        }

        /// <summary>
        /// Starts playing the MIDI data.
        /// </summary>
        public void Play()
        {
            this.EnsureIsNotDisposed();
            this.Start();
            SpinWait.SpinUntil(() => !this.clock.IsRunning);
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
                this.Stop();

                this.clock.Ticked -= this.OnClockTick;
                this.clock.Dispose();
                this.eventsEnumerator.Dispose();
                this.regularPrecisionTickGenerator.Dispose();
            }

            this.disposed = true;
        }

        private static ICollection<MarkablePlaybackEvent> GetPlaybackEvents(
            IEnumerable<ITimedObject> timedObjects,
            TempoMap tempoMap)
        {
            var playbackEvents = new List<MarkablePlaybackEvent>();
            foreach (var timedObject in timedObjects)
            {
                switch (timedObject)
                {
                    case Chord chord:
                        playbackEvents.AddRange(GetPlaybackEvents(chord, tempoMap));
                        continue;
                    case Note note:
                        playbackEvents.AddRange(GetPlaybackEvents(note, tempoMap));
                        continue;
                    case TimedEvent timedEvent:
                        playbackEvents.Add(new MarkablePlaybackEvent(
                            timedEvent.Event,
                            timedEvent.TimeAs<MetricTimeSpan>(tempoMap),
                            timedEvent.Time));
                        break;
                }
            }

            playbackEvents.Sort(new MarkablePlaybackEventComparer());
            return playbackEvents;
        }

        private static IEnumerable<MarkablePlaybackEvent> GetPlaybackEvents(Chord chord, TempoMap tempoMap) =>
            chord.Notes.SelectMany(note => GetPlaybackEvents(note, tempoMap));

        private static IEnumerable<MarkablePlaybackEvent> GetPlaybackEvents(Note note, TempoMap tempoMap)
        {
            yield return GetPlaybackEventWithNoteTag(note, note.GetTimedNoteOnEvent(), tempoMap);
            yield return GetPlaybackEventWithNoteTag(note, note.GetTimedNoteOffEvent(), tempoMap);
        }

        private static MarkablePlaybackEvent GetPlaybackEventWithNoteTag(
            Note note,
            TimedEvent timedEvent,
            TempoMap tempoMap)
        {
            var playbackEvent = new MarkablePlaybackEvent(
                timedEvent.Event,
                timedEvent.TimeAs<MetricTimeSpan>(tempoMap),
                timedEvent.Time);

            TimeSpan noteStartTime = note.TimeAs<MetricTimeSpan>(tempoMap);
            TimeSpan noteEndTime = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time + note.Length, tempoMap);
            playbackEvent.Metadata = new NotePlaybackEventMetadata(note, noteStartTime, noteEndTime);

            return playbackEvent;
        }

        private void StopStartNotes()
        {
            if (!this.TrackNotes)
            {
                return;
            }

            var currentTime = this.clock.CurrentTime;
            var notesToPlay = this.notesMetadata.SkipWhile(m => m.EndTime <= currentTime)
                .TakeWhile(m => m.StartTime < currentTime)
                .Where(m => m.StartTime < currentTime && m.EndTime > currentTime)
                .Select(m => m.RawNote)
                .Distinct()
                .ToArray();

            var notesIds = notesToPlay.Select(n => n.GetNoteId()).ToArray();
            var onNotes = notesToPlay.Where(n => !this.activeNotes.ContainsValue(n)).ToArray();
            var offNotes = this.activeNotes.Where(n => !notesIds.Contains(n.Key)).Select(n => n.Value).ToArray();

            this.OutputDevice?.PrepareForEventsSending();
            foreach (var note in offNotes)
            {
                this.SendEvent(note.GetTimedNoteOffEvent().Event);
            }

            this.OnNotesPlaybackFinished(offNotes);
            foreach (var note in onNotes)
            {
                this.SendEvent(note.GetTimedNoteOnEvent().Event);
            }

            this.OnNotesPlaybackStarted(onNotes);
        }

        private void OnStarted() =>
            this.Started?.Invoke(this, EventArgs.Empty);

        private void OnStopped() =>
            this.Stopped?.Invoke(this, EventArgs.Empty);

        private void OnFinished() =>
            this.Finished?.Invoke(this, EventArgs.Empty);

        private void OnNotesPlaybackStarted(params Note[] notes) =>
            this.NotesPlaybackStarted?.Invoke(this, new MarkableNotesEventArgs(notes));

        private void OnNotesPlaybackFinished(params Note[] notes) =>
            this.NotesPlaybackFinished?.Invoke(this, new MarkableNotesEventArgs(notes));

        private void OnNotesDisplayStarted(MarkablePlaybackEvent playbackEvent) =>
            this.NotesDisplayStarted?.Invoke(this, new MarkablePlaybackEventArgs(playbackEvent));

        private void OnNotesDisplayFinished(MarkablePlaybackEvent playbackEvent) =>
            this.NotesDisplayFinished?.Invoke(this, new MarkablePlaybackEventArgs(playbackEvent));

        private void OnClockTick(object sender, object eventArgs)
        {
            var currentTime = this.clock.CurrentTime;
            foreach (var playbackEvent in this.midiPlaybackEvents)
            {
                if (!this.IsRunning)
                {
                    return;
                }

                if (playbackEvent == null)
                {
                    continue;
                }

                if (playbackEvent.Time > currentTime.Add(new TimeSpan(0, 0, 0, 5, 2)))
                {
                    return;
                }

                var midiEvent = playbackEvent.Event;
                if (midiEvent is ChannelEvent channelEvent && this.deactivatedChannels.Contains(channelEvent.Channel))
                {
                    continue;
                }

                this.CheckDisplay(currentTime, playbackEvent);
                this.CheckPlaySound(currentTime, playbackEvent);
            }

            this.clock.Stop();
            this.OnFinished();
        }

        private void CheckDisplay(TimeSpan currentTime, MarkablePlaybackEvent playbackEvent)
        {
            if (playbackEvent.Time < this.lastTick)
            {
                return;
            }

            if (playbackEvent.Time > currentTime)
            {
                this.lastTick = currentTime;
                return;
            }

            this.SendDisplayEvents(playbackEvent, playbackEvent.Event);
        }

        private void CheckPlaySound(TimeSpan currentTime, MarkablePlaybackEvent playbackEvent)
        {
            if (playbackEvent.Time < this.lastTick.Subtract(new TimeSpan(0, 0, 0, 5))
                || playbackEvent.Time > currentTime.Subtract(new TimeSpan(0, 0, 0, 5)))
            {
                return;
            }

            this.SendPlaySoundEvents(playbackEvent, playbackEvent.Event);
        }

        private void SendPlaySoundEvents(MarkablePlaybackEvent playbackEvent, MidiEvent midiEvent)
        {
            // deal with notes our user is supposed to play
            MarkablePlaybackEvent currentNote = null;

            var inSelectedNotes = false;
            foreach (var selectedNote in this.selectedNotes)
            {
                // This playback event describes a note our user is supposed to play.
                if (selectedNote == playbackEvent)
                {
                    inSelectedNotes = true;
                }

                // This playback event describes a note our user was supposed to play but didn't.
                if (selectedNote == playbackEvent
                    && ((selectedNote.IsMarked && selectedNote.Event.EventType == MidiEventType.NoteOn)
                    || (!selectedNote.IsMarked && selectedNote.Event.EventType != MidiEventType.NoteOn)
                    || selectedNote.Event.EventType == MidiEventType.NoteOff))
                {
                    currentNote = selectedNote;
                }
            }

            if (inSelectedNotes)
            {
                 // user was supposed to play this
                if (currentNote != null)
                {
                    // The user actually played this.
                    this.SendEvent(midiEvent);
                    this.SelectedTrackNotification?.Invoke(
                        this,
                        new TrackNotificationEventArgs(true, midiEvent.ToString()));
                }
                else
                {
                    // The user did not mark the note.
                    this.SelectedTrackNotification?.Invoke(
                        this,
                        new TrackNotificationEventArgs(false, midiEvent.ToString()));
                }
            }
            else if ((UserData.GameMode != UserData.Mode.Solo && UserData.IsMultiplayerConductor) || UserData.GameMode == UserData.Mode.Solo || UserData.GameMode == UserData.Mode.Offline)
            {
                // play notes for instruments that are not played by current user
                 this.SendEvent(midiEvent);
            }

            // Following logic deals with pausing when a noteOn event has been sent but the note off event has not yet
            // been sent, so that we can restart those notes when we unpause.
            var noteMetadata = playbackEvent.Metadata;
            if (noteMetadata == null)
            {
                return;
            }

            if (midiEvent.EventType == MidiEventType.NoteOn)
            {
                this.activeNotes[noteMetadata.RawNote.GetNoteId()] = noteMetadata.RawNote;
                this.OnNotesPlaybackStarted(noteMetadata.RawNote);
            }
            else
            {
                _ = this.activeNotes.Remove(noteMetadata.RawNote.GetNoteId());
                this.OnNotesPlaybackFinished(noteMetadata.RawNote);
            }
        }

        private void SendDisplayEvents(MarkablePlaybackEvent playbackEvent, MidiEvent midiEvent)
        {
            if (midiEvent.EventType != MidiEventType.NoteOn && midiEvent.EventType != MidiEventType.NoteOff)
            {
                return;
            }

            MarkablePlaybackEvent markablePlaybackEvent = null;
            foreach (var selectedNote in this.selectedNotes)
            {
                if (selectedNote == playbackEvent)
                {
                    markablePlaybackEvent = selectedNote;
                }
            }

            if (markablePlaybackEvent == null)
            {
                return;
            }

            switch (midiEvent.EventType)
            {
                // to display
                case MidiEventType.NoteOn when ((NoteOnEvent)midiEvent).Channel == this.SelectedChannel:
                    this.OnNotesDisplayStarted(markablePlaybackEvent);
                    break;
                case MidiEventType.NoteOff when ((NoteOffEvent)midiEvent).Channel == this.SelectedChannel:
                    this.OnNotesDisplayFinished(markablePlaybackEvent);
                    break;
            }
        }

        private void EnsureIsNotDisposed()
        {
            if (!this.disposed)
            {
                return;
            }

            throw new ObjectDisposedException("Playback is disposed.");
        }

        private void SendEvent(MidiEvent midiEvent) => this.OutputDevice?.SendEvent(midiEvent);
    }
}
