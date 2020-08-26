// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;
    using Coimbra.Controls;

    /// <summary>
    /// A class representing rendered markable playback events.
    /// </summary>
    public class RenderedMarkablePlaybackEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderedMarkablePlaybackEvent"/> class.
        /// </summary>
        /// <param name="markablePlaybackEvent">The markable playback event.</param>
        /// <param name="noteControl">The note control.</param>
        public RenderedMarkablePlaybackEvent(MarkablePlaybackEvent markablePlaybackEvent, NoteControl noteControl)
        {
            this.MarkablePlaybackEvent = markablePlaybackEvent;
            this.NoteControl = noteControl;
        }

        /// <summary>
        /// Gets the first time at which the event was displayed.
        /// </summary>
        public DateTime FirstDisplayed { get; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the markable playback event.
        /// </summary>
        public MarkablePlaybackEvent MarkablePlaybackEvent { get; }

        /// <summary>
        /// Gets the note control.
        /// </summary>
        public NoteControl NoteControl { get; }
    }
}
