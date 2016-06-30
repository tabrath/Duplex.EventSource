using System;

namespace Duplex.EventSource
{
    public class EventSourceEventArgs : EventArgs
    {
        public dynamic Data { get; }

        public EventSourceEventArgs(dynamic data)
        {
            Data = data;
        }
    }
}

