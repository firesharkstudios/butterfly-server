using System;
using System.Threading;
using System.Threading.Tasks;

namespace Butterfly.Core.Util {
    // Based on http://linanqiu.github.io/2017/10/10/Disposable-Background-Tasks-in-C/
    public class RunEvery : IDisposable {
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _task;

        public RunEvery(Action action, int everyMillis) {
            Console.WriteLine("DisposableThread started");

            this._cancellationTokenSource = new CancellationTokenSource();
            var token = this._cancellationTokenSource.Token;

            this._task = Task.Run(async () => {
                while (!token.IsCancellationRequested) {
                    action();
                    await Task.Delay(everyMillis, token);
                }
            }, token);
        }


        public void Dispose() {
            this._cancellationTokenSource.Cancel();
            try {
                _task.Wait();
            }
            catch (AggregateException) {
            }
        }
    }
}
