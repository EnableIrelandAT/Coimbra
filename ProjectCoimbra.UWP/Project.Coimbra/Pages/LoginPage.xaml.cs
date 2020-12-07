// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using Coimbra.DataAccess;
    using Windows.ApplicationModel.Resources;
    using Windows.System;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Input;
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

        private void Input_Box_OnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                this.Submit();
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e) =>
            this.Submit();

        private void GoToSignupPage_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(CreateAccountPage));

        private void Submit()
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
    }
}
