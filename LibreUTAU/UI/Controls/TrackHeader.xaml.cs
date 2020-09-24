using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using LibreUtau.Core;
using LibreUtau.Core.Commands;
using LibreUtau.Core.Formats;
using LibreUtau.Core.USTx;

namespace LibreUtau.UI.Controls {
    /// <summary>
    ///     Interaction logic for TrackHeader.xaml
    /// </summary>
    public partial class TrackHeader : UserControl {
        UTrack _track;

        ContextMenu changeSingerMenu;

        long clickTimeMs;

        ContextMenu headerMenu;

        public TrackHeader() {
            InitializeComponent();

            panSlider.Minimum = MusicMath.minPan;
            panSlider.Maximum = MusicMath.maxPan;
        }

        public UTrack Track {
            set {
                _track = value;
                this.DataContext = value;
            }
            get { return _track; }
        }

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        public void setCursorPos(Point point) {
            SetCursorPos((int)(PointToScreen(point).X), (int)(PointToScreen(point).Y));
        }

        private void faderSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            var slider = sender as Slider;
            int thumbWidth = slider == this.faderSlider ? 33 : 11;
            if (e.ChangedButton == MouseButton.Right || (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)) {
                slider.Value = 0;
            } else if (e.ChangedButton == MouseButton.Left) {
                double x = (slider.Value - slider.Minimum) / (slider.Maximum - slider.Minimum) *
                    (slider.ActualWidth - thumbWidth) + (thumbWidth - 1) / 2;
                double y = e.GetPosition(slider).Y;
                setCursorPos(slider.TransformToAncestor(this).Transform(new Point(x, y)));
                clickTimeMs = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                slider.CaptureMouse();
            }

            e.Handled = true;
        }

        private void faderSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            var slider = sender as Slider;
            slider.ReleaseMouseCapture();
            e.Handled = true;
        }

        private void faderSlider_PreviewMouseMove(object sender, MouseEventArgs e) {
            var slider = sender as Slider;
            int thumbWidth = slider == this.faderSlider ? 33 : 11;
            if (slider.IsMouseCaptured && clickTimeMs + 100 < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)
                slider.Value = slider.Minimum + (e.GetPosition(slider).X - (thumbWidth - 1) / 2) /
                    (slider.ActualWidth - thumbWidth) * (slider.Maximum - slider.Minimum);
        }

        private void faderSlider_MouseWheel(object sender, MouseWheelEventArgs e) {
            var slider = sender as Slider;
            slider.Value += e.Delta / 120 * (slider.Maximum - slider.Minimum) / 50;
        }

        private void buildChangeSingerMenuItems() {
            changeSingerMenu.Items.Clear();
            foreach (var pair in UtauSoundbank.GetAllSingers()) {
                var menuItem = new MenuItem {Header = pair.Value.Name};
                menuItem.Click += (_o, _e) => {
                    if (this.Track.Singer != pair.Value) {
                        CommandDispatcher.Inst.StartUndoGroup();
                        CommandDispatcher.Inst.ExecuteCmd(new TrackChangeSingerCommand(CommandDispatcher.Inst.Project,
                            this.Track,
                            pair.Value));
                        CommandDispatcher.Inst.EndUndoGroup();
                    }
                };
                changeSingerMenu.Items.Add(menuItem);
            }
        }

        private void singerNameButton_Click(object sender, RoutedEventArgs e) {
            if (changeSingerMenu == null) {
                changeSingerMenu = new ContextMenu();
                changeSingerMenu.Placement = PlacementMode.Bottom;
                changeSingerMenu.PlacementTarget = (Button)sender;
                changeSingerMenu.HorizontalOffset = -10;
            }

            if (UtauSoundbank.GetAllSingers().Count != 0) {
                buildChangeSingerMenuItems();
                changeSingerMenu.IsOpen = true;
            }

            e.Handled = true;
        }

        public void UpdateSingerName() {
            this.singerNameButton.GetBindingExpression(ContentProperty).UpdateTarget();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e) {
            if (e.RightButton == MouseButtonState.Pressed) {
                if (headerMenu == null) {
                    headerMenu = new ContextMenu();
                    var item = new MenuItem {Header = "Remove track"};
                    item.Click += (_o, _e) => {
                        CommandDispatcher.Inst.StartUndoGroup();
                        CommandDispatcher.Inst.ExecuteCmd(new RemoveTrackCommand(CommandDispatcher.Inst.Project,
                            this.Track));
                        CommandDispatcher.Inst.EndUndoGroup();
                    };
                    headerMenu.Items.Add(item);
                }

                headerMenu.IsOpen = true;
            }

            e.Handled = true;
        }

        private void faderSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            CommandDispatcher.Inst.ExecuteCmd(new VolumeChangeNotification(this.Track.TrackNo, ((Slider)sender).Value));
        }

        private void panSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            CommandDispatcher.Inst.ExecuteCmd(new PanChangeNotification(this.Track.TrackNo, ((Slider)sender).Value));
        }
    }
}
