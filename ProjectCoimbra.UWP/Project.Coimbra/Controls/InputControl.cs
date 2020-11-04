// Licensed under the MIT License.

namespace Coimbra.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Threading;
	using Coimbra.Model;
	using Microsoft.Toolkit.Uwp.Input.GazeInteraction;
	using Windows.ApplicationModel.Core;
	using Windows.Gaming.Input;
	using Windows.System;
	using Windows.UI;
	using Windows.UI.Core;
	using Windows.UI.Xaml;
	using Windows.UI.Xaml.Controls;
	using Windows.UI.Xaml.Input;
	using Windows.UI.Xaml.Media;
	using Windows.UI.Xaml.Shapes;

	/// <summary>
	/// A wrapper around the input control.
	/// </summary>
	public sealed class InputControl : Control, IDisposable
	{
		/// <summary>
		/// A <see cref="DependencyProperty"/> forming the backing store for the <see cref="Pitch"/> objects, to enable
		/// animation, styling, binding, etc.
		/// </summary>
		public static readonly DependencyProperty PitchesProperty =
			DependencyProperty.Register(
				"Pitches",
				typeof(IEnumerable<string>),
				typeof(InputControl),
				new PropertyMetadata(null, OnPitchesChanged));

		/// <summary>
		/// A <see cref="DependencyProperty"/> forming the backing store for the song title, to enable animation,
		/// styling, binding, etc.
		/// </summary>
		public static readonly DependencyProperty SongTitleProperty =
			DependencyProperty.Register("SongTitle", typeof(string), typeof(InputControl), new PropertyMetadata("Title"));

		/// <summary>
		/// A <see cref="DependencyProperty"/> forming the backing store for the time to next note counter, to enable animation,
		/// styling, binding, etc.
		/// </summary>
		public static readonly DependencyProperty TimeToNextNoteProperty =
			DependencyProperty.Register("TimeToNextNote", typeof(string), typeof(InputControl), new PropertyMetadata("TimeToNextNote"));

		private readonly IList<Gamepad> gamepads = new List<Gamepad>(1);

		private readonly object lockObject = new object();

		private readonly Thread gamepadThread;

		private ItemsControl buttons;

		private ItemsControl pitchTracks;

		private Grid pitchBackground;

		private Canvas notes;

		private Gamepad mainGamepad;

		private Button[] buttonControls;

		private TextBlock[] trackLabels;

		private bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="InputControl"/> class.
		/// </summary>
		public InputControl()
		{
			this.DefaultStyleKey = typeof(InputControl);
			this.SizeChanged += this.InputControl_SizeChanged;
			this.Loaded += this.InputControl_Loaded;

			Gamepad.GamepadAdded += this.Gamepad_GamepadAdded;
			Gamepad.GamepadRemoved += this.Gamepad_GamepadRemoved;

			this.gamepadThread = new Thread(this.PollGamepad);
			this.gamepadThread.Start();
		}

		/// <summary>
		/// The event invoked when the eye gaze control has been interacted with.
		/// </summary>
		public event EventHandler<StateChangedEventArgs> EyeGazeInteracted;

		/// <summary>
		/// The event invoked when a lane button has been clicked.
		/// </summary>
		public event EventHandler<int> LaneButtonClicked;

		/// <summary>
		/// Gets or sets the song title.
		/// </summary>
		public string SongTitle
		{
			get => (string)this.GetValue(SongTitleProperty);
			set => this.SetValue(SongTitleProperty, value);
		}

		/// <summary>
		/// Gets the song title.
		/// </summary>
		public string TimeToNextNote
		{
			get => (string)this.GetValue(TimeToNextNoteProperty);
		}

		/// <summary>
		/// Sets the song title.
		/// </summary>
		public async void SetTimeToNextNote(string value)
		{
			await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
				() =>
				{
					this.SetValue(TimeToNextNoteProperty, value);
				});
		}

		/// <summary>
		/// Gets or sets the name of the pitch rows.
		/// </summary>
		public IReadOnlyList<Pitch> Pitches
		{
			get => (IReadOnlyList<Pitch>)this.GetValue(PitchesProperty);
			set => this.SetValue(PitchesProperty, value);
		}

		/// <summary>
		/// Gets or sets the number of seconds during which a note can be clicked, which should be a value between 1 and
		/// 5. This will extend the length of the active bar as well as the note activation timeframe.
		/// </summary>
		public int SecondsDuringWhichNoteIsActive { get; set; }

		/// <summary>
		/// Disposes of the resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes of the resources.
		/// </summary>
		/// <param name="disposing">A value indicating whether the managed resources should be disposed of.</param>
		public void Dispose(bool disposing)
		{
			if (this.disposed)
			{
				return;
			}

			if (disposing)
			{
				this.gamepadThread.Join();
			}

			this.disposed = true;
		}

		/// <summary>
		/// Plays a note that will be removed in the time of the duration.
		/// </summary>
		/// <param name="pitchIndex">The pitch index.</param>
		/// <param name="duration">The duration.</param>
		/// <param name="noteLengthInPercent">The note length in a percentage of the window size.</param>
		/// <returns>The note control object.</returns>
		public NoteControl PlayNote(int pitchIndex, TimeSpan duration, double noteLengthInPercent)
		{
			var fieldRenderWidth = this.notes.ActualWidth;
			if (Math.Abs(noteLengthInPercent) < 0.0001)
			{
				noteLengthInPercent = 100 / fieldRenderWidth * 16;
			}

			var noteWidth = fieldRenderWidth / 100 * noteLengthInPercent;
			var note = new Rectangle
			{
				Width = noteWidth,
				Height = 16,
				StrokeThickness = 3,
				Stroke = new SolidColorBrush(this.Pitches[pitchIndex].Color),
				RadiusX = 5,
				RadiusY = 5,
			};

			var toPosition = fieldRenderWidth * -2;
			var convertedDuration = 3 * duration;
			var gazeElement = GazeInput.GetGazeElement(note);
			if (gazeElement == null)
			{
				gazeElement = new GazeElement();
				GazeInput.SetGazeElement(note, gazeElement);
			}

			var noteControl = new NoteControl
			{
				Content = note,
				Duration = convertedDuration,
				TranslateX = this.notes.ActualWidth,
				ToPosition = toPosition,
				NoteGazeElement = gazeElement,
				MarkColor = new SolidColorBrush(this.Pitches[pitchIndex].Color),
			};

			var trackHeight = Math.Max(8, (this.pitchTracks.ActualHeight / this.Pitches.Count) - 8);
			Canvas.SetTop(noteControl, (pitchIndex * trackHeight) + (trackHeight / 2) - (note.Height / 2));
			gazeElement.StateChanged += this.OnEyeGazeInteracted;
			this.notes.Children.Add(noteControl);
			return noteControl;
		}

		/// <summary>
		/// Removes a note from the display.
		/// </summary>
		/// <param name="noteControl">The note control corresponding to the object to remove.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="noteControl"/> is null.</exception>
		public void RemoveNote(NoteControl noteControl)
		{
			if (noteControl == null)
			{
				throw new ArgumentNullException(nameof(noteControl));
			}

			var gazeElement = noteControl.NoteGazeElement;
			gazeElement.StateChanged -= this.OnEyeGazeInteracted;
			_ = this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => this.notes.Children.Remove(noteControl));
		}

		/// <summary>
		/// Called when the templates are to be applied.
		/// </summary>
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			this.buttons = this.GetTemplateChild("Buttons") as ItemsControl;
			this.pitchTracks = this.GetTemplateChild("PitchTracks") as ItemsControl;
			this.pitchBackground = this.GetTemplateChild("PitchBackground") as Grid;
			this.notes = this.GetTemplateChild("Notes") as Canvas;

			this.UpdateButtons();
			this.UpdatePitches();
		}

		private static void OnPitchesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is InputControl control)
			{
				control.UpdatePitches();
			}
		}

		/// <summary>
		/// Control loaded.
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">Event.</param>
		private void InputControl_Loaded(object sender, RoutedEventArgs e) => this.UpdateButtons();

		private void PollGamepad()
		{
			while (true)
			{
				this.GetGamepads();
				if (this.gamepads?.Count > 0)
				{
					var reading = this.gamepads[0].GetCurrentReading();
					_ = CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
						CoreDispatcherPriority.Normal,
						() =>
						{
							this.ClickLaneButton(reading, GamepadButtons.A, VirtualKey.A);
							this.ClickLaneButton(reading, GamepadButtons.B, VirtualKey.B);
							this.ClickLaneButton(reading, GamepadButtons.X, VirtualKey.X);
							this.ClickLaneButton(reading, GamepadButtons.Y, VirtualKey.Y);
							this.ClickLaneButton(reading, GamepadButtons.DPadDown, VirtualKey.GamepadDPadDown);
							this.ClickLaneButton(reading, GamepadButtons.DPadUp, VirtualKey.GamepadDPadUp);
							this.ClickLaneButton(reading, GamepadButtons.DPadLeft, VirtualKey.GamepadDPadLeft);
							this.ClickLaneButton(reading, GamepadButtons.DPadRight, VirtualKey.GamepadDPadRight);
							this.ClickLaneButton(reading, GamepadButtons.LeftShoulder, VirtualKey.GamepadLeftShoulder);
							this.ClickLaneButton(reading, GamepadButtons.LeftThumbstick, VirtualKey.GamepadLeftThumbstickButton);
							this.ClickLaneButton(reading, GamepadButtons.Menu, VirtualKey.GamepadMenu);
							this.ClickLaneButton(reading, GamepadButtons.Paddle1, VirtualKey.GamepadRightThumbstickDown);
							this.ClickLaneButton(reading, GamepadButtons.Paddle2, VirtualKey.GamepadRightThumbstickLeft);
							this.ClickLaneButton(reading, GamepadButtons.Paddle3, VirtualKey.GamepadRightThumbstickRight);
							this.ClickLaneButton(reading, GamepadButtons.Paddle4, VirtualKey.GamepadRightThumbstickUp);
							this.ClickLaneButton(reading, GamepadButtons.RightShoulder, VirtualKey.GamepadRightShoulder);
							this.ClickLaneButton(reading, GamepadButtons.RightThumbstick, VirtualKey.GamepadRightThumbstickButton);
							this.ClickLaneButton(reading, GamepadButtons.View, VirtualKey.GamepadView);
						});
				}

				Thread.Sleep(TimeSpan.FromMilliseconds(10));
			}
		}

		private void GetGamepads()
		{
			lock (this.lockObject)
			{
				foreach (var gamepad in Gamepad.Gamepads)
				{
					var gamepadInList = this.gamepads.Contains(gamepad);
					if (!gamepadInList)
					{
						this.gamepads.Add(gamepad);
					}
				}
			}
		}

		private void ClickLaneButton(GamepadReading reading, GamepadButtons button, VirtualKey key)
		{
			if ((reading.Buttons & button) == button)
			{
				for (var currentPitch = 0; currentPitch < this.Pitches.Count; currentPitch++)
				{
					foreach (var pitchKey in this.Pitches[currentPitch].Keys)
					{
						if (pitchKey == key)
						{
							this.ClickLaneButton(this.buttonControls[currentPitch]);
							break;
						}
					}
				}
			}
		}

		private void ClickLaneButton(object sender)
		{
			var finalCurrentButtonId = int.MinValue;
			for (var i = 0; i < this.buttonControls.Length; i++)
			{
				if (this.buttonControls[i] == (Button)sender)
				{
					finalCurrentButtonId = i;
					break;
				}
			}

			if (finalCurrentButtonId != int.MinValue)
			{
				this.LaneButtonClicked?.Invoke(sender, finalCurrentButtonId);
			}
		}

		private void OnEyeGazeInteracted(object sender, StateChangedEventArgs stateChangedEventArgs)
		{
			var noteControl = (NoteControl)this.notes.Children.First(note => ((NoteControl)note).NoteGazeElement == (GazeElement)sender);
			this.EyeGazeInteracted?.Invoke(noteControl, stateChangedEventArgs);
		}

		private void UpdatePitches()
		{
			if (this.pitchTracks?.Items == null || this.Pitches == null)
			{
				return;
			}

			this.pitchTracks.Items.Clear();
			this.trackLabels = new TextBlock[this.Pitches.Count];
			var heightPerButton = Math.Max(8, (this.pitchTracks.ActualHeight / this.Pitches.Count) - 8);
			for (var i = 0; i < this.Pitches.Count; i++)
			{
				var label = new TextBlock
				{
					Height = heightPerButton,
					Margin = new Thickness(4),
					Width = 200,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Center,
				};

				this.trackLabels[i] = label;
				this.pitchTracks.Items.Add(label);
			}

			this.UpdateButtons();
		}

		private void UpdateButtons()
		{
			if (this.buttons?.Items == null || this.Pitches == null || this.Pitches.Count == 0)
			{
				return;
			}

			this.buttonControls = new Button[this.Pitches.Count];
			this.buttons.Items.Clear();
			var heightPerButton = Math.Max(8, (this.buttons.ActualHeight / this.Pitches.Count) - 8);
			for (var i = 0; i < this.Pitches.Count; i++)
			{
				var button = new PitchButton
				{
					Content = this.Pitches[i].Glyph,
					Height = heightPerButton,
					Style = Application.Current.Resources["NoteButtonStyle"] as Style,
					Name = this.Pitches[i].Index.ToString(CultureInfo.CurrentCulture),
					Foreground = new SolidColorBrush(this.Pitches[i].Color),
					LineEndPoint = this.notes.ActualWidth / 5 * this.SecondsDuringWhichNoteIsActive,
				};

				foreach (var pitchKey in this.Pitches[i].Keys)
				{
					button.KeyboardAccelerators.Add(new KeyboardAccelerator
					{
						IsEnabled = true,
						Key = pitchKey,
						Modifiers = VirtualKeyModifiers.None,
					});
				}

				this.buttonControls[i] = button;
				this.buttons.Items.Add(button);

				button.Click += this.Button_Click;
				button.KeyUp += this.Button_KeyUp;
			}
		}

		private void Button_KeyUp(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Enter || e.Key == VirtualKey.Space)
			{
				this.ClickLaneButton(sender);
			}
		}

		private void Button_Click(object sender, RoutedEventArgs e) =>
			this.ClickLaneButton(sender);

		private void UpdateBackground()
		{
			this.pitchBackground.Children.Clear();
			var heightPerTrack = Math.Max(8, (this.pitchTracks.ActualHeight / this.Pitches.Count) - 8);
			for (var currentPitch = 0; currentPitch < this.Pitches.Count; currentPitch++)
			{
				var height = (currentPitch * heightPerTrack) + (heightPerTrack / 2);
				var line = new Line
				{
					X1 = 0,
					Y1 = height,
					X2 = this.ActualWidth,
					Y2 = height,
					Stroke = new SolidColorBrush(Colors.Gray),
					StrokeThickness = 2,
				};
				this.pitchBackground.Children.Add(line);
			}
		}

		private void InputControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (this.buttons == null || this.Pitches == null || this.buttonControls == null || this.trackLabels == null)
			{
				return;
			}

			var heightPerButton = (this.buttons.ActualHeight / this.Pitches.Count) - 8;
			foreach (var item in this.buttonControls)
			{
				item.Height = heightPerButton;
			}

			foreach (var item in this.trackLabels)
			{
				item.Height = heightPerButton;
			}

			this.UpdateBackground();
			this.UpdateButtons();
		}

		private void Gamepad_GamepadRemoved(object sender, Gamepad e)
		{
			lock (this.lockObject)
			{
				var indexRemoved = this.gamepads.IndexOf(e);
				if (indexRemoved < 0)
				{
					return;
				}

				if (this.mainGamepad == this.gamepads[indexRemoved])
				{
					this.mainGamepad = null;
				}

				this.gamepads.RemoveAt(indexRemoved);
			}
		}

		private void Gamepad_GamepadAdded(object sender, Gamepad e)
		{
			// Check if the just-added gamepad is already in the list of gamepads. If it isn't, add it.
			lock (this.lockObject)
			{
				if (!this.gamepads.Contains(e))
				{
					this.gamepads.Add(e);
				}
			}
		}
	}
}
