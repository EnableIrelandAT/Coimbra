// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using System;
    using Coimbra.Model;
    using Windows.ApplicationModel.Resources;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the time page of the app.
    /// </summary>
    public sealed partial class TimePage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimePage"/> class.
        /// </summary>
        public TimePage()
        {
            this.InitializeComponent();
            this.TimePicker.Time = UserData.StartTime ?? DateTime.Now.TimeOfDay + TimeSpan.FromMinutes(3);
        }

        private void TimePicked(object sender, TimePickerSelectedValueChangedEventArgs e)
        {
            if (this.TimePicker.Time <= DateTime.Now.TimeOfDay)
            {
                this.Next.IsEnabled = false;
                return;
            }

            this.Next.IsEnabled = true;
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.TimePicker.Time > DateTime.Now.TimeOfDay)
            {
                UserData.StartTime = this.TimePicker.Time;
                _ = this.Frame.Navigate(typeof(GamePage), null, new DrillInNavigationTransitionInfo());
            }
            else
            {
                var res = ResourceLoader.GetForCurrentView();
                this.ErrorBox.Text = res.GetString("TimePage/Error");
                this.Next.IsEnabled = false;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(InstrumentsPage), null, new DrillInNavigationTransitionInfo());
    }
}
