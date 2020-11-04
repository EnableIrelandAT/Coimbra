// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using DataAccessLibrary;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoginPage"/> class.
        /// </summary>
        public LoginPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles the Click event of the LoginButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataAccess.Exists(Input_Box.Text))
            {
                _ = this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());
            } else
            {
                ErrorMessage.Text = " Nickname does not exist. Please signup via link below or try again.";
            }
        }

        /// <summary>
        /// Handles the Click event of the GoToSignupPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void GoToSignupPage_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(CreateAccountPage));
    }
}
