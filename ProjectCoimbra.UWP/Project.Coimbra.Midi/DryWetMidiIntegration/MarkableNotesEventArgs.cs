// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;
    using System.Collections.Generic;
    using Melanchall.DryWetMidi.Interaction;

    /// <summary>
    /// A class holding a markable notes collection for <c>NotesPlaybackStarted</c> and <c>NotesPlaybackFinished</c>.
    /// </summary>
    public sealed class MarkableNotesEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkableNotesEventArgs" /> class.
        /// </summary>
        /// <param name="notes">The collection of notes that started or finished playing using a <c>Playback</c> object.</param>
        public MarkableNotesEventArgs(params Note[] notes) =>
            this.Notes = notes;

        /// <summary>
        /// Gets notes collection that started or finished to play by a <c>Playback</c>.
        /// </summary>
        public IEnumerable<Note> Notes { get; }
    }
}
