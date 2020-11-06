// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using Coimbra.DataAccess;
    using Windows.ApplicationModel.Resources;
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

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataAccess.Exists(Input_Box.Text))
            {
                _ = this.Frame.Navigate(typeof(ModePage), null, new DrillInNavigationTransitionInfo());
            } 
            else
            {
                var res = ResourceLoader.GetForCurrentView();
                this.ErrorBox.Text = res.GetString("LoginPage/Error");
            }
        }

        private void GoToSignupPage_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(CreateAccountPage));
    }
}
