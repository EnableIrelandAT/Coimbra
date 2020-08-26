// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;

    /// <summary>
    /// A class representing track notification events.
    /// </summary>
    public class TrackNotificationEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrackNotificationEventArgs"/> class.
        /// </summary>
        /// <param name="isPlayed">A value indicating whether the note has been played.</param>
        /// <param name="note">The note.</param>
        public TrackNotificationEventArgs(bool isPlayed, string note)
        {
            this.IsPlayed = isPlayed;
            this.Note = note;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the note has been played.
        /// </summary>
        public bool IsPlayed { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        public string Note { get; set; }
    }
}
