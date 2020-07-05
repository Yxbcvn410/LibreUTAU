using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using LibreUtau.Core;
using LibreUtau.Core.Commands;
using LibreUtau.Core.USTx;
using LibreUtau.UI.Models;

namespace LibreUtau.UI.Controls {
    class NotesElement : ExpElement {
        double _quarterWidth;

        bool _showPitch = true;

        double _trackHeight;
        protected Dictionary<string, double> fTextHeights = new Dictionary<string, double>();

        protected Dictionary<string, FormattedText> fTextPool = new Dictionary<string, FormattedText>();
        protected Dictionary<string, double> fTextWidths = new Dictionary<string, double>();

        public MidiViewModel midiVM;

        protected Pen penPit;

        public NotesElement() {
            penPit = new Pen(ThemeManager.WhiteKeyNameBrushNormal, 1);
            penPit.Freeze();
            this.IsHitTestVisible = false;
        }

        public new double X {
            set {
                if (tTrans.X != Math.Round(value)) {
                    tTrans.X = Math.Round(value);
                    MarkUpdate();
                }
            }
            get { return tTrans.X; }
        }

        public double Y {
            set {
                if (tTrans.Y != Math.Round(value)) { tTrans.Y = Math.Round(value); }
            }
            get { return tTrans.Y; }
        }

        public double TrackHeight {
            set {
                if (_trackHeight != value) {
                    _trackHeight = value;
                    MarkUpdate();
                }
            }
            get { return _trackHeight; }
        }

        public double QuarterWidth {
            set {
                if (_quarterWidth != value) {
                    _quarterWidth = value;
                    MarkUpdate();
                }
            }
            get { return _quarterWidth; }
        }

        public bool ShowPitch {
            set {
                if (_showPitch != value) {
                    _showPitch = value;
                    MarkUpdate();
                }
            }
            get { return _showPitch; }
        }

        public override UVoicePart Part {
            set {
                _part = value;
                ClearFormattedTextPool();
                MarkUpdate();
            }
            get { return _part; }
        }

        public override void RedrawIfUpdated() {
            if (!_updated) return;
            DrawingContext cxt = visual.RenderOpen();
            if (Part != null) {
                bool inView, lastInView = false;
                UNote lastNote = null;
                foreach (var note in Part.Notes) {
                    inView = midiVM.NoteIsInView(note);

                    if (inView && !lastInView)
                        if (lastNote != null)
                            DrawNote(lastNote, cxt);

                    if (inView || !inView && lastInView)
                        DrawNote(note, cxt);

                    lastNote = note;
                    lastInView = inView;
                }
            }

            cxt.Close();
            _updated = false;
        }

        private void ClearFormattedTextPool() {
            fTextPool.Clear();
            fTextWidths.Clear();
            fTextHeights.Clear();
        }

        private void DrawNote(UNote note, DrawingContext cxt) {
            DrawNoteBody(note, cxt);
            if (!note.Error) {
                if (ShowPitch) DrawPitchBend(note, cxt);
                if (ShowPitch) DrawVibrato(note, cxt);
            }
        }

        private void DrawNoteBody(UNote note, DrawingContext cxt) {
            double left = note.PosTick * midiVM.QuarterWidth / CommandDispatcher.Inst.Project.Resolution + 1;
            double top = midiVM.TrackHeight * ((double)UIConstants.MaxNoteNum - 1 - note.NoteNum) + 1;
            double width = Math.Max(2,
                note.DurTick * midiVM.QuarterWidth / CommandDispatcher.Inst.Project.Resolution - 1);
            double height = Math.Max(2, midiVM.TrackHeight - 2);
            cxt.DrawRoundedRectangle(
                note.Error ? midiVM.SelectedNotes.Contains(note) ? ThemeManager.NoteFillSelectedErrorBrush :
                ThemeManager.NoteFillErrorBrushes[0] :
                midiVM.SelectedNotes.Contains(note) ? ThemeManager.NoteFillSelectedBrush :
                ThemeManager.NoteFillBrushes[0],
                null, new Rect(new Point(left, top), new Size(width, height)), 2, 2);
            if (height >= 10) {
                if (note.Lyric.Length == 0) return;
                string displayLyric = note.Lyric;

                if (!fTextPool.ContainsKey(displayLyric)) AddToFormattedTextPool(displayLyric);
                var fText = fTextPool[displayLyric];

                if (fTextWidths[displayLyric] + 5 > width) {
                    displayLyric = note.Lyric[0] + "..";
                    if (!fTextPool.ContainsKey(displayLyric)) AddToFormattedTextPool(displayLyric);
                    fText = fTextPool[displayLyric];
                    if (fTextWidths[displayLyric] + 5 > width) return;
                }

                cxt.DrawText(fText,
                    new Point((int)left + 5, Math.Round(top + (height - fTextHeights[displayLyric]) / 2)));
            }
        }

