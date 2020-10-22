﻿using System.Collections.Generic;

namespace Melanchall.DryWetMidi.Interaction
{
    /// <summary>
    /// Represents a time grid which is the set of points in time.
    /// </summary>
    public interface IGrid
    {
        /// <summary>
        /// Gets points in time of the current grid.
        /// </summary>
        /// <param name="tempoMap">Tempo map used to get grid's times.</param>
        /// <returns>Collection of points in time of the current grid.</returns>
        IEnumerable<long> GetTimes(TempoMap tempoMap);
    }
}
