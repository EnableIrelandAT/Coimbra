// Licensed under the MIT License.

namespace Coimbra.Pages
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using Coimbra.Controls;
    using Coimbra.Midi;
    using Coimbra.Model;
    using Windows.System;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;
    using Windows.UI.Xaml.Media.Animation;

    /// <summary>
    /// A class encapsulating the logic of the time page of the app.
    /// </summary>
    public sealed partial class LaneSettingsPage : Page
    {
        private static readonly MidiEngine MidiEngine = MidiEngine.Instance;

        private List<string> notes;

        /// <summary>
        /// Initializes a new instance of the <see cref="LaneSettingsPage"/> class.
        /// </summary>
        public LaneSettingsPage()
        {
            this.InitializeNotes();
            this.InitializeComponent();
            this.SelectedPitchCount.Maximum = this.notes.Count;
            this.SelectedPitchCount.Value = 2;
            this.InitializeKeys();
            this.InitializeSymbols();
            this.InitializeLanes();
        }

        private ObservableCollection<OrderedString> AvailableKeys { get; set; }

        private ObservableCollection<OrderedString> AvailableSymbols { get; set; }

        private ObservableCollection<OrderedString> AvailableNotes { get; set; }

        private static Color RetrieveColor()
        {
            var random = new Random();
            #pragma warning disable CA5394 // Do not use insecure randomness
            return Color.FromArgb((byte)255, (byte)random.Next(255), (byte)random.Next(255), (byte)random.Next(255));
            #pragma warning restore CA5394 // Do not use insecure randomness
        }

        private void InitializeLanes()
        {
            for (var currentAdd = 0; currentAdd < this.SelectedPitchCount.Value; currentAdd++)
            {
                #pragma warning disable CA2000 // Dispose objects before losing scope
                this.Lanes.Children.Add(new LaneSetupControl(this.AvailableSymbols, this.AvailableNotes, this.AvailableKeys));
                #pragma warning restore CA2000 // Dispose objects before losing scope
            }
        }

        private void InitializeNotes()
        {
            this.notes = MidiEngine.RetrievePitchesForInstrument(MidiEngine.SelectedTrack);
            this.AvailableNotes = new ObservableCollection<OrderedString>();
            this.AvailableNotes.Clear();
            var index = 0;
            this.notes.ForEach((note) => this.AvailableNotes.Add(new OrderedString(index++, note)));
        }

        private void InitializeKeys()
        {
            var index = 0;
            this.AvailableKeys = new ObservableCollection<OrderedString>();
            foreach (var key in Enum.GetValues(typeof(VirtualKey)))
            {
                this.AvailableKeys.Add(new OrderedString(index++, key.ToString()));
            }
        }

        private void InitializeSymbols()
        {
            this.AvailableSymbols = new ObservableCollection<OrderedString>();
            var index = 0;

            // xbox and various symbols
            foreach (var symbol in new string[] { "\uF095", "\uF096", "\uF094", "\uF093", "\uF0AD", "\uF0AE", "\uF0AF", "\uF0B0", "\uF108", "\uF109", "\uF10A", "\uF10B", "\uF10C", "\uF10D", "\uF10E", "\uF136", "\uF137", "\uF138", "\uF139", "\uF13A", "\uF156", "\uF157", "\uF158", "\uF159" })
            {
                this.AvailableSymbols.Add(new OrderedString(index++, symbol));
            }

            // 1 - 16
            for (var symbol = '\uF146'; symbol <= '\uF155'; symbol++)
            {
                this.AvailableSymbols.Add(new OrderedString(index++, symbol.ToString(CultureInfo.InvariantCulture)));
            }

            // arrows
            for (var symbol = '\uE010'; symbol <= '\uE013'; symbol++)
            {
                this.AvailableSymbols.Add(new OrderedString(index++, symbol.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            UserData.PitchMap = this.CreatePitchMap();

            if (UserData.GameMode == UserData.Mode.Offline)
            {
                _ = this.Frame.Navigate(typeof(TimePage), null, new DrillInNavigationTransitionInfo());
                return;
            }

            _ = this.Frame.Navigate(typeof(GamePage), null, new DrillInNavigationTransitionInfo());
        }

        private PitchMap CreatePitchMap()
        {
            var pitchMapItems = new Pitch[this.Lanes.Children.Count];
            var index = 0;
            foreach (var lane in this.Lanes.Children)
            {
                var setup = (LaneSetupControl)lane;
                pitchMapItems[index] = new Pitch(
                    index,
                    setup.PitchBackground.Color,
                    setup.SelectedKeys.Select(key => (VirtualKey)Enum.Parse(typeof(VirtualKey), key.Value)).ToList(),
                    setup.SelectedSymbol.Value,
                    setup.SelectedNotes.Select(note => note.Value).ToList());
                index++;
            }

            return new PitchMap(pitchMapItems);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) =>
            this.Frame.Navigate(typeof(InstrumentsPage), null, new DrillInNavigationTransitionInfo());

        private void SelectedPitchCount_ValueChanged(Microsoft.UI.Xaml.Controls.NumberBox sender, Microsoft.UI.Xaml.Controls.NumberBoxValueChangedEventArgs args)
        {
            if (args.NewValue == this.Lanes.Children.Count)
            {
                return;
            }

            if (args.NewValue > args.OldValue)
            {
                for (var currentAdd = 0; currentAdd < args.NewValue - args.OldValue; currentAdd++)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope
                    var lane = new LaneSetupControl(this.AvailableSymbols, this.AvailableNotes, this.AvailableKeys);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    lane.PropertyChanged += this.Lane_PropertyChanged;
                    this.Lanes.Children.Add(lane);
                }
            }

            if (args.NewValue < args.OldValue)
            {
                for (var currentAdd = 0; currentAdd < args.OldValue - args.NewValue; currentAdd++)
                {
                    ((LaneSetupControl)this.Lanes.Children[this.Lanes.Children.Count - 1]).Dispose();
                    this.Lanes.Children.RemoveAt(this.Lanes.Children.Count - 1);
                }
            }
        }

        private void Lane_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => this.CheckNextButtonState();

        private void CheckNextButtonState()
        {
            foreach (var lane in this.Lanes.Children)
            {
                if (!((LaneSetupControl)lane).IsValid)
                {
                    this.Next.IsEnabled = false;
                    return;
                }
            }

            this.Next.IsEnabled = true;
        }

        private void OptimizeKeyboard_Click(object sender, RoutedEventArgs e)
        {
            foreach (var lane in this.Lanes.Children)
            {
                ((LaneSetupControl)lane).Dispose();
            }

            this.Lanes.Children.Clear();

            var keyboardKeys = new VirtualKey[]
            {
                VirtualKey.Number1,
                VirtualKey.Number2,
                VirtualKey.Number3,
                VirtualKey.Number4,
                VirtualKey.Number5,
                VirtualKey.Number6,
                VirtualKey.Number7,
            };

            this.SelectedPitchCount.Maximum = Math.Max(keyboardKeys.Length, this.notes.Count);

            var notes = new string[] { "C", "D", "E", "F", "G", "A", "B" };

            var currentSymbol = '\uF146';
            for (var currentAdd = 0; currentAdd < keyboardKeys.Length; currentAdd++)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var lane = new LaneSetupControl(this.AvailableSymbols, this.AvailableNotes, this.AvailableKeys);
#pragma warning restore CA2000 // Dispose objects before losing scope
                lane.PropertyChanged += this.Lane_PropertyChanged;
                this.Lanes.Children.Add(lane);

                foreach (var note in this.AvailableNotes.Where(note => note.Value.StartsWith(notes[currentAdd], StringComparison.OrdinalIgnoreCase)).ToList())
                {
                    lane.SelectedNotes.Add(note);
                }

                lane.SelectedKeys.Add(this.AvailableKeys.First(key => string.Equals(key.Value, keyboardKeys[currentAdd].ToString(), StringComparison.Ordinal)));

                lane.SelectedSymbol = this.AvailableSymbols.First(symbol => string.Equals(symbol.Value, currentSymbol.ToString(), StringComparison.Ordinal));
                lane.PitchBackground = new SolidColorBrush(RetrieveColor());

                if (currentSymbol != '\uF155')
                {
                    currentSymbol++;
                }
            }

            this.SelectedPitchCount.Value = keyboardKeys.Length;
        }

        private void OptimizeXbox_Click(object sender, RoutedEventArgs e)
        {
            foreach (var lane in this.Lanes.Children)
            {
                ((LaneSetupControl)lane).Dispose();
            }

            this.Lanes.Children.Clear();

            const int newLanes = 7;
            this.SelectedPitchCount.Maximum = Math.Max(newLanes, this.notes.Count);

            var gamepadKeys = new VirtualKey[newLanes * 2] { VirtualKey.Number1, VirtualKey.Y, VirtualKey.Number2, VirtualKey.X, VirtualKey.Number3, VirtualKey.B, VirtualKey.Number4, VirtualKey.A, VirtualKey.Number5, VirtualKey.GamepadDPadUp, VirtualKey.Number6, VirtualKey.GamepadDPadDown, VirtualKey.Number7, VirtualKey.GamepadDPadLeft };
            var notes = new string[newLanes] { "C", "D", "E", "F", "G", "A", "B" };
            var colors = new Color[newLanes] { Colors.Orange, Colors.Blue, Colors.Red, Colors.Green, Colors.Gray, Colors.Purple, Colors.DeepPink };
            var symbols = new string[newLanes] { "\uF095", "\uF096", "\uF094", "\uF093", "\uF0AD", "\uF0AE", "\uF0B0" };

            for (var currentAdd = 0; currentAdd < newLanes; currentAdd++)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope
                var lane = new LaneSetupControl(this.AvailableSymbols, this.AvailableNotes, this.AvailableKeys);
#pragma warning restore CA2000 // Dispose objects before losing scope
                lane.PropertyChanged += this.Lane_PropertyChanged;
                this.Lanes.Children.Add(lane);

                foreach (var note in this.AvailableNotes.ToList().Where(note => note.Value.StartsWith(notes[currentAdd], StringComparison.InvariantCultureIgnoreCase)))
                {
                    lane.SelectedNotes.Add(note);
                }

                lane.SelectedKeys.Add(this.AvailableKeys.First(key => string.Equals(key.Value, gamepadKeys[currentAdd * 2].ToString(), StringComparison.Ordinal)));
                lane.SelectedKeys.Add(this.AvailableKeys.First(key => string.Equals(key.Value, gamepadKeys[(currentAdd * 2) + 1].ToString(), StringComparison.Ordinal)));

                lane.SelectedSymbol = this.AvailableSymbols.First(symbol => string.Equals(symbol.Value, symbols[currentAdd], StringComparison.Ordinal));
                lane.PitchBackground = new SolidColorBrush(colors[currentAdd]);
            }

            this.SelectedPitchCount.Value = newLanes;
        }
    }
}
