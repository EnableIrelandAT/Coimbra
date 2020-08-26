// Licensed under the MIT License.

namespace Coimbra.DryWetMidiIntegration
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Melanchall.DryWetMidi.Core;

    /// <summary>
    /// A comparer for <see cref="MarkablePlaybackEvent"/>.
    /// </summary>
    /// <seealso cref="IComparer{T}"/>
    /// <seealso cref="MarkablePlaybackEvent"/>
    public sealed class MarkablePlaybackEventComparer : IComparer<MarkablePlaybackEvent>, IComparer
    {
        /// <summary>
        /// Compares two playback events and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x">x</paramref> and
        /// <paramref name="y">y</paramref>. If less than 0, <paramref name="x">x</paramref> is less than
        /// <paramref name="y">y</paramref>. If 0, <paramref name="x">x</paramref> equals
        /// <paramref name="y">y</paramref>. If greater than 0, <paramref name="x">x</paramref> is greater than
        /// <paramref name="y">y</paramref>.</returns>
        public int Compare(MarkablePlaybackEvent x, MarkablePlaybackEvent y)
        {
            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            var timeDifference = x.RawTime - y.RawTime;
            if (timeDifference != 0)
            {
                return Math.Sign(timeDifference);
            }

            if (!(x.Event is ChannelEvent channelXEvent) || !(y.Event is ChannelEvent channelYEvent))
            {
                return 0;
            }

            if (!(channelXEvent is NoteEvent) && channelYEvent is NoteEvent)
            {
                return -1;
            }

            if (channelXEvent is NoteEvent && !(channelYEvent is NoteEvent))
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Compares two playback events and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>A signed integer that indicates the relative values of <paramref name="x">x</paramref> and
        /// <paramref name="y">y</paramref>. If less than 0, <paramref name="x">x</paramref> is less than
        /// <paramref name="y">y</paramref>. If 0, <paramref name="x">x</paramref> equals
        /// <paramref name="y">y</paramref>. If greater than 0, <paramref name="x">x</paramref> is greater than
        /// <paramref name="y">y</paramref>.</returns>
        int IComparer.Compare(object x, object y)
        {
            if (x == y)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            return this.Compare(x as MarkablePlaybackEvent, y as MarkablePlaybackEvent);
        }
    }
}
