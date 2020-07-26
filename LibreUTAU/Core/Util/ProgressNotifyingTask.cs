using System;
using System.ComponentModel;
using LibreUtau.Core.Commands;

namespace LibreUtau.Core.Util {
    public abstract class ProgressNotifyingTask : BackgroundWorker, ICmdSubscriber {
        protected ProgressNotifyingTask() {
            this.RunWorkerCompleted += (sender, args) =>
                CommandDispatcher.Inst.ExecuteCmd(new ProgressIndicatorVisibilityNotification(false));

            this.WorkerSupportsCancellation = true;

            CommandDispatcher.Inst.Subscribe(this);

            this.WorkerReportsProgress = true;
            this.ProgressChanged += (sender, args) =>
                CommandDispatcher.Inst.ExecuteCmd(
                    new ProgressIndicatorUpdateNotification(args.ProgressPercentage, TaskInfo));
        }

        protected abstract string TaskInfo { get; }

        public void SubscribeTo(ICmdPublisher publisher) {
            throw new NotImplementedException();
        }

        public void OnCommandExecuted(UCommand cmd, bool isUndo) {
            if (cmd is ProgressIndicatorCancelNotification) {
                this.CancelAsync();
                CommandDispatcher.Inst.ExecuteCmd(new ProgressIndicatorVisibilityNotification(false));
            }
        }

        public new void RunWorkerAsync() {
            CommandDispatcher.Inst.ExecuteCmd(new ProgressIndicatorVisibilityNotification(true));
            base.RunWorkerAsync();
        }
    }
}
