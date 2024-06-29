using Microsoft.UI.Dispatching;
using System;

namespace Financial_Manager.Client.Services
{
    public class DispatcherQueueProvider
    {
        public DispatcherQueue? DispatcherQueue { get; private set; }

        public void Initialize(DispatcherQueue dispatcherQueue)
        {
            DispatcherQueue = dispatcherQueue ?? throw new ArgumentNullException(nameof(dispatcherQueue));
        }
    }
}
