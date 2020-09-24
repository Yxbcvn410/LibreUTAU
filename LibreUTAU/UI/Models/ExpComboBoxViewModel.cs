using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using LibreUtau.Core.Commands;
using LibreUtau.UI.Controls;

namespace LibreUtau.UI.Models {
    class ExpComboBoxViewModel : INotifyPropertyChanged, ICmdSubscriber {
        ExpDisMode _displayMode = ExpDisMode.Hidden;

        int _selectedIndex;

        public int Index;

        public ExpComboBoxViewModel() { CommandDispatcher.Inst.AddSubscriber(this); }

        public int SelectedIndex {
            set {
                _selectedIndex = value;
                OnPropertyChanged("SelectedIndex");
            }
            get { return _selectedIndex; }
        }

        public ObservableCollection<string> Keys { get; private set; } = new ObservableCollection<string>();

        public ExpDisMode DisplayMode {
            set {
                if (_displayMode != value) {
                    _displayMode = value;
                    OnPropertyChanged("TagBrush");
                    OnPropertyChanged("Background");
                    OnPropertyChanged("Highlight");
                }
            }
            get { return _displayMode; }
        }

        public Brush TagBrush {
            get {
                return DisplayMode == ExpDisMode.Visible ? ThemeManager.BlackKeyNameBrushNormal :
                    DisplayMode == ExpDisMode.Shadow ? ThemeManager.CenterKeyNameBrushNormal :
                    ThemeManager.WhiteKeyNameBrushNormal;
            }
        }

        public Brush Background {
            get {
                return DisplayMode == ExpDisMode.Visible ? ThemeManager.BlackKeyBrushNormal :
                    DisplayMode == ExpDisMode.Shadow ? ThemeManager.CenterKeyBrushNormal :
                    ThemeManager.WhiteKeyBrushNormal;
            }
        }

        public Brush Highlight {
            get {
                return DisplayMode == ExpDisMode.Visible ? Brushes.Black :
                    DisplayMode == ExpDisMode.Shadow ? Brushes.Black : Brushes.Black;
            }
        }

        # region ICmdSubscriber

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            if (cmd is ChangeExpressionListNotification || cmd is LoadProjectNotification) OnListChange();
            else if (cmd is LoadPartNotification) {
                if (Keys.Count == 0) OnListChange();
            } else if (cmd is SelectExpressionNotification) OnSelectExp((SelectExpressionNotification)cmd);
        }

        # endregion

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public void CreateBindings(ExpComboBox box) {
            box.DataContext = this;
            box.SetBinding(ExpComboBox.ItemsSourceProperty, new Binding("Keys") {Source = this});
            box.SetBinding(ExpComboBox.SelectedIndexProperty,
                new Binding("SelectedIndex") {Source = this, Mode = BindingMode.TwoWay});
            box.SetBinding(ExpComboBox.TagBrushProperty, new Binding("TagBrush") {Source = this});
            box.SetBinding(Control.BackgroundProperty, new Binding("Background") {Source = this});
            box.SetBinding(ExpComboBox.HighlightProperty, new Binding("Highlight") {Source = this});
            box.Click += box_Click;
            box.SelectionChanged += box_SelectionChanged;
        }

        void box_Click(object sender, EventArgs e) {
            if (DisplayMode != ExpDisMode.Visible)
                CommandDispatcher.Inst.ExecuteCmd(new SelectExpressionNotification(Keys[SelectedIndex], this.Index,
                    true));
        }

        void box_SelectionChanged(object sender, EventArgs e) {
            if (DisplayMode != ExpDisMode.Visible)
                CommandDispatcher.Inst.ExecuteCmd(new SelectExpressionNotification(Keys[SelectedIndex], this.Index,
                    true));
            else
                CommandDispatcher.Inst.ExecuteCmd(new SelectExpressionNotification(Keys[SelectedIndex], this.Index,
                    false));
        }

        # region Cmd Handling

        private void OnListChange() {
            Keys = new ObservableCollection<string>(CommandDispatcher.Inst.Project.ExpressionTable.Keys);
            if (Keys.Count > 0) SelectedIndex = Index % Keys.Count;
            OnPropertyChanged("Keys");
        }

        private void OnSelectExp(SelectExpressionNotification cmd) {
            if (Keys.Count == 0) return;
            if (cmd.SelectorIndex == this.Index) {
                if (Keys[SelectedIndex] != cmd.ExpKey) SelectedIndex = Keys.IndexOf(cmd.ExpKey);
                DisplayMode = ExpDisMode.Visible;
            } else if (cmd.UpdateShadow) {
                DisplayMode = DisplayMode == ExpDisMode.Visible ? ExpDisMode.Shadow : ExpDisMode.Hidden;
            }
        }

        # endregion
    }
}
