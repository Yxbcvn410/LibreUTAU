using System.Collections.Generic;
using System.Linq;

namespace LibreUtau.Core.Commands {
    public abstract class UCommand {
        public abstract void Execute();
        public abstract void Rollback();

        public abstract override string ToString();
    }

    public class UCommandGroup {
        public List<UCommand> Commands;
        public UCommandGroup() { Commands = new List<UCommand>(); }
        public override string ToString() { return Commands.Count == 0 ? "No op" : Commands.First().ToString(); }
    }

    public class ICmdPublisher {
        private readonly List<ICmdSubscriber> subscribers = new List<ICmdSubscriber>();

        public void Subscribe(ICmdSubscriber subscriber) {
            if (!subscribers.Contains(subscriber)) subscribers.Add(subscriber);
        }

        protected void Publish(UCommand cmd, bool isUndo = false) {
            foreach (var sub in subscribers) sub.OnCommandExecuted(cmd, isUndo);
        }
    }

    public interface ICmdSubscriber {
        void SubscribeTo(ICmdPublisher publisher);
        void OnCommandExecuted(UCommand cmd, bool isUndo);
    }
}
