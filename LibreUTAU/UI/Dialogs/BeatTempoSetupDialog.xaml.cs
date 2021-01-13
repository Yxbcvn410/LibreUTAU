using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using LibreUtau.Core;
using LibreUtau.Core.Commands;

namespace LibreUtau.UI.Dialogs {
    public partial class BeatTempoSetupDialog {
        private DateTime LastTapTime;

        public BeatTempoSetupDialog() {
            InitializeComponent();
            TempoTextBox.Text = Convert.ToString(CommandDispatcher.Inst.Project.BPM, CultureInfo.InvariantCulture);
            BeatPerBarTextBox.Text = Convert.ToString(CommandDispatcher.Inst.Project.BeatPerBar);
            BeatUnitComboBox.ItemsSource = MusicMath.PossibleBeatUnits;
            BeatUnitComboBox.SelectedItem = CommandDispatcher.Inst.Project.BeatUnit;
            LastTapTime = DateTime.Now;
        }

        private bool IsValid() => int.TryParse(BeatPerBarTextBox.Text, out _) &&
                                  double.TryParse(TempoTextBox.Text, out _) &&
                                  this.BeatUnitComboBox.SelectedItem != null;

        private void Submit() {
            if (!IsValid()) {
                MessageBox.Show("One or more fields contains invalid data", "Invalid parameters", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            CommandDispatcher.Inst.Project.BPM = Double.Parse(TempoTextBox.Text);
            CommandDispatcher.Inst.Project.BeatPerBar = int.Parse(BeatPerBarTextBox.Text);
            CommandDispatcher.Inst.Project.BeatUnit = (int)BeatUnitComboBox.SelectedItem;
            CommandDispatcher.Inst.ExecuteCmd(new RecalculateNotesNotification
                {project = CommandDispatcher.Inst.Project});
            Hide();
        }

        private void OkButton_OnClick(object sender, EventArgs e) => Submit();

        private void CancelButton_OnClick(object sender, RoutedEventArgs e) => Hide();

        private void TapTempoButton_OnClick(object sender, RoutedEventArgs e) {
            TimeSpan dt = DateTime.Now - LastTapTime;
            double tempo = Math.Truncate(100 / dt.TotalMinutes) / 100;
            if (MusicMath.MinTempo < tempo && tempo < MusicMath.MaxTempo)
                TempoTextBox.Text = Convert.ToString(tempo, CultureInfo.InvariantCulture);
            else if (tempo < MusicMath.MinTempo)
                TempoTextBox.Text = Convert.ToString(MusicMath.MinTempo);
            else TempoTextBox.Text = Convert.ToString(MusicMath.MaxTempo);
            LastTapTime = DateTime.Now;
        }

        private void EnterKeyPressConfirm(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter)
                Submit();
        }
    }
}
