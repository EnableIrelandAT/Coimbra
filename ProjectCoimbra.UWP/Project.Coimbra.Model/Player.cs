// Licensed under the MIT License.

namespace Coimbra.Model
{
    /// <summary>
    /// Player.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Gets or sets NickName.
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// Gets or sets Instrument.
        /// </summary>
        public int Instrument { get; set; } = -1;

        /// <summary>
        /// Gets or sets a value indicating whether ReadyToStart.
        /// </summary>
        public bool ReadyToStart { get; set; }
    }
}
