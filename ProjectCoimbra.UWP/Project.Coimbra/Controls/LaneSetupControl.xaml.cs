// Licensed under the MIT License.

namespace Coimbra.Controls
{
    using System;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using Coimbra.Extensions;
    using Coimbra.Model;
    using Windows.ApplicationModel.Resources;
    using Windows.UI;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;
    using Windows.UI.Xaml.Media;

    /// <summary>
    /// Lane Setup Control.
    /// </summary>
    public sealed partial class LaneSetupControl : UserControl, INotifyPropertyChanged, IDisposable
    {
        private readonly ResourceLoader resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

        private string selectedNotesText;

        private string selectedKeysText;

        private OrderedString selectedSymbol;

        private SolidColorBrush pitchBackground = new SolidColorBrush(Colors.Gray);

        /// <summary>
        /// Initializes a new instance of the <see cref="LaneSetupControl"/> class.
        /// </summary>
        /// <param name="availableSymbols">Available symbols to chose from.</param>
        /// <param name="availableNotes">Available notes to chose from.</param>
        /// <param name="availableKeys">Available keys to chose from.</param>
        public LaneSetupControl(ObservableCollection<OrderedString> availableSymbols, ObservableCollection<OrderedString> availableNotes, ObservableCollection<OrderedString> availableKeys)
        {
            this.AvailableSymbols = availableSymbols ?? throw new ArgumentNullException(nameof(availableSymbols));
            this.AvailableNotes = availableNotes ?? throw new ArgumentNullException(nameof(availableNotes));
            this.AvailableKeys = availableKeys ?? throw new ArgumentNullException(nameof(availableKeys));

            this.AllSymbolsToDisplay = new ObservableCollection<OrderedString>(availableSymbols.ToList());

            this.AllNotesToDisplay = new ObservableCollection<OrderedString>(availableNotes.ToList());
            this.SelectedNotes = new ObservableCollection<OrderedString>();

            this.AllKeysToDisplay = new ObservableCollection<OrderedString>(availableKeys.ToList());
            this.SelectedKeys = new ObservableCollection<OrderedString>();

            this.InitializeComponent();
            this.SelectedNotes.CollectionChanged += this.SelectedNotes_CollectionChanged;
            this.AvailableNotes.CollectionChanged += this.AvailableNotes_CollectionChanged;
            this.SelectedKeys.CollectionChanged += this.SelectedKeys_CollectionChanged;
            this.AvailableKeys.CollectionChanged += this.AvailableKeys_CollectionChanged;
            this.AvailableSymbols.CollectionChanged += this.AvailableSymbols_CollectionChanged;
            this.UpdateSelectedNotesText();
            this.UpdateSelectedKeysText();
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets a value indicating whether current configuration is valid.
        /// </summary>
        public bool IsValid => this.pitchBackground != null && this.SelectedKeys.Count != 0 && this.SelectedSymbol != null;

        /// <summary>
        /// Gets or sets background of pitch lane.
        /// </summary>
        public SolidColorBrush PitchBackground
        {
            get => this.pitchBackground;
            set
            {
                this.pitchBackground = value;
                if (value != null)
                {
                    this.LaneColorPicker.Color = value.Color;
                }

                this.NotifyPropertyChanged("PitchBackground");
            }
        }

        /// <summary>
        /// Gets or sets text listing selected notes.
        /// </summary>
        public string SelectedNotesText
        {
            get => this.selectedNotesText;
            set
            {
                this.selectedNotesText = value;
                this.NotifyPropertyChanged("SelectedNotesText");
            }
        }

        /// <summary>
        /// Gets or sets text listing selected keys.
        /// </summary>
        public string SelectedKeysText
        {
            get => this.selectedKeysText;
            set
            {
                this.selectedKeysText = value;
                this.NotifyPropertyChanged("SelectedKeysText");
            }
        }

        /// <summary>
        /// Gets notes selected by this lane.
        /// </summary>
        public ObservableCollection<OrderedString> SelectedNotes { get; }

        /// <summary>
        /// Gets available notes not selected by any lane setup.
        /// </summary>
        public ObservableCollection<OrderedString> AvailableNotes { get; }

        /// <summary>
        /// Gets all notes that should be displayed by this instance.
        /// </summary>
        public ObservableCollection<OrderedString> AllNotesToDisplay { get; }

        /// <summary>
        /// Gets keys selected by this lane.
        /// </summary>
        public ObservableCollection<OrderedString> SelectedKeys { get; }

        /// <summary>
        /// Gets available keys not selected by any lane setup.
        /// </summary>
        public ObservableCollection<OrderedString> AvailableKeys { get; }

        /// <summary>
        /// Gets both available and selected keys.
        /// </summary>
        public ObservableCollection<OrderedString> AllKeysToDisplay { get; }

        /// <summary>
        /// Gets or sets selected symbol.
        /// </summary>
        public OrderedString SelectedSymbol
        {
            get => this.selectedSymbol;
            set
            {
                this.selectedSymbol = value;
                this.NotifyPropertyChanged("SelectedSymbol");
            }
        }

        /// <summary>
        /// Gets available symbols.
        /// </summary>
        public ObservableCollection<OrderedString> AvailableSymbols { get; }

        /// <summary>
        /// Gets all symbols that should be displayed bis this instance.
        /// </summary>
        public ObservableCollection<OrderedString> AllSymbolsToDisplay { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.SelectedNotes.CollectionChanged -= this.SelectedNotes_CollectionChanged;
            this.AvailableNotes.CollectionChanged -= this.AvailableNotes_CollectionChanged;
            this.SelectedKeys.CollectionChanged -= this.SelectedKeys_CollectionChanged;
            this.AvailableKeys.CollectionChanged -= this.AvailableKeys_CollectionChanged;
            this.AvailableSymbols.CollectionChanged -= this.AvailableSymbols_CollectionChanged;

            if (this.SelectedSymbol != null)
            {
                this.AvailableSymbols.SortIn(this.SelectedSymbol, (OrderedString input) => input.Position);
            }

            foreach (var key in this.SelectedKeys)
            {
                if (key == null)
                {
                    continue;
                }

                this.AvailableKeys.SortIn(key, (OrderedString input) => input.Position);
            }

            foreach (var note in this.SelectedNotes)
            {
                if (note == null)
                {
                    continue;
                }

                this.AvailableNotes.SortIn(note, (OrderedString input) => input.Position);
            }
        }

        private void NotifyPropertyChanged(string name) => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void AvailableSymbols_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Remove selected symbols in other controls from this control
            foreach (var symbol in this.AllSymbolsToDisplay.ToList())
            {
                if (this.SelectedSymbol != symbol && !this.AvailableSymbols.Contains(symbol))
                {
                    _ = this.AllSymbolsToDisplay.Remove(symbol);
                }
            }

            foreach (var symbol in this.AvailableSymbols)
            {
                if (this.AllSymbolsToDisplay.Contains(symbol))
                {
                    continue;
                }

                this.AllSymbolsToDisplay.SortIn(symbol, (OrderedString input) => input.Position);
            }
        }

