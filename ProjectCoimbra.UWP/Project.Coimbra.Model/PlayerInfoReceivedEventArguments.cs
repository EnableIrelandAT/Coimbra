// Licensed under the MIT License.

namespace Coimbra.Model
{
    using System;

    /// <summary>
    /// PlayerInfoReceivedEventArguments.
    /// </summary>
    public class PlayerInfoReceivedEventArguments
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerInfoReceivedEventArguments"/> class.
        /// </summary>
        /// <param name="player">Player.</param>
        public PlayerInfoReceivedEventArguments(Player player) => this.PlayerInfo = player;

        /// <summary>
        /// Gets PlayerInfo.
        /// </summary>
        public Player PlayerInfo { get; }
    }
}
