// Licensed under the MIT License.

namespace Coimbra.Model
{
    /// <summary>
    /// Maps pitches to notes.
    /// </summary>
    public class PitchMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PitchMap"/> class.
        /// </summary>
        /// <param name="pitches">Pitches to be added to this pitchmap.</param>
#pragma warning disable CA1819 // Properties should not return arrays
        public PitchMap(Pitch[] pitches) => this.Pitches = pitches;
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets pitches added to this pitch map.
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public Pitch[] Pitches { get; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
