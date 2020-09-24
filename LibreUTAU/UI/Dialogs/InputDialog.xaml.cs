using System.Windows;
using System.Windows.Input;

namespace LibreUtau.UI.Dialogs {
    public partial class InputDialog : Window {
        public string Text;

        public InputDialog(string info, string initialText = "") {
            InitializeComponent();

            InfoTextBlock.Text = info;

            ContentTextBox.Text = initialText;
            ContentTextBox.Focus();
            ContentTextBox.SelectAll();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e) {
            Text = ContentTextBox.Text;
            this.Close();
        }

        private void ContentTextBox_OnKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                OkButton_OnClick(sender, e);
        }
    }
}
