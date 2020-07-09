using System;
using System.Windows;
using LibreUtau.Core.USTx;

namespace LibreUtau.Core {
    public static class MusicMath {
        public enum KeyColor { White, Black }

        public static readonly Tuple<string, KeyColor>[] KeysInOctave = {
            Tuple.Create("C", KeyColor.White),
            Tuple.Create("C#", KeyColor.Black),
            Tuple.Create("D", KeyColor.White),
            Tuple.Create("D#", KeyColor.Black),
            Tuple.Create("E", KeyColor.White),
            Tuple.Create("F", KeyColor.White),
            Tuple.Create("F#", KeyColor.Black),
            Tuple.Create("G", KeyColor.White),
            Tuple.Create("G#", KeyColor.Black),
            Tuple.Create("A", KeyColor.White),
            Tuple.Create("A#", KeyColor.Black),
            Tuple.Create("B", KeyColor.White)
        };

        public static double[] zoomRatios = {4.0, 2.0, 1.0, 1.0 / 2, 1.0 / 4, 1.0 / 8, 1.0 / 16, 1.0 / 32, 1.0 / 64};

        public static string GetNoteString(int noteNum) {
            return noteNum < 0 ? string.Empty : KeysInOctave[noteNum % 12].Item1 + (noteNum / 12);
        }

        public static bool IsBlackKey(int noteNum) {
            return KeysInOctave[noteNum % 12].Item2 == KeyColor.Black;
        }

        public static bool IsCenterKey(int noteNum) {
            return noteNum % 12 == 0;
        }

        public static double getZoomRatio(double quarterWidth, int beatPerBar, int beatUnit, double minWidth) {
            int i;

            switch (beatUnit) {
                case 2:
                    i = 0;
                    break;
                case 4:
                    i = 1;
                    break;
                case 8:
                    i = 2;
                    break;
                case 16:
                    i = 3;
                    break;
                default: throw new Exception("Invalid beat unit.");
            }

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
