// Licensed under the MIT License.

namespace Coimbra.Model
{
    using System.Collections.Generic;
    using Windows.System;
    using Windows.UI;

    /// <summary>
    /// A class encapsulating details about pitches.
    /// </summary>
    public class Pitch
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Pitch"/> class.
        /// </summary>
        /// <param name="index">The pitch index.</param>
        /// <param name="color">The color associated with the pitch in the UI.</param>
        /// <param name="keys">The keys associated with the pitch.</param>
        /// <param name="glyph">The Segoe MDL2 Assets font glyph associated with the pitch.</param>
        /// <param name="noteNames">The note names assigned to this pitch.</param>
        public Pitch(int index, Color color, List<VirtualKey> keys, string glyph, List<string> noteNames)
        {
            this.Index = index;
            this.Color = color;
            this.Keys = keys;
            this.Glyph = glyph;
            this.NoteNames = noteNames;
        }

        /// <summary>
        /// Gets the pitch index.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets the color associated with the pitch in the UI.
        /// </summary>
        public Color Color { get; }

        /// <summary>
        /// Gets the keys associated with the pitch.
        /// </summary>
        public List<VirtualKey> Keys { get; }

        /// <summary>
        /// Gets the Segoe MDL2 Assets font glyph associated with the pitch.
        /// </summary>
        public string Glyph { get; }

        /// <summary>
        /// Gets the note names assigned to this pitch.
        /// </summary>
        public List<string> NoteNames { get; }
    }
}
