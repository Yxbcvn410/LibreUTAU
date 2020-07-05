using LibreUtau.Core.USTx;

namespace LibreUtau.Core.Commands {
    public abstract class PartCommand : UCommand {
        public UPart part;
        public UProject project;
    }

    public class AddPartCommand : PartCommand {
        public AddPartCommand(UProject project, UPart part) {
            this.project = project;
            this.part = part;
        }

        public override string ToString() { return "Add part"; }
        public override void Execute() { project.Parts.Add(part); }
        public override void Rollback() { project.Parts.Remove(part); }
    }

    public class RemovePartCommand : PartCommand {
        public RemovePartCommand(UProject project, UPart part) {
            this.project = project;
            this.part = part;
        }

        public override string ToString() { return "Remove parts"; }
        public override void Execute() { project.Parts.Remove(part); }
        public override void Rollback() { project.Parts.Add(part); }
    }

    public class MovePartCommand : PartCommand {
        private readonly int newPos, oldPos;
        public readonly int newTrackNo, oldTrackNo;

        public MovePartCommand(UProject project, UPart part, int newPos, int newTrackNo) {
            this.project = project;
            this.part = part;
            this.newPos = newPos;
            this.newTrackNo = newTrackNo;
            this.oldPos = part.PosTick;
            this.oldTrackNo = part.TrackNo;
        }

        public override string ToString() { return "Move parts"; }

        public override void Execute() {
            part.PosTick = newPos;
            part.TrackNo = newTrackNo;
        }

        public override void Rollback() {
            part.PosTick = oldPos;
            part.TrackNo = oldTrackNo;
        }
    }

    public class ResizePartCommand : PartCommand {
        readonly int newDur;
        readonly int oldDur;

        public ResizePartCommand(UProject project, UPart part, int newDur) {
            this.project = project;
            this.part = part;
            this.newDur = newDur;
            this.oldDur = part.DurTick;
        }

        public override string ToString() { return "Change parts duration"; }
        public override void Execute() { part.DurTick = newDur; }
        public override void Rollback() { part.DurTick = oldDur; }
    }
}
