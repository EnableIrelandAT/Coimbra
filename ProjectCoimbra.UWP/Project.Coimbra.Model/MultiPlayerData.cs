// Licensed under the MIT License.

namespace Coimbra.Model
{
    using System;
    using System.Collections.Generic;
    using Windows.Storage;

    /// <summary>
    /// MultiPlayerData.
    /// </summary>
    public static class MultiPlayerData
    {
        /// <summary>
        /// Gets or sets nickName.
        /// </summary>
        public static string NickName { get; set; }

        /// <summary>
        /// Gets or sets selectedSong.
        /// </summary>
        public static StorageFile SelectedSong { get; set; }

        /// <summary>
        /// Gets otherPlayers.
        /// </summary>
        public static Dictionary<string, Player> OtherPlayers { get; } = new Dictionary<string, Player>();

        /// <summary>
        /// Gets or sets StartTime.
        /// </summary>
        public static DateTime StartTime { get; set; }
    }
}
