// Licensed under the MIT License.

namespace Coimbra.Controls
{
    using System;
    using Microsoft.Toolkit.Uwp.Input.GazeInteraction;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;
    using Windows.UI.Xaml.Shapes;

    /// <summary>
    /// A wrapper around the note control.
    /// </summary>
    public sealed class NoteControl : ContentControl
    {
        /// <summary>
        /// A <see cref="DependencyProperty"/> forming the backing store for the durations, to enable animation,
        /// styling, binding, etc.
        /// </summary>
        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                "Duration",
                typeof(TimeSpan),
                typeof(NoteControl),
                new PropertyMetadata(TimeSpan.Zero));

        /// <summary>
        /// A <see cref="DependencyProperty"/> forming the backing store for the X-axis translations, to enable
        /// animation, styling, binding, etc.
        /// </summary>
        public static readonly DependencyProperty TranslateXProperty =
            DependencyProperty.Register(
                "TranslateX",
                typeof(double),
                typeof(NoteControl),
                new PropertyMetadata(0));

        /// <summary>
        /// A <see cref="DependencyProperty"/> forming the backing store for the destination positions, to enable
        /// animation, styling, binding, etc.
        /// </summary>
        public static readonly DependencyProperty ToPositionProperty =
            DependencyProperty.Register(
                "ToPosition",
                typeof(double),
                typeof(NoteControl),
                new PropertyMetadata(0));

        private Border border;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoteControl"/> class.
        /// </summary>
        public NoteControl()
        {
            this.DefaultStyleKey = typeof(NoteControl);
            this.Loaded += this.NoteControl_Loaded;
        }

        /// <summary>
        /// Gets or sets the note gaze element.
        /// </summary>
        public GazeElement NoteGazeElement { get; set; }

        /// <summary>
        /// Gets or sets duration.
        /// </summary>
        public TimeSpan Duration
        {
            get => (TimeSpan)this.GetValue(DurationProperty);
            set => this.SetValue(DurationProperty, value);
        }

        /// <summary>
        /// Gets or sets the X-axis translation.
        /// </summary>
        public double TranslateX
        {
            get => (double)this.GetValue(TranslateXProperty);
            set => this.SetValue(TranslateXProperty, value);
        }

        /// <summary>
        /// Gets or sets the destination position.
        /// </summary>
        public double ToPosition
        {
            get => (double)this.GetValue(ToPositionProperty);
            set => this.SetValue(ToPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush color.
        /// </summary>
        public SolidColorBrush MarkColor { get; set; }

        /// <summary>
        /// Marks the note as played.
        /// </summary>
        public void Mark()
        {
            if (!(this.Content is Rectangle rectangle))
            {
                return;
            }

            rectangle.Fill = this.MarkColor;
        }

        /// <summary>
        /// Invoked when the template is applied.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.border = this.GetTemplateChild("Border") as Border;
            if (!(this.GetTemplateChild("TranslateX") is DoubleAnimation translateAnimation))
            {
                return;
            }

            translateAnimation.From = this.TranslateX;
            translateAnimation.Duration = this.Duration;
            translateAnimation.To = this.ToPosition;
        }

        private void NoteControl_Loaded(object sender, RoutedEventArgs e)
        {
            var playNote = this.border.Resources["PlayNote"] as Storyboard;
            playNote?.Begin();
        }
    }
}
