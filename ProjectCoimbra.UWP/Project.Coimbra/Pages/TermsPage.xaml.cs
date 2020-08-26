// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Windows.System.UserProfile;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the terms page of the app.
    /// </summary>
    public sealed partial class TermsPage : Page
    {
        /// <summary>
        /// Whether the user accepted the terms.
        /// </summary>
        private bool termsAccepted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TermsPage"/> class.
        /// </summary>
        public TermsPage()
        {
            this.InitializeComponent();

            this.TermsContainer.Navigate(new Uri(FormattableString.Invariant($"ms-appx-web:///Strings/{GetTermsCulture()}/Terms.html")));
            this.UpdateChecks();
        }

        private static string GetTermsCulture()
        {
            var allowedCultures = new HashSet<string>(2, StringComparer.OrdinalIgnoreCase)
            {
                "de-DE",
                "en-US",
            };

            var currentCulture = GlobalizationPreferences.Languages[0].ToString(CultureInfo.InvariantCulture);
            return allowedCultures.Contains(currentCulture) ? currentCulture : "en-US";
        }

        private void NextButton_Click(object sender, RoutedEventArgs e) =>
            _ = this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());

        private void AcceptCheck_Click(object sender, RoutedEventArgs e)
        {
            this.termsAccepted = !this.termsAccepted;
            this.UpdateChecks();
        }

        private void UpdateChecks()
        {
            this.NextButton.IsEnabled = this.termsAccepted;
            this.NeedAcceptError.Visibility = this.termsAccepted ? Visibility.Collapsed : Visibility.Visible;
        }
    }
}