        protected virtual void AddToFormattedTextPool(string text) {
            var fText = new FormattedText(
                text,
                Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                12,
                Brushes.White);
            fTextPool.Add(text, fText);
            fTextWidths.Add(text, fText.Width);
            fTextHeights.Add(text, fText.Height);
        }

        private void DrawVibrato(UNote note, DrawingContext cxt) {
            if (note.Vibrato == null) return;
            var vibrato = note.Vibrato;
            double periodPix = CommandDispatcher.Inst.Project.MillisecondToTick(vibrato.Period) * midiVM.QuarterWidth /
                               CommandDispatcher.Inst.Project.Resolution;
            double lengthPix = note.DurTick * vibrato.Length / 100 * midiVM.QuarterWidth /
                               CommandDispatcher.Inst.Project.Resolution;

            double startX = (note.PosTick + note.DurTick * (1 - vibrato.Length / 100)) * midiVM.QuarterWidth /
                            CommandDispatcher.Inst.Project.Resolution;
            double startY = TrackHeight * (UIConstants.MaxNoteNum - 1.0 - note.NoteNum) + TrackHeight / 2;
            double inPix = lengthPix * vibrato.In / 100;
            double outPix = lengthPix * vibrato.Out / 100;
            double depthPix = vibrato.Depth / 100 * midiVM.TrackHeight;

            double _x0 = 0, _y0 = 0, _x1 = 0, _y1 = 0;
            while (_x1 < lengthPix) {
                cxt.DrawLine(penPit, new Point(startX + _x0, startY + _y0), new Point(startX + _x1, startY + _y1));
                _x0 = _x1;
                _y0 = _y1;
                _x1 += Math.Min(2, periodPix / 8);
                _y1 = -Math.Sin(2 * Math.PI * (_x1 / periodPix + vibrato.Shift / 100)) * depthPix;
                if (_x1 < inPix) _y1 = _y1 * _x1 / inPix;
                else if (_x1 > lengthPix - outPix) _y1 = _y1 * (lengthPix - _x1) / outPix;
            }
        }

        private void DrawPitchBend(UNote note, DrawingContext cxt) {
            var _pts = note.PitchBend.Data as List<PitchPoint>;
            if (_pts.Count < 2) return;

            Point GetPitchPointLocation(PitchPoint _pt) {
                double tick = note.PosTick + MusicMath.MillisecondToTick(_pt.X, CommandDispatcher.Inst.Project.BPM,
                    CommandDispatcher.Inst.Project.BeatUnit, CommandDispatcher.Inst.Project.Resolution);
                double pitch = note.NoteNum + _pt.Y / 10.0;

                double _X = midiVM.QuarterWidth * tick / CommandDispatcher.Inst.Project.Resolution;
                double _Y = TrackHeight * (UIConstants.MaxNoteNum - 1.0 - pitch) + TrackHeight / 2;
                return new Point(_X, _Y);
            }

            Point pt0 = GetPitchPointLocation(_pts[0]);

            if (note.PitchBend.SnapFirst)
                cxt.DrawEllipse(ThemeManager.WhiteKeyNameBrushNormal, penPit, pt0, 2.5, 2.5);
            else cxt.DrawEllipse(null, penPit, pt0, 2.5, 2.5);

            for (int i = 1; i < _pts.Count; i++) {
                Point pt1 = GetPitchPointLocation(_pts[i]);

                // Draw arc
                if (pt1.X - pt0.X < 5) {
                    cxt.DrawLine(penPit, pt0, pt1);
                } else {
                    Point _pt0 = pt0;
                    while (_pt0.X < pt1.X) {
                        Point _pt1 = new Point();
                        _pt1.X = Math.Min(_pt0.X + 4, pt1.X);
                        _pt1.Y = MusicMath.InterpolateShape(pt0, pt1, _pt1.X, _pts[i - 1].Shape);
                        cxt.DrawLine(penPit, _pt0, _pt1);
                        _pt0 = _pt1;
                    }
                }

                cxt.DrawEllipse(null, penPit, pt1, 2.5, 2.5);

                pt0 = pt1;
            }
        }
    }
}
