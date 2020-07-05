using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using LibreUtau.Core.Commands;
using LibreUtau.Core.USTx;
using LibreUtau.UI.Controls;
using LibreUtau.UI.Models;
using WinInterop = System.Windows.Interop;

namespace LibreUtau.UI {
    /// <summary>
    ///     Interaction logic for BorderlessWindow.xaml
    /// </summary>
    public partial class MidiWindow {
        readonly MidiViewHitTest midiHT;
        readonly MidiViewModel midiVM;
        readonly ContextMenu pitchCxtMenu;

        readonly PitchPointHitTestResultContainer pitHitContainer = new PitchPointHitTestResultContainer();
        private TimeSpan lastFrame = TimeSpan.Zero;

        public MidiWindow() {
            InitializeComponent();

            void WindowHide() {
                EndNoteEditing(true);
                midiVM.DeselectAll();
                Hide();
            }

            this.CloseButtonClicked += (o, e) => WindowHide();
            this.Deactivated += (o, e) => WindowHide();
            CompositionTargetEx.FrameUpdating += RenderLoop;

            viewScaler.Max = UIConstants.NoteMaxHeight;
            viewScaler.Min = UIConstants.NoteMinHeight;
            viewScaler.Value = UIConstants.NoteDefaultHeight;
            viewScaler.ViewScaled += viewScaler_ViewScaled;

            midiVM = (MidiViewModel)this.Resources["midiVM"];
            midiVM.TimelineCanvas = this.timelineCanvas;
            midiVM.MidiCanvas = this.notesCanvas;
            midiVM.PhonemeCanvas = this.phonemeCanvas;
            midiVM.ExpCanvas = this.expCanvas;
            midiVM.SubscribeTo(CommandDispatcher.Inst);

            midiHT = new MidiViewHitTest(midiVM);

            pitchCxtMenu = (ContextMenu)this.Resources["pitchCxtMenu"];

            List<ExpComboBoxViewModel> comboVMs = new List<ExpComboBoxViewModel> {
                new ExpComboBoxViewModel {Index = 0},
                new ExpComboBoxViewModel {Index = 1},
                new ExpComboBoxViewModel {Index = 2},
                new ExpComboBoxViewModel {Index = 3}
            };

            comboVMs[0].CreateBindings(expCombo0);
            comboVMs[1].CreateBindings(expCombo1);
            comboVMs[2].CreateBindings(expCombo2);
            comboVMs[3].CreateBindings(expCombo3);
        }

        void viewScaler_ViewScaled(object sender, EventArgs e) {
            double zoomCenter = (midiVM.OffsetY + midiVM.ViewHeight / 2) / midiVM.TrackHeight;
            midiVM.TrackHeight = ((ViewScaledEventArgs)e).Value;
            midiVM.OffsetY = midiVM.TrackHeight * zoomCenter - midiVM.ViewHeight / 2;
            midiVM.MarkUpdate();
        }

        void RenderLoop(object sender, EventArgs e) {
            if (midiVM.Part == null || midiVM.Project == null) return;

            TimeSpan nextFrame = ((RenderingEventArgs)e).RenderingTime;
            double deltaTime = (nextFrame - lastFrame).TotalMilliseconds;
            lastFrame = nextFrame;

            DragScroll(deltaTime);
            keyboardBackground.RenderIfUpdated();
            tickBackground.RenderIfUpdated();
            timelineBackground.RenderIfUpdated();
            keyTrackBackground.RenderIfUpdated();
            ExpTickBackground.RenderIfUpdated();
            midiVM.RedrawIfUpdated();
        }