        private void AvailableNotes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Remove selected notes in other controls from this control
            foreach (var note in this.AllNotesToDisplay.ToList())
            {
                if (!this.SelectedNotes.Contains(note) && !this.AvailableNotes.Contains(note))
                {
                    _ = this.AllNotesToDisplay.Remove(note);
                }
            }

            foreach (var note in this.AvailableNotes)
            {
                if (this.AllNotesToDisplay.Contains(note))
                {
                    continue;
                }

                this.AllNotesToDisplay.SortIn(note, (OrderedString input) => input.Position);
            }
        }

        private void AvailableKeys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Remove selected keys in other controls from this control
            foreach (var key in this.AllKeysToDisplay.ToList())
            {
                if (!this.SelectedKeys.Contains(key) && !this.AvailableKeys.Contains(key))
                {
                    _ = this.AllKeysToDisplay.Remove(key);
                }
            }

            foreach (var key in this.AvailableKeys)
            {
                if (this.AllKeysToDisplay.Contains(key))
                {
                    continue;
                }

                this.AllKeysToDisplay.SortIn(key, (OrderedString input) => input.Position);
            }
        }

        private void UpdateSelectedNotesText() => this.SelectedNotesText = string.Format(CultureInfo.InvariantCulture, this.resourceLoader.GetString("LaneSetupControl.NotesSelected"), string.Join(",", this.SelectedNotes.Select(note => note.Value)));

        private void UpdateSelectedKeysText() => this.SelectedKeysText = string.Format(CultureInfo.InvariantCulture, this.resourceLoader.GetString("LaneSetupControl.KeysSelected"), string.Join(",", this.SelectedKeys.Select(key => key.Value)));

        private void SelectedNotes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.UpdateSelectedNotesText();

            // Remove selected notes from other controls
            foreach (var note in this.SelectedNotes)
            {
                _ = this.AvailableNotes.Remove(note);
            }

            // Add previously selected notes to other controls
            foreach (var note in this.AllNotesToDisplay)
            {
                if (this.AvailableNotes.Contains(note) || this.SelectedNotes.Contains(note))
                {
                    continue;
                }

                this.AvailableNotes.SortIn(note, (OrderedString input) => input.Position);
            }
        }

        private void SelectedKeys_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.UpdateSelectedKeysText();

            // Remove selected keys from other controls
            foreach (var key in this.SelectedKeys)
            {
                _ = this.AvailableKeys.Remove(key);
            }

            // Add previously selected keys to other controls
            foreach (var key in this.AllKeysToDisplay)
            {
                if (this.AvailableKeys.Contains(key) || this.SelectedKeys.Contains(key))
                {
                    continue;
                }

                this.AvailableKeys.SortIn(key, (OrderedString input) => input.Position);
            }
        }

        private void ColorConfirm_Click(object sender, RoutedEventArgs e)
        {
            this.PitchBackground = new SolidColorBrush(this.LaneColorPicker.Color);
            this.ChangeColor.Flyout.Hide();
        }

        private void ColorCancel_Click(object sender, RoutedEventArgs e) => this.ChangeColor.Flyout.Hide();

        private void SymbolCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Add previously selected symbol to other controls
            foreach (var symbol in this.AllSymbolsToDisplay)
            {
                if (this.AvailableSymbols.Contains(symbol) || this.SelectedSymbol == symbol)
                {
                    continue;
                }

                this.AvailableSymbols.SortIn(symbol, (OrderedString input) => input.Position);
            }

            // Remove selected symbol from other controls
            _ = this.AvailableSymbols.Remove(this.SelectedSymbol);
        }
    }
}
