using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;

namespace Financial_Manager.Client.Helpers
{
    public class DispatcherQueueHelper
    {
        private readonly DispatcherQueue _dispatcherQueue;

        public DispatcherQueueHelper(DispatcherQueue dispatcherQueue)
        {
            _dispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        }

        public Task EnqueueAsync(Action action)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            _dispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    action();
                    taskCompletionSource.SetResult(true);
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            return taskCompletionSource.Task;
        }
    }
}
