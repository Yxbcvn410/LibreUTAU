using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreUtau.Core.Lib;
using LibreUtau.Core.USTx;
using LibreUtau.Core;

namespace LibreUtau.Core {
    /// <summary>
    /// DocManager class
    /// Controls actions performed on the project
    /// TODO:
    /// Why play position is here?
    /// Maybe, rename to CommandDispatcher?
    /// </summary>
    class DocManager : ICmdPublisher {
        DocManager() {
            Project = new UProject();
        }

        static DocManager _s;

        static DocManager GetInst() {
            return _s ?? (_s = new DocManager());
        }

        public static DocManager Inst { get { return GetInst(); } }

        public int playPosTick;

        public UProject Project { get; private set; }

        # region Command Queue

        readonly Deque<UCommandGroup> undoQueue = new Deque<UCommandGroup>();
        readonly Deque<UCommandGroup> redoQueue = new Deque<UCommandGroup>();
        UCommandGroup undoGroup;
        UCommandGroup savedPoint;

        public bool ChangesSaved {
            get {
                return Project.Saved && (undoQueue.Count > 0 && savedPoint == undoQueue.Last() ||
                                         undoQueue.Count == 0 && savedPoint == null);
            }
        }

        public void ExecuteCmd(UCommand cmd, bool quiet = false) {
            if (cmd is UNotification) {
                switch (cmd) {
                    case SaveProjectNotification saveNotification:
                        if (undoQueue.Count > 0) savedPoint = undoQueue.Last();
                        Formats.USTx.Save(
                            string.IsNullOrEmpty(saveNotification.Path) ? Project.FilePath : saveNotification.Path,
                            Project);
                        break;
                    case LoadProjectNotification loadNotification:
                        undoQueue.Clear();
                        redoQueue.Clear();
                        undoGroup = null;
                        savedPoint = null;
                        this.Project = loadNotification.project;
                        this.playPosTick = 0;
                        break;
                    case SetPlayPosTickNotification setPlayPosNotification:
                        this.playPosTick = setPlayPosNotification.playPosTick;
                        break;
                }

                Publish(cmd);
                if (!quiet) System.Diagnostics.Debug.WriteLine($"Publish notification {cmd}");
                return;
            } else if (undoGroup == null) {
                System.Diagnostics.Debug.WriteLine("Null undoGroup");
                return;
            } else {
                undoGroup.Commands.Add(cmd);
                cmd.Execute();
                Publish(cmd);
            }

            if (!quiet) System.Diagnostics.Debug.WriteLine($"ExecuteCmd {cmd}");
        }

        public void StartUndoGroup() {
            if (undoGroup != null) {
                System.Diagnostics.Debug.WriteLine("undoGroup already started");
                EndUndoGroup();
            }

            undoGroup = new UCommandGroup();
            System.Diagnostics.Debug.WriteLine("undoGroup started");
        }

        public void EndUndoGroup() {
            if (undoGroup != null && undoGroup.Commands.Count > 0) {
                undoQueue.AddToBack(undoGroup);
                redoQueue.Clear();
            }

            if (undoQueue.Count > Util.Preferences.Default.UndoLimit) undoQueue.RemoveFromFront();
            undoGroup = null;
            System.Diagnostics.Debug.WriteLine("undoGroup ended");
        }

        public void Undo() {
            if (undoQueue.Count == 0) return;
            var lastCommandGroup = undoQueue.RemoveFromBack();
            for (int i = lastCommandGroup.Commands.Count - 1; i >= 0; i--) {
                var cmd = lastCommandGroup.Commands[i];
                cmd.Rollback();
                if (!(cmd is NoteCommand)) Publish(cmd, true);
            }

            redoQueue.AddToBack(lastCommandGroup);
        }

        public void Redo() {
            if (redoQueue.Count == 0) return;
            var lastCommandGroup = redoQueue.RemoveFromBack();
            foreach (var cmd in lastCommandGroup.Commands) {
                cmd.Execute();
                Publish(cmd);
            }

            undoQueue.AddToBack(lastCommandGroup);
        }

        # endregion

        # region Command handeling

        # endregion
    }
}
