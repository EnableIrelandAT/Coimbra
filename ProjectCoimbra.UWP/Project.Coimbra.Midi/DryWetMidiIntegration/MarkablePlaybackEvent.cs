// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;
    using System.Collections.Generic;
    using Melanchall.DryWetMidi.Core;

    /// <summary>
    /// A class representing a event related to a markable playback.
    /// </summary>
    public class MarkablePlaybackEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkablePlaybackEvent"/> class.
        /// </summary>
        /// <param name="midiEvent">The MIDI event.</param>
        /// <param name="time">The time of the event.</param>
        /// <param name="rawTime">The raw epoch time of the event.</param>
        public MarkablePlaybackEvent(MidiEvent midiEvent, TimeSpan time, long rawTime)
        {
            this.Id = Guid.NewGuid();
            this.IsMarked = false;
            this.Event = midiEvent;
            this.Time = time;
            this.RawTime = rawTime;
        }

        /// <summary>
        /// Gets the ID of the event.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the event is marked.
        /// </summary>
        public bool IsMarked { get; set; }

        /// <summary>
        /// Gets the MIDI event.
        /// </summary>
        public MidiEvent Event { get; }

        /// <summary>
        /// Gets the time of the event.
        /// </summary>
        public TimeSpan Time { get; }

        /// <summary>
        /// Gets the raw epoch time of the event.
        /// </summary>
        public long RawTime { get; }

        /// <summary>
        /// Gets or sets the event metadata.
        /// </summary>
        public NotePlaybackEventMetadata Metadata { get; set; }

        /// <summary>
        /// Gets a list of related events.
        /// </summary>
        public List<MarkablePlaybackEvent> RelatedEvents { get; } = new List<MarkablePlaybackEvent>();
    }
}
