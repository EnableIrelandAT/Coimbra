// Licensed under the MIT License.

namespace Coimbra.OSIntegration
{
    using System;
    using Windows.Media;
    using Windows.Media.Playback;

    /// <summary>
    /// A wrapper around the OS media controls.
    /// </summary>
    public class MediaControls : IDisposable
    {
        private static MediaControls current;

        private readonly MediaPlayer mediaPlayer;

        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MediaControls"/> class.
        /// </summary>
        private MediaControls()
        {
            this.mediaPlayer = new MediaPlayer();
            var mediaPlaybackCommandManager = this.mediaPlayer.CommandManager;
            mediaPlaybackCommandManager.IsEnabled = false;

            var systemMediaTransportControls = this.mediaPlayer.SystemMediaTransportControls;
            systemMediaTransportControls.ButtonPressed += this.SystemMediaTransportControls_ButtonPressed;
            systemMediaTransportControls.IsPlayEnabled = true;
            systemMediaTransportControls.IsPauseEnabled = true;
        }

        /// <summary>
        /// The event the occurs when the OS Play button is pressed.
        /// </summary>
        public event EventHandler PlayPressed;

        /// <summary>
        /// The event the occurs when the OS Pause button is pressed.
        /// </summary>
        public event EventHandler PausePressed;

        /// <summary>
        /// Gets the current set of media controls.
        /// </summary>
        public static MediaControls Current =>
#pragma warning disable IDE0074 // Use compound assignment
            current ?? (current = new MediaControls());
#pragma warning restore IDE0074 // Use compound assignment

        /// <summary>
        /// Disposes of the resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources.
        /// </summary>
        /// <param name="disposing">A value indicating whether the managed resources should be disposed of.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.mediaPlayer?.Dispose();
            }

            this.disposed = true;
        }

        private void SystemMediaTransportControls_ButtonPressed(
            SystemMediaTransportControls sender,
            SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    this.PlayPressed?.Invoke(this, EventArgs.Empty);
                    break;

                case SystemMediaTransportControlsButton.Pause:
                    this.PausePressed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
    }
}