        public void DragScroll(double deltaTime) {
            if (Mouse.Captured == this.notesCanvas && Mouse.LeftButton == MouseButtonState.Pressed) {
                const double scrollSpeed = 0.015;
                Point mousePos = Mouse.GetPosition(notesCanvas);
                bool needsUpdate = false;
                double delta = scrollSpeed * deltaTime;
                if (mousePos.X < 0) {
                    this.horizontalScroll.Value -= this.horizontalScroll.SmallChange * delta;
                    needsUpdate = true;
                } else if (mousePos.X > notesCanvas.ActualWidth) {
                    this.horizontalScroll.Value += this.horizontalScroll.SmallChange * delta;
                    needsUpdate = true;
                }

                if (mousePos.Y < 0 && Mouse.Captured == this.notesCanvas) {
                    this.verticalScroll.Value -= this.verticalScroll.SmallChange * delta;
                    needsUpdate = true;
                } else if (mousePos.Y > notesCanvas.ActualHeight && Mouse.Captured == this.notesCanvas) {
                    this.verticalScroll.Value += this.verticalScroll.SmallChange * delta;
                    needsUpdate = true;
                }

                if (needsUpdate) {
                    notesCanvas_MouseMove_Helper(mousePos);
                    if (Mouse.Captured == this.timelineCanvas) timelineCanvas_MouseMove_Helper(mousePos);
                }
            } else if (Mouse.Captured == timelineCanvas && Mouse.LeftButton == MouseButtonState.Pressed) {
                Point mousePos = Mouse.GetPosition(timelineCanvas);
                timelineCanvas_MouseMove_Helper(mousePos);
            }
        }

        #region Navigate Drag

        private void navigateDrag_NavDrag(object sender, EventArgs e) {
            midiVM.OffsetX += ((NavDragEventArgs)e).X * midiVM.SmallChangeX;
            midiVM.OffsetY += ((NavDragEventArgs)e).Y * midiVM.SmallChangeY * 0.5;
            midiVM.MarkUpdate();
        }

        #endregion

        protected override void OnKeyDown(KeyEventArgs e) {
            if (LyricBox.IsFocused) {
            } else {
                Window_KeyDown(this, e);
                e.Handled = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.F4) {
                this.Hide();
            } else if (midiVM.Part == null) {
            } else if (Keyboard.Modifiers == ModifierKeys.Control) // Ctrl
            {
                if (e.Key == Key.A) {
                    midiVM.SelectAll();
                } else if (e.Key == Key.Z) {
                    midiVM.DeselectAll();
                    CommandDispatcher.Inst.Undo();
                } else if (e.Key == Key.Y) {
                    midiVM.DeselectAll();
                    CommandDispatcher.Inst.Redo();
                }
            } else if (Keyboard.Modifiers == 0) // No modifiers
            {
                if (e.Key == Key.Delete) {
                    if (midiVM.SelectedNotes.Count > 0) {
                        CommandDispatcher.Inst.StartUndoGroup();
                        CommandDispatcher.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part,
                            midiVM.SelectedNotes.GetNotes()));
                        CommandDispatcher.Inst.EndUndoGroup();
                    }
                } else if (CurrentState != EditorState.EDIT_LYRIC)
                    switch (e.Key) {
                        case Key.I:
                            midiVM.ShowPitch = !midiVM.ShowPitch;
                            break;
                        case Key.O:
                            midiVM.ShowPhoneme = !midiVM.ShowPhoneme;
                            break;
                        case Key.P:
                            midiVM.Snap = !midiVM.Snap;
                            break;
                    }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            e.Cancel = true;
            this.Hide();
        }

