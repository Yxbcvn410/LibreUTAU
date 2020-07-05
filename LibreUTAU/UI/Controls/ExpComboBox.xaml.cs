using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using LibreUtau.Core.Commands;

namespace LibreUtau.UI.Controls {
    /// <summary>
    ///     Interaction logic for ExpComboBox.xaml
    /// </summary>
    public partial class ExpComboBox : UserControl {
        public static readonly DependencyProperty SelectedIndexProperty =
            DependencyProperty.Register("SelectedIndex", typeof(int), typeof(ExpComboBox), new PropertyMetadata(0));

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<string>), typeof(ExpComboBox));

        public static readonly DependencyProperty TagBrushProperty = DependencyProperty.Register("TagBrush",
            typeof(Brush), typeof(ExpComboBox), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register("Highlight",
            typeof(Brush), typeof(ExpComboBox), new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(ExpComboBox), new PropertyMetadata(""));

        public ExpComboBox() {
            InitializeComponent();
        }

        public int SelectedIndex {
            set { SetValue(SelectedIndexProperty, value); }
            get { return (int)GetValue(SelectedIndexProperty); }
        }

        public ObservableCollection<string> ItemsSource {
            set { SetValue(ItemsSourceProperty, value); }
            get { return (ObservableCollection<string>)GetValue(ItemsSourceProperty); }
        }

        public Brush TagBrush {
            set { SetValue(TagBrushProperty, value); }
            get { return (Brush)GetValue(TagBrushProperty); }
        }

        public Brush Highlight {
            set { SetValue(HighlightProperty, value); }
            get { return (Brush)GetValue(HighlightProperty); }
        }

        public string Text { set { SetValue(TextProperty, value); } get { return (string)GetValue(TextProperty); } }
        public event EventHandler Click;
        public event EventHandler SelectionChanged;

        private void mainGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            EventHandler handler = Click;
            if (handler != null) handler(this, e);
        }

        private void dropList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            string name = ItemsSource[SelectedIndex];
            string abbr = CommandDispatcher.Inst.Project.ExpressionTable[name].Abbr;
            Text = abbr.Substring(0, Math.Min(3, abbr.Length));
            EventHandler handler = SelectionChanged;
            if (handler != null) handler(this, e);
        }
    }
}
