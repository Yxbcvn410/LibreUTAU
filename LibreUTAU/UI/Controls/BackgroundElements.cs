using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using LibreUtau.Core;

namespace LibreUtau.UI.Controls {
    class BackgroundElement : FrameworkElement {
        protected Size _size;
        protected bool _updated;

        public BackgroundElement() {
            this.SizeChanged += (o, e) => {
                _size = e.NewSize;
                MarkUpdate();
            };
        }

        public void MarkUpdate() { _updated = true; }

        public void RenderIfUpdated() {
            if (_updated) this.InvalidateVisual();
            _updated = false;
        }

        public static void MarkUpdateCallback(DependencyObject source, DependencyPropertyChangedEventArgs e) {
            ((BackgroundElement)source).MarkUpdate();
        }
    }

    class TrackBackground : BackgroundElement {
        public static readonly DependencyProperty TrackHeightProperty = DependencyProperty.Register("TrackHeight",
            typeof(double), typeof(TrackBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public static readonly DependencyProperty OffsetYProperty = DependencyProperty.Register("OffsetY",
            typeof(double), typeof(TrackBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public TrackBackground() {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
        }

        public double TrackHeight {
            set { SetValue(TrackHeightProperty, value); }
            get { return (double)GetValue(TrackHeightProperty); }
        }

        public double OffsetY {
            set { SetValue(OffsetYProperty, value); }
            get { return (double)GetValue(OffsetYProperty); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            int firstTrack = (int)(OffsetY / TrackHeight);
            bool alt = firstTrack % 2 == 1;
            double top = TrackHeight * firstTrack - OffsetY;

            while (top < _size.Height) {
                drawingContext.DrawRectangle(
                    alt ? ThemeManager.TrackBackgroundBrushAlt : ThemeManager.TrackBackgroundBrush,
                    null,
                    new Rect(0, (int)top, _size.Width, TrackHeight));
                drawingContext.DrawLine(new Pen(ThemeManager.TickLineBrushLight, 1), new Point(0, (int)top),
                    new Point(_size.Width, (int)top));
                top += TrackHeight;
                alt = !alt;
            }
        }
    }

    class KeyTrackBackground : TrackBackground {
        protected override void OnRender(DrawingContext drawingContext) {
            int firstTrack = (int)(OffsetY / TrackHeight);
            int alt = firstTrack;
            double top = TrackHeight * firstTrack - OffsetY;

            while (top < _size.Height) {
                int noteNum = UIConstants.MaxNoteNum - alt - 1;
                drawingContext.DrawRectangle(
                    MusicMath.IsBlackKey(noteNum)
                        ? ThemeManager.TrackBackgroundBrushAlt
                        : ThemeManager.TrackBackgroundBrush,
                    null,
                    new Rect(0, (int)top, _size.Width, TrackHeight));
                drawingContext.DrawLine(new Pen(ThemeManager.TickLineBrushLight, 1), new Point(0, (int)top),
                    new Point(_size.Width, (int)top));
                top += TrackHeight;
                alt++;
            }
        }
    }

    class KeyboardBackground : KeyTrackBackground {
        protected override void OnRender(DrawingContext drawingContext) {
            int firstTrack = (int)(OffsetY / TrackHeight);
            int alt = firstTrack;
            double top = TrackHeight * firstTrack - OffsetY;

            while (top < _size.Height) {
                int noteNum = UIConstants.MaxNoteNum - alt - 1;
                drawingContext.DrawRectangle(
                    MusicMath.IsBlackKey(noteNum) ? ThemeManager.BlackKeyBrushNormal :
                    MusicMath.IsCenterKey(noteNum) ? ThemeManager.CenterKeyBrushNormal :
                    ThemeManager.WhiteKeyBrushNormal,
                    new Pen(ThemeManager.BlackKeyBrushNormal, 1),
                    new Rect(0, (int)top, _size.Width, TrackHeight));

                FormattedText text = new FormattedText(
                    MusicMath.GetNoteString(noteNum),
                    Thread.CurrentThread.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                    12,
                    MusicMath.IsBlackKey(noteNum) ? ThemeManager.BlackKeyNameBrushNormal :
                    MusicMath.IsCenterKey(noteNum) ? ThemeManager.CenterKeyNameBrushNormal :
                    ThemeManager.WhiteKeyNameBrushNormal
                );
                drawingContext.DrawText(text,
                    new Point(_size.Width - text.Width - 5, (int)(top + (TrackHeight - text.Height) / 2)));

                top += TrackHeight;
                alt++;
            }
        }
    }

    class TickBackground : BackgroundElement {
        public static readonly DependencyProperty QuarterWidthProperty = DependencyProperty.Register("QuarterWidth",
            typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public static readonly DependencyProperty MinTickWidthProperty = DependencyProperty.Register("MinTickWidth",
            typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register("OffsetX",
            typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public static readonly DependencyProperty QuarterOffsetProperty = DependencyProperty.Register("QuarterOffset",
            typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public static readonly DependencyProperty BeatPerBarProperty = DependencyProperty.Register("BeatPerBar",
            typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));

        public static readonly DependencyProperty BeatUnitProperty = DependencyProperty.Register("BeatUnit",
            typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));

        public static readonly DependencyProperty TickModeProperty = DependencyProperty.Register("TickMode",
            typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));

        protected Pen darkPen, lightPen, dashedPen;

        public TickBackground() {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
            darkPen = new Pen(ThemeManager.TickLineBrushDark, 1);
            lightPen = new Pen(ThemeManager.TickLineBrushLight, 1);
            dashedPen = new Pen(ThemeManager.TickLineBrushLight, 1)
                {DashStyle = new DashStyle(UIConstants.DashLineArray, 0)};
        }

        public double QuarterWidth {
            set { SetValue(QuarterWidthProperty, value); }
            get { return (double)GetValue(QuarterWidthProperty); }
        }

        public double MinTickWidth {
            set { SetValue(MinTickWidthProperty, value); }
            get { return (double)GetValue(MinTickWidthProperty); }
        }

        public double OffsetX {
            set { SetValue(OffsetXProperty, value); }
            get { return (double)GetValue(OffsetXProperty); }
        }

        public double QuarterOffset {
            set { SetValue(QuarterOffsetProperty, value); }
            get { return (double)GetValue(QuarterOffsetProperty); }
        }

        public int BeatPerBar {
            set { SetValue(BeatPerBarProperty, value); }
            get { return (int)GetValue(BeatPerBarProperty); }
        }

        public int BeatUnit {
            set { SetValue(BeatUnitProperty, value); }
            get { return (int)GetValue(BeatUnitProperty); }
        }

        public int TickMode {
            set { SetValue(TickModeProperty, value); }
            get { return (int)GetValue(TickModeProperty); }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int tick = (int)((OffsetX + QuarterOffset * QuarterWidth) / interval) + 1;
            double left = tick * interval - OffsetX - QuarterOffset * QuarterWidth;

            while (left < _size.Width) {
                double snappedLeft = Math.Round(left) + 0.5;
                if ((tick * zoomRatio * BeatUnit) % (BeatPerBar * 4) == 0) {
                    drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5),
                        new Point(snappedLeft, ActualHeight + 0.5));
                } else if ((tick * zoomRatio * BeatUnit) % 4 == 0) {
                    if (TickMode == 1)
                        drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5),
                            new Point(snappedLeft, ActualHeight + 0.5));
                    else
                        drawingContext.DrawLine(lightPen, new Point(snappedLeft, -0.5),
                            new Point(snappedLeft, ActualHeight + 0.5));
                } else if ((tick * zoomRatio * BeatUnit) % 1 == 0) {
                    if (TickMode == 1)
                        drawingContext.DrawLine(lightPen, new Point(snappedLeft, -0.5),
                            new Point(snappedLeft, ActualHeight + 0.5));
                    else
                        drawingContext.DrawLine(dashedPen, new Point(snappedLeft, -0.5),
                            new Point(snappedLeft, ActualHeight + 0.5));
                } else {
                    drawingContext.DrawLine(dashedPen, new Point(snappedLeft, -0.5),
                        new Point(snappedLeft, ActualHeight + 0.5));
                }

                left += interval;
                tick++;
            }
        }
    }

    class TimelineBackground : TickBackground {
        readonly Dictionary<int, FormattedText> fTextPool = new Dictionary<int, FormattedText>();

        protected override void OnRender(DrawingContext drawingContext) {
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int tick = (int)((OffsetX + QuarterOffset * QuarterWidth) / interval);
            double left = tick * interval - OffsetX - QuarterOffset * QuarterWidth;
            bool first_tick = true;

            while (left < _size.Width) {
                double snappedLeft = Math.Round(left) + 0.5;
                if ((tick * zoomRatio * BeatUnit) % (BeatPerBar * 4) == 0) {
                    if (!first_tick)
                        drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5),
                            new Point(snappedLeft, ActualHeight + 0.5));
                    int barNumber = (int)((tick * zoomRatio * BeatUnit) / BeatPerBar / 4 + 1);

                    FormattedText fText;
                    if (!fTextPool.ContainsKey(barNumber)) {
                        fText = new FormattedText(
                            barNumber.ToString(),
                            Thread.CurrentThread.CurrentUICulture,
                            FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                            12,
                            darkPen.Brush);
                        fTextPool.Add(barNumber, fText);
                    } else fText = fTextPool[barNumber];

                    drawingContext.DrawText(fText, new Point(snappedLeft + 3, 3));
                }

                left += interval;
                tick++;
                first_tick = false;
            }
        }
    }
}
