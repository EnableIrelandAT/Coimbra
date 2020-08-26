// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;

    /// <summary>
    /// A class holding the arguments related to markable playback events.
    /// </summary>
    public sealed class MarkablePlaybackEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkablePlaybackEventArgs"/> class.
        /// </summary>
        /// <param name="playbackEvent">The playback event.</param>
        public MarkablePlaybackEventArgs(MarkablePlaybackEvent playbackEvent) =>
            this.PlaybackEvent = playbackEvent;

        /// <summary>
        /// Gets the playback event.
        /// </summary>
        public MarkablePlaybackEvent PlaybackEvent { get; }
    }
}
