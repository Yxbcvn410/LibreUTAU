using LibreUtau.Core.USTx;

namespace LibreUtau.Core.Commands {
    public class UNotification : UCommand {
        public UPart part;
        public UProject project;
        public override void Execute() { }
        public override void Rollback() { }
        public override string ToString() { return "Notification"; }
    }

    /// <summary>
    ///     Message for user's information.
    /// </summary>
    public class UserMessageNotification : UNotification {
        public string message;
        public UserMessageNotification(string message) { this.message = message; }
        public override string ToString() { return "User message: " + message; }
    }

    public class LoadPartNotification : UNotification {
        public LoadPartNotification(UPart part, UProject project) {
            this.part = part;
            this.project = project;
        }

        public override string ToString() { return "Load part"; }
    }

    public class LoadProjectNotification : UNotification {
        public LoadProjectNotification(UProject project) { this.project = project; }
        public override string ToString() { return "Load project"; }
    }

    public class SaveProjectNotification : UNotification {
        public string Path;
        public SaveProjectNotification(string path) { Path = path; }
        public override string ToString() { return "Save project"; }
    }

    public class RedrawNotesNotification : UNotification {
        public override string ToString() { return "Redraw Notes"; }
    }

    public class ChangeExpressionListNotification : UNotification {
        public override string ToString() { return "Change expression list"; }
    }

    public class SelectExpressionNotification : UNotification {
        public string ExpKey;
        public int SelectorIndex;
        public bool UpdateShadow;

        public SelectExpressionNotification(string expKey, int index, bool updateShadow) {
            ExpKey = expKey;
            SelectorIndex = index;
            UpdateShadow = updateShadow;
        }

        public override string ToString() { return "Select expression " + ExpKey; }
    }

    public class ShowPitchExpNotification : UNotification {
        public override string ToString() { return "Show pitch expression list"; }
    }

    public class HidePitchExpNotification : UNotification {
        public override string ToString() { return "Hide pitch expression list"; }
    }

    // Notification for UI to move PlayPosMarker
    public class SetPlayPosTickNotification : UNotification {
        public int playPosTick;
        public SetPlayPosTickNotification(int tick) { this.playPosTick = tick; }
        public override string ToString() { return "Set play position to tick " + playPosTick; }
    }

    // Notification for playback manager to change play position
    public class SeekPlayPosTickNotification : UNotification {
        public int playPosTick;
        public SeekPlayPosTickNotification(int tick) { this.playPosTick = tick; }
        public override string ToString() { return "Seek play position to tick " + playPosTick; }
    }

    public class ProgressIndicatorUpdateNotification : UNotification {
        public string Info;
        public int Progress;

        public ProgressIndicatorUpdateNotification(int progress, string info) {
            Progress = progress;
            Info = info;
        }

        public override string ToString() => $"Set progress {Progress}, info {Info}";
    }

    public class ProgressIndicatorVisibilityNotification : UNotification {
        public bool Visible;

        public ProgressIndicatorVisibilityNotification(bool visible) {
            Visible = visible;
        }

        public override string ToString() => $"Set progressbar visibility {Visible}";
    }

    public class ProgressIndicatorCancelNotification : UNotification {
        public override string ToString() => "Cancel background task";
    }

    public class VolumeChangeNotification : UNotification {
        public int TrackNo;
        public double Volume;

        public VolumeChangeNotification(int trackNo, double volume) {
            this.TrackNo = trackNo;
            this.Volume = volume;
        }

        public override string ToString() { return $"Set track {TrackNo} volume to {Volume}"; }
    }
}
