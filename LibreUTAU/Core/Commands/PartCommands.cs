using LibreUtau.Core.USTx;

namespace LibreUtau.Core.Commands {
    public abstract class PartCommand : UCommand {
        public UPart Part;
        public UProject Project;
    }

    public class AddPartCommand : PartCommand {
        public AddPartCommand(UProject project, UPart part) {
            this.Project = project;
            this.Part = part;
        }

        public override string ToString() { return "Add part"; }
        public override void Execute() { Project.Parts.Add(Part); }
        public override void Rollback() { Project.Parts.Remove(Part); }
    }

    public class RemovePartCommand : PartCommand {
        public RemovePartCommand(UProject project, UPart part) {
            this.Project = project;
            this.Part = part;
        }

        public override string ToString() { return "Remove parts"; }
        public override void Execute() { Project.Parts.Remove(Part); }
        public override void Rollback() { Project.Parts.Add(Part); }
    }

    public class MovePartCommand : PartCommand {
        private readonly int newPos, oldPos;
        public readonly int newTrackNo, oldTrackNo;

        public MovePartCommand(UProject project, UPart part, int newPos, int newTrackNo) {
            this.Project = project;
            this.Part = part;
            this.newPos = newPos;
            this.newTrackNo = newTrackNo;
            this.oldPos = part.PosTick;
            this.oldTrackNo = part.TrackNo;
        }

        public override string ToString() { return "Move parts"; }

        public override void Execute() {
            Part.PosTick = newPos;
            Part.TrackNo = newTrackNo;
        }

        public override void Rollback() {
            Part.PosTick = oldPos;
            Part.TrackNo = oldTrackNo;
        }
    }

    public class ResizePartCommand : PartCommand {
        readonly int newDur;
        readonly int oldDur;

        public ResizePartCommand(UProject project, UPart part, int newDur) {
            this.Project = project;
            this.Part = part;
            this.newDur = newDur;
            this.oldDur = part.DurTick;
        }

        public override string ToString() { return "Change parts duration"; }
        public override void Execute() { Part.DurTick = newDur; }
        public override void Rollback() { Part.DurTick = oldDur; }
    }

    public class RenamePartCommand : PartCommand {
        readonly string OldName, NewName;

        public RenamePartCommand(UProject project, UPart part, string newName) {
            this.Project = project;
            this.Part = part;
            NewName = newName;
            OldName = part.Name;
        }

        public override void Execute() {
            this.Part.Name = NewName;
        }

        public override void Rollback() {
            this.Part.Name = OldName;
        }

        public override string ToString() => $"Rename part from {OldName} to {NewName}";
    }
}
