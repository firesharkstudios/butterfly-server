using System;
using System.Threading.Tasks;

namespace Butterfly.Core.Database {
    public class OnCommitRef {
        public readonly Func<Task> onCommit;
        public readonly string key;
        public OnCommitRef(Func<Task> onCommit, string key) {
            this.onCommit = onCommit;
            this.key = key;
        }
    }
}
