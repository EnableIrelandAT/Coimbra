namespace Coimbra.Controls
{
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// Pitch button.
    /// </summary>
    public class PitchButton : Button
    {
        /// <summary>
        /// A <see cref="DependencyProperty"/> forming the backing store for the line endpoint
        /// styling, binding, etc.
        /// </summary>
        public static readonly DependencyProperty LineEndPointProperty =
            DependencyProperty.Register(
                "LineEndPoint",
                typeof(double),
                typeof(PitchButton),
                new PropertyMetadata(0));

        /// <summary>
        /// Gets or sets line width.
        /// </summary>
        public double LineEndPoint
        {
            get => (double)this.GetValue(LineEndPointProperty);
            set => this.SetValue(LineEndPointProperty, value);
        }
    }
}
