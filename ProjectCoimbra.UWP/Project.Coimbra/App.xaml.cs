// Licensed under the MIT License.

namespace Coimbra
{
    using System;
    using System.Globalization;
    using Coimbra.Communication;
    using Coimbra.Pages;
    using DataAccessLibrary;
    using Microsoft.Toolkit.Uwp.Input.GazeInteraction;
    using Windows.ApplicationModel;
    using Windows.ApplicationModel.Activation;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Navigation;

    /// <summary>
    /// Provides app-specific behavior to supplement the default <see cref="Application"/> class.
    /// </summary>
    public sealed partial class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class. This initializes the singleton app object and is
        /// the first line of authored code executed, and as such is the logical equivalent of <c>Main()</c>.
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
            NetworkListener.StartListener();

            DataAccess.InitializeDatabase();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="args"/> is null.</exception>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            if (args == null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Do not repeat app initialization when the window already has content. Instead ensure that the window is
            // active.
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a frame to act as the navigation context and navigate to the first page.
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current window.
                Window.Current.Content = rootFrame;
            }

            // Navigate to the login page once application has started
            _ = rootFrame.Navigate(typeof(LoginPage), args.Arguments);

            Window.Current.Activate();
            GazeInput.Interaction = Interaction.Enabled;
        }

        private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e) =>
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Failed to load page: {0}", e.SourcePageType.FullName));

        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            deferral.Complete();
        }
    }
}
