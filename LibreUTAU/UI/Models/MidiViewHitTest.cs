using System;
using System.Windows;
using LibreUtau.Core;
using LibreUtau.Core.Commands;
using LibreUtau.Core.USTx;

namespace LibreUtau.UI.Models {
    public class PitchPointHitTestResult {
        public int Index;
        public UNote Note;
        public bool OnPoint;
        public double X;
        public double Y;
    }

    class MidiViewHitTest : ICmdSubscriber {
        readonly MidiViewModel midiVM;

        public MidiViewHitTest(MidiViewModel midiVM) { this.midiVM = midiVM; }
        UProject Project { get { return CommandDispatcher.Inst.Project; } }

        public UNote HitTestNoteX(double x) {
            int tick = (int)(midiVM.CanvasToQuarter(x) * Project.Resolution);
            foreach (UNote note in midiVM.Part.Notes)
                if (note.PosTick <= tick && note.EndTick >= tick)
                    return note;
            return null;
        }

        public UNote HitTestNote(Point mousePos) {
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.Resolution);
            int noteNum = midiVM.CanvasToNoteNum(mousePos.Y);
            foreach (UNote note in midiVM.Part.Notes)
                if (note.PosTick <= tick && note.EndTick >= tick && note.NoteNum == noteNum)
                    return note;
            return null;
        }

        public bool HitNoteResizeArea(UNote note, Point mousePos) {
            double x = midiVM.QuarterToCanvas((double)note.EndTick / Project.Resolution);
            return mousePos.X <= x && mousePos.X > x - UIConstants.ResizeMargin;
        }

        public UNote HitTestVibrato(Point mousePos) {
            int tick = (int)(midiVM.CanvasToQuarter(mousePos.X) * Project.Resolution);
            double pitch = midiVM.CanvasToPitch(mousePos.Y);
            foreach (UNote note in midiVM.Part.Notes)
                if (note.PosTick + note.DurTick * (1 - note.Vibrato.Length / 100) <= tick && note.EndTick >= tick &&
                    Math.Abs(note.NoteNum - pitch) < note.Vibrato.Depth / 100)
                    return note;
            return null;
        }

        public PitchPointHitTestResult HitTestPitchPoint(Point mousePos) {
            foreach (var note in midiVM.Part.Notes) {
                if (midiVM.NoteIsInView(note)) // FIXME this is not enough
                {
                    if (note.Error) continue;
                    Point last = new Point(0, 0);
                    PitchPointShape lastShape = PitchPointShape.LINEAR;
                    for (int i = 0; i < note.PitchBend.Points.Count; i++) {
                        var pit = note.PitchBend.Points[i];
                        int posTick = note.PosTick + Project.MillisecondToTick(pit.X);
                        double noteNum = note.NoteNum + pit.Y / 10;
                        double x = midiVM.TickToCanvas(posTick);
                        double y = midiVM.NoteNumToCanvas(noteNum) + midiVM.TrackHeight / 2;
                        if (Math.Abs(mousePos.X - x) < 4 && Math.Abs(mousePos.Y - y) < 4)
                            return new PitchPointHitTestResult {Note = note, Index = i, OnPoint = true};
                        if (mousePos.X < x && i > 0 && mousePos.X > last.X) {
                            // Hit test curve
                            var lastPit = note.PitchBend.Points[i - 1];
                            double castY = MusicMath.InterpolateShape(last, new Point(x, y), mousePos.X, lastShape) -
                                           mousePos.Y;
                            if (y >= last.Y) {
                                if (mousePos.Y - y > 3 || last.Y - mousePos.Y > 3) break;
                            } else {
                                if (y - mousePos.Y > 3 || mousePos.Y - last.Y > 3) break;
                            }

                            double castX = MusicMath.InterpolateShapeX(last, new Point(x, y), mousePos.Y, lastShape) -
                                           mousePos.X;
                            double dis = double.IsNaN(castX)
                                ? Math.Abs(castY)
                                : Math.Cos(Math.Atan2(Math.Abs(castY), Math.Abs(castX))) * Math.Abs(castY);
                            if (dis < 3) {
                                double msX = CommandDispatcher.Inst.Project.TickToMillisecond(
                                    midiVM.CanvasToQuarter(mousePos.X) * CommandDispatcher.Inst.Project.Resolution -
                                    note.PosTick);
                                double msY = (midiVM.CanvasToPitch(mousePos.Y) - note.NoteNum) * 10;
                                return (new PitchPointHitTestResult
                                    {Note = note, Index = i - 1, OnPoint = false, X = msX, Y = msY});
                            }

                            break;
                        }

                        last = new Point(x, y);
                        lastShape = pit.Shape;
                    }
                }
            }

            return null;
        }

        # region ICmdSubscriber

        public void SubscribeTo(ICmdPublisher publisher) {
            if (publisher != null) publisher.Subscribe(this);
        }

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            if (cmd is RedrawNotesNotification) { }
        }

        # endregion
    }
}
