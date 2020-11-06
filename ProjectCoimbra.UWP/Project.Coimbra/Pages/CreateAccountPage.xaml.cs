// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using Coimbra.DataAccess;
    using Windows.ApplicationModel.Resources;
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

        private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataAccess.Exists(Input_Box.Text))
            {
                var res = ResourceLoader.GetForCurrentView();
                this.ErrorBox.Text = res.GetString("CreateAccountPage/Error");
            }
            else
            {
                DataAccess.AddData(Input_Box.Text);
                _ = this.Frame.Navigate(typeof(TermsPage), null, new DrillInNavigationTransitionInfo());
            }
        }

         private void BackToLoginPage_Click(object sender, RoutedEventArgs e)
            => this.Frame.Navigate(typeof(LoginPage));
    }
}
