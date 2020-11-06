// Licensed under the MIT License.

namespace Coimbra.Model
{
    using System;

    /// <summary>
    /// A class encapsulating user data.
    /// </summary>
    public static class UserData
    {
        /// <summary>
        /// Enum for mode.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// One person
            /// </summary>
            Solo = 0,

            /// <summary>
            /// Online Multiplayer
            /// </summary>
            Online = 1,

            /// <summary>
            /// Offline Multiplayer
            /// </summary>
            Offline = 2,
        }

        /// <summary>
        /// Enum for the note active duration.
        /// </summary>
        public enum Duration
        {
            /// <summary>
            /// The unknown duration.
            /// </summary>
            UnknownDuration = 0,

            /// <summary>
            /// The short duration.
            /// </summary>
            ShortDuration = 1,

            /// <summary>
            /// The medium duration.
            /// </summary>
            MediumDuration = 3,

            /// <summary>
            /// The long duration.
            /// </summary>
            LongDuration = 5,
        }

        /// <summary>
        /// Gets or sets a value indicating what mode is being played.
        /// </summary>
        public static Mode GameMode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the duration for which the note is active.
        /// </summary>
        public static Duration ActiveDuration { get; set; }

        /// <summary>
        /// Gets or sets Nick Name.
        /// </summary>
        public static string NickName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IsOffline.
        /// </summary>
        public static string Song { get; set; }

        /// <summary>
        /// Gets or sets the event start time.
        /// </summary>
        public static TimeSpan? StartTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets IsMultiplayerConductor.
        /// </summary>
        public static bool IsMultiplayerConductor { get; set; }

        /// <summary>
        /// Gets or sets the pitchmapper.
        /// </summary>
        public static PitchMap PitchMap { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is changing an option.
        /// </summary>
        public static bool IsOptionChangeMode { get; set; }
    }
}
