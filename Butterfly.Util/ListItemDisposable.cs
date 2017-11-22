using System;
using System.Collections.Generic;

namespace Butterfly.Util {
    public class ListItemDisposable<T> : IDisposable {

        protected readonly List<T> list;
        protected readonly T item;

        public ListItemDisposable(List<T> list, T item) {
            this.list = list;
            this.item = item;
            this.list.Add(item);
        }

        public void Dispose() {
            this.list.Remove(this.item);
        }
    }
}
