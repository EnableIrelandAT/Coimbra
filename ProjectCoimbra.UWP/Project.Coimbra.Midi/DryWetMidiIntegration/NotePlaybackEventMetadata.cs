// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;
    using Melanchall.DryWetMidi.Devices;
    using Melanchall.DryWetMidi.Interaction;

    /// <summary>
    /// A class representing event metadata for note playback events.
    /// </summary>
    public sealed class NotePlaybackEventMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotePlaybackEventMetadata"/> class.
        /// </summary>
        /// <param name="note">The note.</param>
        /// <param name="startTime">The start time.</param>
        /// <param name="endTime">The end time.</param>
        public NotePlaybackEventMetadata(Note note, TimeSpan startTime, TimeSpan endTime)
        {
            this.RawNote = note ?? throw new ArgumentNullException(nameof(note));
            this.StartTime = startTime;
            this.EndTime = endTime;

            this.RawNotePlaybackData = new NotePlaybackData(
                this.RawNote.NoteNumber,
                this.RawNote.Velocity,
                this.RawNote.OffVelocity,
                this.RawNote.Channel);
            this.NotePlaybackData = this.RawNotePlaybackData;
        }

        /// <summary>
        /// Gets the raw note.
        /// </summary>
        public Note RawNote { get; }

        /// <summary>
        /// Gets the start time.
        /// </summary>
        public TimeSpan StartTime { get; }

        /// <summary>
        /// Gets the end time.
        /// </summary>
        public TimeSpan EndTime { get; }

        /// <summary>
        /// Gets the raw note playback data.
        /// </summary>
        public NotePlaybackData RawNotePlaybackData { get; }

        /// <summary>
        /// Gets the note playback data.
        /// </summary>
        public NotePlaybackData NotePlaybackData { get; }
    }
}
