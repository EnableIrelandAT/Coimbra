// Licensed under the MIT License.

namespace Coimbra.Midi.Models
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text.RegularExpressions;
	using Melanchall.DryWetMidi.Common;
	using Melanchall.DryWetMidi.Standards;

	/// <summary>
	/// A class representing an instrument.
	/// </summary>
	public class InstrumentInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstrumentInfo"/> class.
        /// </summary>
        /// <param name="channel">Channel of the instrument</param>
        /// <param name="programNumbers">Program numbers of the instrument</param>
        /// <param name="noteCount">Note count of the instrument</param>
        public InstrumentInfo(FourBitNumber channel, ICollection<SevenBitNumber> programNumbers, int noteCount)
        {
            Channel = channel;
            ProgramNumbers = programNumbers;
            NoteCount = noteCount;
        }

        /// <summary>
        /// A string that contains the name and the note count of the instrument
        /// </summary>
        public string NameAndNoteCount => string.Format("{0} - {1} notes", string.Join(
                ", ",
                ProgramNumbers.Select(d =>
                    RegularExpression.Replace(
                        Enum.GetName(typeof(GeneralMidi2Program), (int)d) ?? throw new InvalidOperationException(),
                        " $1"))), NoteCount.ToString());

        /// <summary>
        /// Channel of the instrument
        /// </summary>
        public FourBitNumber Channel { get; set; }

        /// <summary>
        /// Program number of the instrument
        /// </summary>
        public ICollection<SevenBitNumber> ProgramNumbers { get; set; }

        /// <summary>
        /// Note count of the instrument
        /// </summary>
        public int NoteCount { get; set; }

        private static readonly Regex RegularExpression = new Regex("(\\B([A-Z]|[0-9]))", RegexOptions.Compiled);
    }
}
