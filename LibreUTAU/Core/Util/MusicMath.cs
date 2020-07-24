using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using LibreUtau.Core.USTx;

namespace LibreUtau.Core {
    internal enum KeyType {
        BLACK,
        WHITE_UP,
        WHITE_DOWN,
        WHITE_UP_DOWN
    }

    public class PianoKey {
        private readonly KeyType NoteKeyType;
        private readonly string NoteStr;
        internal int OctaveNo = -1;

        internal PianoKey(string noteStr, KeyType keyType) {
            NoteStr = noteStr;
            NoteKeyType = keyType;
        }

        public PianoKey(PianoKey pianoKey) {
            NoteStr = pianoKey.NoteStr;
            NoteKeyType = pianoKey.NoteKeyType;
            OctaveNo = pianoKey.OctaveNo;
        }

        public bool IsBlack() => NoteKeyType == KeyType.BLACK;

        public void DrawKey(DrawingContext cxt, Rect rect, Brush brush, Brush textBrush, Pen pen) {
            LineSegment GetByCoords(double x, double y) {
                return new LineSegment(new Point(rect.Left + rect.Width * x, rect.Top + rect.Height * y), true);
            }

            double blackKeyLen = 0.6;
            PathSegment[] segments;
            switch (NoteKeyType) {
                case KeyType.BLACK:
                    segments = new[] {
                        GetByCoords(0, 1),
                        GetByCoords(blackKeyLen, 1),
                        GetByCoords(blackKeyLen, 0)
                    };
                    break;
                case KeyType.WHITE_UP:
                    segments = new[] {
                        GetByCoords(0, 1),
                        GetByCoords(1, 1),
                        GetByCoords(1, -0.5),
                        GetByCoords(blackKeyLen, -0.5),
                        GetByCoords(blackKeyLen, 0)
                    };
                    break;
                case KeyType.WHITE_DOWN:
                    segments = new[] {
                        GetByCoords(0, 1),
                        GetByCoords(blackKeyLen, 1),
                        GetByCoords(blackKeyLen, 1.5),
                        GetByCoords(1, 1.5),
                        GetByCoords(1, 0)
                    };
                    break;
                case KeyType.WHITE_UP_DOWN:
                    segments = new[] {
                        GetByCoords(0, 1),
                        GetByCoords(blackKeyLen, 1),
                        GetByCoords(blackKeyLen, 1.5),
                        GetByCoords(1, 1.5),
                        GetByCoords(1, -0.5),
                        GetByCoords(blackKeyLen, -0.5),
                        GetByCoords(blackKeyLen, 0)
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            PathGeometry keyShape = new PathGeometry(new[] {new PathFigure(rect.Location, segments, true)});
            cxt.DrawGeometry(brush, pen, keyShape);

            FormattedText text = new FormattedText(
                ToString(),
                Thread.CurrentThread.CurrentUICulture,
                FlowDirection.LeftToRight,
                SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                12,
                textBrush
            );
            cxt.DrawText(text,
                new Point(rect.X + 5, rect.Y + (rect.Height - text.Height) / 2));
        }

        public override string ToString() => NoteStr + (OctaveNo >= 0 ? Convert.ToString(OctaveNo) : "");
    }

    public static class MusicMath {
        public static readonly PianoKey[] KeysInOctave = {
            new PianoKey("C", KeyType.WHITE_UP),
            new PianoKey("C#", KeyType.BLACK),
            new PianoKey("D", KeyType.WHITE_UP_DOWN),
            new PianoKey("D#", KeyType.BLACK),
            new PianoKey("E", KeyType.WHITE_DOWN),
            new PianoKey("F", KeyType.WHITE_UP),
            new PianoKey("F#", KeyType.BLACK),
            new PianoKey("G", KeyType.WHITE_UP_DOWN),
            new PianoKey("G#", KeyType.BLACK),
            new PianoKey("A", KeyType.WHITE_UP_DOWN),
            new PianoKey("A#", KeyType.BLACK),
            new PianoKey("B", KeyType.WHITE_DOWN)
        };

        public static readonly int[] PossibleBeatUnits = {
            2, 4, 8, 16
        };

        public static readonly int MinTempo = 10, MaxTempo = 250;

        public static double[] zoomRatios = {4.0, 2.0, 1.0, 1.0 / 2, 1.0 / 4, 1.0 / 8, 1.0 / 16, 1.0 / 32, 1.0 / 64};

        public static PianoKey GetPianoKey(int noteNum) =>
            new PianoKey(KeysInOctave[noteNum % 12]) {OctaveNo = noteNum / 12};

        public static bool IsCenterKey(int noteNum) {
            return noteNum % 12 == 0;
        }

        public static double getZoomRatio(double quarterWidth, int beatPerBar, int beatUnit, double minWidth) {
            if (!PossibleBeatUnits.Contains(beatUnit))
                throw new Exception("Invalid beat unit.");

            int i = (int)Math.Log(beatUnit, 2) - 1;

            if (beatPerBar % 4 == 0) {
                i--; // level below bar is half bar, or 2 beatunit
            }
            // else // otherwise level below bar is beat unit

            if (quarterWidth * beatPerBar * 4 <= minWidth * beatUnit) {
                return beatPerBar / beatUnit * 4;
            }

            while (i + 1 < zoomRatios.Length && quarterWidth * zoomRatios[i + 1] > minWidth) {
                i++;
            }

            return zoomRatios[i];
        }

        public static double TickToMillisecond(double tick, double BPM, int beatUnit, int resolution) {
            return tick * 60000.0 / BPM * beatUnit / 4 / resolution;
        }

        public static int MillisecondToTick(double ms, double BPM, int beatUnit, int resolution) {
            return (int)Math.Ceiling(ms / 60000.0 * BPM / beatUnit * 4 * resolution);
        }

        public static double SinEasingInOut(Point p0, Point p1, double x) {
            return p0.Y + (p1.Y - p0.Y) * (1 - Math.Cos((x - p0.X) / (p1.X - p0.X) * Math.PI)) / 2;
        }

        public static double SinEasingInOutX(Point p0, Point p1, double y) {
            return Math.Acos(1 - (y - p0.Y) * 2 / (p1.Y - p0.Y)) / Math.PI * (p1.X - p0.X) + p0.X;
        }

        public static double SinEasingIn(Point p0, Point p1, double x) {
            return p0.Y + (p1.Y - p0.Y) * (1 - Math.Cos((x - p0.X) / (p1.X - p0.X) * Math.PI / 2));
        }

        public static double SinEasingInX(Point p0, Point p1, double y) {
            return Math.Acos(1 - (y - p0.Y) / (p1.Y - p0.Y)) / Math.PI * 2 * (p1.X - p0.X) + p0.X;
        }

        public static double SinEasingOut(Point p0, Point p1, double x) {
            return p0.Y + (p1.Y - p0.Y) * Math.Sin((x - p0.X) / (p1.X - p0.X) * Math.PI / 2);
        }

        public static double SinEasingOutX(Point p0, Point p1, double y) {
            return Math.Asin((y - p0.Y) / (p1.Y - p0.Y)) / Math.PI * 2 * (p1.X - p0.X) + p0.X;
        }

        public static double Linear(Point p0, Point p1, double x) {
            return p0.Y + (p1.Y - p0.Y) * (x - p0.X) / (p1.X - p0.X);
        }

        public static double LinearX(Point p0, Point p1, double y) {
            return (y - p0.Y) / (p1.Y - p0.Y) * (p1.X - p0.X) + p0.X;
        }

        public static double InterpolateShape(Point p0, Point p1, double x, PitchPointShape shape) {
            switch (shape) {
                case PitchPointShape.SINE_IN_OUT: return SinEasingInOut(p0, p1, x);
                case PitchPointShape.SINE_IN: return SinEasingIn(p0, p1, x);
                case PitchPointShape.SINE_OUT: return SinEasingOut(p0, p1, x);
                default: return Linear(p0, p1, x);
            }
        }

        public static double InterpolateShapeX(Point p0, Point p1, double y, PitchPointShape shape) {
            switch (shape) {
                case PitchPointShape.SINE_IN_OUT: return SinEasingInOutX(p0, p1, y);
                case PitchPointShape.SINE_IN: return SinEasingInX(p0, p1, y);
                case PitchPointShape.SINE_OUT: return SinEasingOutX(p0, p1, y);
                default: return LinearX(p0, p1, y);
            }
        }

        public static double DecibelToLinear(double db) {
            return Math.Pow(10, db / 20);
        }

        public static double LinearToDecibel(double v) {
            return Math.Log10(v) * 20;
        }

        public static float DecibelToVolume(double db) {
            return (db <= -24)
                ? 0
                : (float)((db < -16) ? DecibelToLinear(db * 2 + 16) : DecibelToLinear(db));
            //TODO Что за костыль?
        }
    }
}
