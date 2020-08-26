// Licensed under the MIT License.

namespace Coimbra.Model
{
    using System;

    /// <summary>
    /// StartTimeInfoReceivedEventArgs.
    /// </summary>
    public class StartTimeInfoReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartTimeInfoReceivedEventArgs"/> class.
        /// To ADD.
        /// </summary>
        /// <param name="startTime">Start Time.</param>
        public StartTimeInfoReceivedEventArgs(DateTime startTime) => this.StartTime = startTime;

        /// <summary>
        /// Gets StartTime.
        /// </summary>
        public DateTime StartTime { get; }
    }
}
