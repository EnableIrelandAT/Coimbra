// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using DataAccessLibrary;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// The Signup / Create Account Page
    /// </summary>
    public sealed partial class CreateAccountPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccountPage"/> class.
        /// </summary>
        public CreateAccountPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Handles the Click event of the CreateAccountButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataAccess.Exists(Input_Box.Text))
            {
                ErrorMessage.Text = "This nickname already exists. Please try again.";
            }
            else
            {
                DataAccess.AddData(Input_Box.Text);
                _ = this.Frame.Navigate(typeof(TermsPage), null, new DrillInNavigationTransitionInfo());
            }
        }
        /// <summary>
        /// Handles the Click event of the BackToLoginPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
         private void BackToLoginPage_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(LoginPage));
        }
    }
}