        private void expCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            ((Canvas)sender).CaptureMouse();
            CommandDispatcher.Inst.StartUndoGroup();
            Point mousePos = e.GetPosition((UIElement)sender);
            expCanvas_SetExpHelper(mousePos);
        }

        private void expCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            CommandDispatcher.Inst.EndUndoGroup();
            ((Canvas)sender).ReleaseMouseCapture();
        }

        private void expCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (Mouse.LeftButton == MouseButtonState.Pressed) {
                Point mousePos = e.GetPosition((UIElement)sender);
                expCanvas_SetExpHelper(mousePos);
            }
        }

        private void expCanvas_SetExpHelper(Point mousePos) {
            if (midiVM.Part == null) return;
            int newValue;
            string _key = midiVM.visibleExpElement.Key;

            var _expTemplate = CommandDispatcher.Inst.Project.ExpressionTable[_key] as IntExpression;
            if (Keyboard.Modifiers == ModifierKeys.Alt) newValue = (int)_expTemplate.Data;
            else
                newValue = (int)Math.Max(_expTemplate.Min,
                    Math.Min(_expTemplate.Max,
                        (1 - mousePos.Y / expCanvas.ActualHeight) * (_expTemplate.Max - _expTemplate.Min) +
                        _expTemplate.Min));

            UNote note = midiHT.HitTestNoteX(mousePos.X);
            if (midiVM.SelectedNotes.Count == 0 || midiVM.SelectedNotes.Contains(note))
                if (note != null)
                    CommandDispatcher.Inst.ExecuteCmd(new SetIntExpCommand(midiVM.Part, note,
                        midiVM.visibleExpElement.Key,
                        newValue));
        }

        private void mainButton_Click(object sender, RoutedEventArgs e) {
            CommandDispatcher.Inst.ExecuteCmd(new ShowPitchExpNotification());
        }

        class PitchPointHitTestResultContainer {
            public PitchPointHitTestResult Result;
        }

        #region Context menu

        void PitchCxtMenuItem_SetIn_Click(object o, RoutedEventArgs e) {
            PitchPointSetShape(PitchPointShape.SINE_IN);
        }

        void PitchCxtMenuItem_SetOut_Click(object o, RoutedEventArgs e) {
            PitchPointSetShape(PitchPointShape.SINE_OUT);
        }

        void PitchCxtMenuItem_SetInOut_Click(object o, RoutedEventArgs e) {
            PitchPointSetShape(PitchPointShape.SINE_IN_OUT);
        }

        void PitchCxtMenuItem_SetLinear_Click(object o, RoutedEventArgs e) {
            PitchPointSetShape(PitchPointShape.LINEAR);
        }

        void PitchCxtMenuItem_AddPoint_Click(object o, RoutedEventArgs e) {
            var pitHit = pitHitContainer.Result;
            CommandDispatcher.Inst.StartUndoGroup();
            CommandDispatcher.Inst.ExecuteCmd(new AddPitchPointCommand(pitHit.Note, new PitchPoint(pitHit.X, pitHit.Y),
                pitHit.Index + 1));
            CommandDispatcher.Inst.EndUndoGroup();
        }

        void PitchCxtMenuItem_DeletePoint_Click(object o, RoutedEventArgs e) {
            var pitHit = pitHitContainer.Result;
            CommandDispatcher.Inst.StartUndoGroup();
            CommandDispatcher.Inst.ExecuteCmd(new DeletePitchPointCommand(midiVM.Part, pitHit.Note, pitHit.Index));
            CommandDispatcher.Inst.EndUndoGroup();
        }

        void PitchCxtMenuItem_SnapPoint_Click(object o, RoutedEventArgs e) {
            var pitHit = pitHitContainer.Result;
            CommandDispatcher.Inst.StartUndoGroup();
            CommandDispatcher.Inst.ExecuteCmd(new SnapPitchPointCommand(pitHit.Note));
            CommandDispatcher.Inst.EndUndoGroup();
        }

        private void PitchPointSetShape(PitchPointShape shape) {
            var pitHit = pitHitContainer.Result;
            CommandDispatcher.Inst.StartUndoGroup();
            CommandDispatcher.Inst.ExecuteCmd(
                new ChangePitchPointShapeCommand(pitHit.Note.PitchBend.Points[pitHit.Index], shape));
            CommandDispatcher.Inst.EndUndoGroup();
        }

        private void PitchCxtMenuItem_AddPoint_Update(object o, RoutedEventArgs e) {
            var pitHit = pitHitContainer.Result;
            ((MenuItem)o).Visibility = pitHit.OnPoint ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PitchCxtMenuItem_DelPoint_Update(object o, RoutedEventArgs e) {
            var pitHit = pitHitContainer.Result;
            ((MenuItem)o).Visibility =
                pitHit.OnPoint && pitHit.Index != 0 && pitHit.Index != pitHit.Note.PitchBend.Points.Count - 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void PitchCxtMenuItem_SnapPoint_Update(object o, RoutedEventArgs e) {
            var pitHit = pitHitContainer.Result;
            if (pitHit.OnPoint)
                ((MenuItem)o).Header = pitHit.Note.PitchBend.SnapFirst
                    ? (string)FindResource("contextmenu.unsnappoint")
                    : (string)FindResource("contextmenu.snappoint");
            ((MenuItem)o).Visibility = pitHit.OnPoint && pitHit.Index == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        #endregion

        #region Lyric editor

        private void LyricBox_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Enter:
                    EndNoteEditing(false);
                    break;
                case Key.Escape:
                    EndNoteEditing(true);
                    break;
            }
        }

        private void LyricBox_KeyUp(object sender, KeyEventArgs e) {
            if (e.Key == Key.Down && LyricBox.Visibility == Visibility.Visible) {
                LyricVariants.Focus();
                LyricVariants.SelectedIndex = 0;
            }
        }

        private void LyricBox_TextChanged(object sender, TextChangedEventArgs e) {
            if (LyricBox.IsKeyboardFocused) UpdateLyricHints();
        }

        private void UpdateLyricHints() {
            var singer = CommandDispatcher.Inst.Project.Tracks[midiVM.Part.TrackNo].Singer;
            if (singer?.AliasMap != null) {
                List<string> autocomplete = new List<string>((from phoneme in singer.AliasMap.Keys
                    where phoneme.StartsWith(LyricBox.Text)
                    select phoneme));
                autocomplete.Sort();
                LyricVariants.ItemsSource = autocomplete;
                LyricVariants.Visibility = Visibility.Visible;
            } else LyricVariants.Visibility = Visibility.Hidden;
        }

        private void LyricVariants_OnSelected(object sender, RoutedEventArgs e) {
            LyricBox.Text = LyricVariants.SelectedItem as string ?? String.Empty;
        }

        private void LyricVariants_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key != Key.Enter && e.Key != Key.Escape)
                return;
            UpdateLyricHints();
            LyricBox_KeyDown(sender, e);
        }

        private void LyricVariants_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            UpdateLyricHints();
            EndNoteEditing(false);
        }

        private void EndNoteEditing(bool cancel) {
            if (!string.IsNullOrEmpty(LyricBox.Text.Trim()) && !cancel) {
                CommandDispatcher.Inst.StartUndoGroup();
                CommandDispatcher.Inst.ExecuteCmd(new ChangeNoteLyricCommand(midiVM.Part, midiVM.GetModifiedNote(),
                    LyricBox.Text));
                CommandDispatcher.Inst.EndUndoGroup();
            }

            LyricBox.Text = string.Empty;
            LyricEnterGrid.Visibility = Visibility.Hidden;
            midiVM.SetModifiedNote(null);

            CurrentState = EditorState.IDLE;
        }

        #endregion

        # region Note Canvas

        Rectangle selectionBox;
        Point? selectionStart;
        int _lastNoteLength = 480;

        private enum EditorState {
            IDLE,
            MOVE_NOTE,
            MOVE_PITCHPOINT,
            RESIZE_NOTE,
            EDIT_LYRIC
        }

        private EditorState CurrentState = EditorState.IDLE;
        UNote _noteHit;
        PitchPoint _pitHit;
        int _pitHitIndex;
        int _tickMoveRelative;
        int _tickMoveStart;

        private void notesCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            if (CurrentState == EditorState.EDIT_LYRIC) {
                EndNoteEditing(false);
                CurrentState = EditorState.IDLE;
                return;
            }

            Point mousePos = e.GetPosition((Canvas)sender);

            var hit = VisualTreeHelper.HitTest(notesCanvas, mousePos).VisualHit;
            Debug.WriteLine("Mouse hit " + hit);

            var pitchPointHit = midiHT.HitTestPitchPoint(mousePos);
            var noteHit = midiHT.HitTestNote(mousePos);

            if (pitchPointHit != null) {
                // Start moving pitchpoint
                if (pitchPointHit.OnPoint) {
                    CurrentState = EditorState.MOVE_PITCHPOINT;
                    _pitHit = pitchPointHit.Note.PitchBend.Points[pitchPointHit.Index];
                    _pitHitIndex = pitchPointHit.Index;
                    _noteHit = pitchPointHit.Note;
                    CommandDispatcher.Inst.StartUndoGroup();
                }
            } else if (Keyboard.Modifiers == ModifierKeys.Control ||
                       Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift)) {
                if (noteHit != null) {
                    // Toggle note selection
                    midiVM.ToggleSelection(noteHit);
                } else {
                    // Start selection
                    selectionStart = new Point(midiVM.CanvasToQuarter(mousePos.X), midiVM.CanvasToNoteNum(mousePos.Y));

                    if (Keyboard.IsKeyUp(Key.LeftShift) && Keyboard.IsKeyUp(Key.RightShift)) midiVM.DeselectAll();

                    if (selectionBox == null) {
                        selectionBox = new Rectangle {
                            Stroke = Brushes.Black,
                            StrokeThickness = 2,
                            Fill = ThemeManager.BarNumberBrush,
                            Opacity = 0.5,
                            RadiusX = 8,
                            RadiusY = 8,
                            IsHitTestVisible = false
                        };
                        notesCanvas.Children.Add(selectionBox);
                    }

                    selectionBox.Width = 0;
                    selectionBox.Height = 0;
                    Panel.SetZIndex(selectionBox, 1000);
                    selectionBox.Visibility = Visibility.Visible;

                    Mouse.OverrideCursor = Cursors.Cross;
                }
            } else if (noteHit != null) {
                _noteHit = noteHit;
                if (!midiVM.SelectedNotes.Contains(noteHit)) midiVM.SetModifiedNote(noteHit);

                if (e.ClickCount == 2) {
                    // Edit note lyric
                    midiVM.DeselectAll();
                    midiVM.SetModifiedNote(noteHit);
                    LyricEnterGrid.Visibility = Visibility.Visible;
                    CurrentState = EditorState.EDIT_LYRIC;
                    LyricBox.Text = midiVM.GetModifiedNote().Lyric;
                    // TODO: clear undo from last note edited
                    LyricBox.Focus();
                    LyricBox.SelectAll();
                    UpdateLyricHints();
                } else if (midiHT.HitNoteResizeArea(noteHit, mousePos)) {
                    // Resize note
                    CurrentState = EditorState.RESIZE_NOTE;
                    Mouse.OverrideCursor = Cursors.SizeWE;
                    CommandDispatcher.Inst.StartUndoGroup();
                } else {
                    // Move note
                    CurrentState = EditorState.MOVE_NOTE;
                    _tickMoveRelative = midiVM.CanvasToSnappedTick(mousePos.X) - noteHit.PosTick;
                    _tickMoveStart = noteHit.PosTick;
                    _lastNoteLength = noteHit.DurTick;
                    CommandDispatcher.Inst.StartUndoGroup();
                }
            } else // Add note
            {
                UNote newNote = CommandDispatcher.Inst.Project.CreateNote(
                    midiVM.CanvasToNoteNum(mousePos.Y),
                    midiVM.CanvasToSnappedTick(mousePos.X),
                    _lastNoteLength);

                CommandDispatcher.Inst.StartUndoGroup();
                CommandDispatcher.Inst.ExecuteCmd(new AddNoteCommand(midiVM.Part, newNote));
                CommandDispatcher.Inst.EndUndoGroup();
                midiVM.MarkUpdate();
                // Enable drag
                midiVM.SetModifiedNote(newNote);
                CurrentState = EditorState.MOVE_NOTE;
                _noteHit = newNote;
                _tickMoveRelative = 0;
                _tickMoveStart = newNote.PosTick;
                CommandDispatcher.Inst.StartUndoGroup();
            }

            ((UIElement)sender).CaptureMouse();
        }

        private void notesCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;

            _noteHit = null;
            _pitHit = null;
            if (CurrentState == EditorState.MOVE_NOTE || CurrentState == EditorState.RESIZE_NOTE ||
                CurrentState == EditorState.MOVE_PITCHPOINT) {
                CurrentState = EditorState.IDLE;
                CommandDispatcher.Inst.EndUndoGroup();
            }

            // End selection
            selectionStart = null;
            if (selectionBox != null) {
                Panel.SetZIndex(selectionBox, -100);
                selectionBox.Visibility = Visibility.Hidden;
            }

            ((Canvas)sender).ReleaseMouseCapture();
            Mouse.OverrideCursor = null;
        }

        private void notesCanvas_MouseMove(object sender, MouseEventArgs e) {
            Point mousePos = e.GetPosition((Canvas)sender);
            notesCanvas_MouseMove_Helper(mousePos);
        }

        private void notesCanvas_MouseMove_Helper(Point mousePos) {
            if (midiVM.Part == null) return;
            if (selectionStart != null) // Selection
            {
                double top =
                    midiVM.NoteNumToCanvas(Math.Max(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y));
                double bottom =
                    midiVM.NoteNumToCanvas(
                        Math.Min(midiVM.CanvasToNoteNum(mousePos.Y), (int)selectionStart.Value.Y) - 1);
                double left = Math.Min(mousePos.X, midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Width = Math.Abs(mousePos.X - midiVM.QuarterToCanvas(selectionStart.Value.X));
                selectionBox.Height = bottom - top;
                Canvas.SetLeft(selectionBox, left);
                Canvas.SetTop(selectionBox, top);
                midiVM.SelectFromBox(selectionStart.Value.X, midiVM.CanvasToQuarter(mousePos.X),
                    (int)selectionStart.Value.Y, midiVM.CanvasToNoteNum(mousePos.Y));
            } else {
                switch (CurrentState) {
                    case EditorState.MOVE_PITCHPOINT:
                        double tickX = midiVM.CanvasToQuarter(mousePos.X) * CommandDispatcher.Inst.Project.Resolution -
                                       _noteHit.PosTick;
                        double deltaX = CommandDispatcher.Inst.Project.TickToMillisecond(tickX) - _pitHit.X;
                        if (_pitHitIndex != 0)
                            deltaX = Math.Max(deltaX, _noteHit.PitchBend.Points[_pitHitIndex - 1].X - _pitHit.X);
                        if (_pitHitIndex != _noteHit.PitchBend.Points.Count - 1)
                            deltaX = Math.Min(deltaX, _noteHit.PitchBend.Points[_pitHitIndex + 1].X - _pitHit.X);
                        double deltaY = Keyboard.Modifiers == ModifierKeys.Shift
                            ? Math.Round(midiVM.CanvasToPitch(mousePos.Y) - _noteHit.NoteNum) * 10 - _pitHit.Y
                            : (midiVM.CanvasToPitch(mousePos.Y) - _noteHit.NoteNum) * 10 - _pitHit.Y;
                        if (_noteHit.PitchBend.Points.First() == _pitHit && _noteHit.PitchBend.SnapFirst ||
                            _noteHit.PitchBend.Points.Last() == _pitHit) deltaY = 0;
                        if (deltaX != 0 || deltaY != 0)
                            CommandDispatcher.Inst.ExecuteCmd(new MovePitchPointCommand(_pitHit, deltaX, deltaY));
                        break;
                    case EditorState.MOVE_NOTE: {
                        int deltaNoteNum = midiVM.CanvasToNoteNum(mousePos.Y) - _noteHit.NoteNum;
                        int deltaPosTick =
                            ((int)(midiVM.Project.Resolution * midiVM.CanvasToSnappedQuarter(mousePos.X)) -
                             _tickMoveRelative) - _noteHit.PosTick;

                        if (deltaNoteNum != 0 || deltaPosTick != 0) {
                            var constraints = midiVM.ManipulatedNotes.GetSelectionConstraints();
                            bool changeNoteNum =
                                deltaNoteNum + constraints.BottomNoteNum >= 0 &&
                                deltaNoteNum + constraints.TopNoteNum < UIConstants.MaxNoteNum;
                            bool changePosTick = deltaPosTick + constraints.EarliestNoteStart >= 0 &&
                                                 deltaPosTick + constraints.LatestNoteEnd <=
                                                 midiVM.QuarterCount * midiVM.Project.Resolution;
                            if (changeNoteNum || changePosTick)

                                CommandDispatcher.Inst.ExecuteCmd(new MoveNoteCommand(midiVM.Part,
                                    midiVM.ManipulatedNotes.GetNotes(),
                                    changePosTick ? deltaPosTick : 0, changeNoteNum ? deltaNoteNum : 0));
                        }
                    }

                        Mouse.OverrideCursor = Cursors.SizeAll;
                        break;

                    case EditorState.RESIZE_NOTE: {
                        int deltaDurTick =
                            (int)(midiVM.CanvasRoundToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution) -
                            _noteHit.EndTick;
                        if (deltaDurTick != 0 &&
                            deltaDurTick + midiVM.ManipulatedNotes.GetSelectionConstraints().ShortestNoteDur >
                            midiVM.GetSnapUnit()) {
                            CommandDispatcher.Inst.ExecuteCmd(new ResizeNoteCommand(midiVM.Part,
                                midiVM.ManipulatedNotes.GetNotes(),
                                deltaDurTick));
                            _lastNoteLength = _noteHit.DurTick;
                        }

                        break;
                    }
                    default:
                        if (Mouse.RightButton == MouseButtonState.Pressed) // Remove Note
                        {
                            UNote noteHit = midiHT.HitTestNote(mousePos);
                            if (noteHit != null)
                                CommandDispatcher.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, noteHit));
                        } else if (Mouse.LeftButton == MouseButtonState.Released &&
                                   Mouse.RightButton == MouseButtonState.Released) {
                            var pitHit = midiHT.HitTestPitchPoint(mousePos);
                            if (pitHit != null) {
                                Mouse.OverrideCursor = Cursors.Hand;
                            } else {
                                UNote noteHit = midiHT.HitTestNote(mousePos);
                                if (noteHit != null && midiHT.HitNoteResizeArea(noteHit, mousePos))
                                    Mouse.OverrideCursor = Cursors.SizeWE;
                                else {
                                    UNote vibHit = midiHT.HitTestVibrato(mousePos);
                                    if (vibHit != null) {
                                        Mouse.OverrideCursor = Cursors.SizeNS;
                                    } else Mouse.OverrideCursor = null;
                                }
                            }
                        }

                        break;
                }
            }
        }

        private void notesCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            if (CurrentState == EditorState.EDIT_LYRIC) {
                EndNoteEditing(false);
                CurrentState = EditorState.IDLE;
                return;
            }

            Point mousePos = e.GetPosition((Canvas)sender);

            var pitchPointHit = midiHT.HitTestPitchPoint(mousePos);
            if (pitchPointHit != null) {
                Mouse.OverrideCursor = null;
                pitHitContainer.Result = pitchPointHit;

                pitchCxtMenu.IsOpen = true;
                pitchCxtMenu.PlacementTarget = this.notesCanvas;
            } else {
                UNote noteHit = midiHT.HitTestNote(mousePos);
                CommandDispatcher.Inst.StartUndoGroup();
                if (noteHit != null && (midiVM.SelectedNotes.Contains(noteHit) || midiVM.SelectedNotes.Count == 0)) {
                    CommandDispatcher.Inst.ExecuteCmd(new RemoveNoteCommand(midiVM.Part, noteHit));
                    CommandDispatcher.Inst.ExecuteCmd(new RedrawNotesNotification());
                } else midiVM.DeselectAll();

                ((UIElement)sender).CaptureMouse();
                Mouse.OverrideCursor = Cursors.No;
            }

            Debug.WriteLine("Total notes: " + midiVM.Part.Notes.Count + " selected: " +
                            midiVM.SelectedNotes.Count);
        }

        private void notesCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            Mouse.OverrideCursor = null;
            ((UIElement)sender).ReleaseMouseCapture();
            CommandDispatcher.Inst.EndUndoGroup();
        }

        private void notesCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (Keyboard.Modifiers == ModifierKeys.Control) {
                timelineCanvas_MouseWheel(sender, e);
            } else if (Keyboard.Modifiers == ModifierKeys.Shift) {
                midiVM.OffsetX -= midiVM.ViewWidth * 0.001 * e.Delta;
            } else if (Keyboard.Modifiers == ModifierKeys.Alt) {
            } else {
                verticalScroll.Value -= verticalScroll.SmallChange * e.Delta / 100;
                verticalScroll.Value = Math.Max(verticalScroll.Minimum,
                    Math.Min(verticalScroll.Maximum, verticalScroll.Value));
            }
        }

        # endregion

        # region Timeline Canvas

        private void timelineCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            const double zoomSpeed = 0.0012;
            Point mousePos = e.GetPosition((UIElement)sender);

            double zoomCenter;
            if (midiVM.OffsetX == 0 && mousePos.X < 128) zoomCenter = 0;
            else zoomCenter = (midiVM.OffsetX + mousePos.X) / midiVM.QuarterWidth;
            midiVM.QuarterWidth *= 1 + e.Delta * zoomSpeed;
            midiVM.OffsetX = Math.Max(0, Math.Min(midiVM.TotalWidth, zoomCenter * midiVM.QuarterWidth - mousePos.X));
        }

        private void timelineCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (midiVM.Part == null) return;
            Point mousePos = e.GetPosition((UIElement)sender);
            int tick = (int)(midiVM.CanvasToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution);
            CommandDispatcher.Inst.ExecuteCmd(new SeekPlayPosTickNotification(Math.Max(0, tick) + midiVM.Part.PosTick));
            ((Canvas)sender).CaptureMouse();
        }

        private void timelineCanvas_MouseMove(object sender, MouseEventArgs e) {
        }

        private void timelineCanvas_MouseMove_Helper(Point mousePos) {
            if (Mouse.LeftButton == MouseButtonState.Pressed && Mouse.Captured == timelineCanvas) {
                int tick = (int)(midiVM.CanvasToSnappedQuarter(mousePos.X) * midiVM.Project.Resolution);
                if (midiVM.playPosTick != tick + midiVM.Part.PosTick)
                    CommandDispatcher.Inst.ExecuteCmd(
                        new SeekPlayPosTickNotification(Math.Max(0, tick) + midiVM.Part.PosTick));
            }
        }

        private void timelineCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            ((Canvas)sender).ReleaseMouseCapture();
        }

        # endregion

        #region Keys Action

        // TODO : keys mouse over, click, release, click and move

        private void keysCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        }

        private void keysCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        }

        private void keysCanvas_MouseMove(object sender, MouseEventArgs e) {
        }

        private void keysCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            //this.notesVerticalScroll.Value = this.notesVerticalScroll.Value - 0.01 * notesVerticalScroll.SmallChange * e.Delta;
            //ncModel.updateGraphics();
        }

        # endregion
    }
}
