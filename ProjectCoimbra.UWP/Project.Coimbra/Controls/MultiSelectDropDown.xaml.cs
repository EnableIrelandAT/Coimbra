// Licensed under the MIT License.

namespace Coimbra.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Linq;
    using Coimbra.Model;
    using Windows.UI.Xaml;
    using Windows.UI.Xaml.Controls;

    /// <summary>
    /// DropDown supporting selection of multiple items.
    /// </summary>
    public sealed partial class MultiSelectDropDown : UserControl
    {
        /// <summary>
        /// Dependency Property for ItemsSource.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(object), typeof(MultiSelectDropDown), new PropertyMetadata(new List<object>(), OnItemsSourcePropertyChanged));

        /// <summary>
        /// Dependency Property for ItemTemplate.
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(MultiSelectDropDown), new PropertyMetadata(null, OnItemTemplatePropertyChanged));

        /// <summary>
        /// Dependency Property for selected items.
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<OrderedString>), typeof(MultiSelectDropDown), new PropertyMetadata(new ObservableCollection<OrderedString>()));

        /// <summary>
        /// Dependency Property for NoItemsSelectedText.
        /// </summary>
        public static readonly DependencyProperty NoItemsSelectedTextProperty =
            DependencyProperty.Register("NoItemsSelectedText", typeof(string), typeof(MultiSelectDropDown), new PropertyMetadata("No selection", OnNoItemsSelectedTextPropertyChanged));

        /// <summary>
        /// Dependency Property for ItemsSelectedTextFormat.
        /// </summary>
        public static readonly DependencyProperty ItemsSelectedTextFormatProperty =
            DependencyProperty.Register("ItemsSelectedTextFormat", typeof(string), typeof(MultiSelectDropDown), new PropertyMetadata("{0} selected", OnItemsSelectedTextFormatPropertyChanged));

        /// <summary>
        /// Dependency Property for MaximumSelectedItems.
        /// </summary>
        public static readonly DependencyProperty MaximumSelectedItemsProperty =
            DependencyProperty.Register("MaximumSelectedItems", typeof(int), typeof(MultiSelectDropDown), new PropertyMetadata(0, OnMaximumSelectedItemsPropertyChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSelectDropDown"/> class.
        /// </summary>
        public MultiSelectDropDown()
        {
            this.InitializeComponent();
            this.Loaded += this.MultiSelectDropDown_Loaded;
        }

        /// <summary>
        /// Gets or sets maximum selected items.
        /// </summary>
        public int MaximumSelectedItems
        {
            get => (int)this.GetValue(MaximumSelectedItemsProperty);
            set => this.SetValue(MaximumSelectedItemsProperty, value);
        }

        /// <summary>
        /// Gets or sets items to display in control.
        /// </summary>
        public object ItemsSource
        {
            get => this.GetValue(ItemsSourceProperty);
            set
            {
                this.SetValue(ItemsSourceProperty, value);
                this.SelectableItems.ItemsSource = value;
            }
        }

        /// <summary>
        /// Gets or sets ItemTemplate.
        /// </summary>
        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)this.GetValue(ItemTemplateProperty);
            set
            {
                this.SetValue(ItemTemplateProperty, value);
                this.SelectableItems.ItemTemplate = value;
            }
        }

        /// <summary>
        /// Gets or sets selected items.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public ObservableCollection<OrderedString> SelectedItems
#pragma warning restore CA2227 // Collection properties should be read only
        {
            get => (ObservableCollection<OrderedString>)this.GetValue(SelectedItemsProperty);
            set => this.SetValue(SelectedItemsProperty, value);
        }

        /// <summary>
        /// Gets or sets text to display when no selection has been made.
        /// </summary>
        public string NoItemsSelectedText
        {
            get => (string)this.GetValue(NoItemsSelectedTextProperty);
            set => this.SetValue(NoItemsSelectedTextProperty, value);
        }

        /// <summary>
        /// Gets or sets format for multiple selection text.
        /// </summary>
        public string ItemsSelectedTextFormat
        {
            get => (string)this.GetValue(ItemsSelectedTextFormatProperty);
            set => this.SetValue(ItemsSelectedTextFormatProperty, value);
        }

        private static void OnNoItemsSelectedTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectDropDown instance && e.NewValue != null)
            {
                instance.NoItemsSelectedText = (string)e.NewValue;
            }
        }

        private static void OnItemsSelectedTextFormatPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectDropDown instance && e.NewValue != null)
            {
                instance.ItemsSelectedTextFormat = (string)e.NewValue;
            }
        }

        private static void OnMaximumSelectedItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectDropDown instance && e.NewValue != null)
            {
                instance.MaximumSelectedItems = (int)e.NewValue;
            }
        }

        private static void OnItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectDropDown instance && e.NewValue != null)
            {
                instance.SelectableItems.ItemsSource = e.NewValue;
            }
        }

        private static void OnItemTemplatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MultiSelectDropDown instance && e.NewValue is DataTemplate template)
            {
                instance.SelectableItems.ItemTemplate = template;
            }
        }

        private void MultiSelectDropDown_Loaded(object sender, RoutedEventArgs e) => this.UpdateSelectionText();

        private void DropDown_Click(object sender, RoutedEventArgs args)
        {
            this.SelectableItems.SelectedItems.Clear();
            foreach (var item in this.SelectedItems)
            {
                this.SelectableItems.SelectedItems.Add(item);
            }
        }

        private void SelectableItems_SelectionChanged(object sender, RoutedEventArgs args)
        {
            if (!this.DropDown.Flyout.IsOpen)
            {
                return;
            }

            foreach (var item in this.SelectedItems.ToList())
            {
                if (!this.SelectableItems.SelectedItems.Contains(item))
                {
                    _ = this.SelectedItems.Remove(item);
                }
            }

            foreach (var item in this.SelectableItems.SelectedItems)
            {
                if (!this.SelectedItems.Contains(item))
                {
                    this.SelectedItems.Add((OrderedString)item);
                }
            }

            this.UpdateSelectionText();
        }

        private void UpdateSelectionText()
        {
            if (this.SelectedItems == null || this.SelectedItems.Count == 0)
            {
                this.DropDown.Content = this.NoItemsSelectedText;
                return;
            }

            if (this.SelectedItems.Count == 1)
            {
                this.DropDown.Content = this.SelectedItems[0].Value.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                this.DropDown.Content = string.Format(CultureInfo.InvariantCulture, this.ItemsSelectedTextFormat.Replace("\\", string.Empty, StringComparison.Ordinal), this.SelectedItems.Count, this.MaximumSelectedItems);
            }

            this.ToggleSelectableItems(!(this.SelectedItems.Count >= this.MaximumSelectedItems && this.MaximumSelectedItems != 0));
        }

        private void ToggleSelectableItems(bool enableItems)
        {
            foreach (var item in this.SelectableItems.Items)
            {
                var container = (ListViewItem)this.SelectableItems.ContainerFromItem(item);
                if (container == null || this.SelectedItems.Contains(item))
                {
                    continue;
                }

                container.IsEnabled = enableItems;
            }
        }
    }
}
