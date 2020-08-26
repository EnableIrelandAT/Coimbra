// Licensed under the MIT License.

namespace Coimbra.Model
{
    using System;

    /// <summary>
    /// MidiFileReceivedEventArguments.
    /// </summary>
    public class MidiFileReceivedEventArguments
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MidiFileReceivedEventArguments"/> class.
        /// To ADD.
        /// </summary>
        /// <param name="filePath">File Path.</param>
        public MidiFileReceivedEventArguments(string filePath) => this.FilePath = filePath;

        /// <summary>
        /// Gets FilePath.
        /// </summary>
        public string FilePath { get; }
    }
}
