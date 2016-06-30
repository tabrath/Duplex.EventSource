using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Duplex.EventSource
{
    public class EventSourceClient : IDisposable
    {
        public event EventHandler<EventSourceEventArgs> Event;
        public event EventHandler Heartbeat;

        public Uri Uri { get; }

        private readonly WebClient client;
        private CancellationTokenSource cancellation;

        public EventSourceClient(Uri uri)
        {
            Uri = uri;
            this.client = new WebClient();
        }

        public EventSourceClient(string uri)
            : this(new Uri(uri))
        {
        }

        ~EventSourceClient()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.cancellation?.Dispose();
                this.client?.Dispose();
            }
        }

        public void Start()
        {
            if (this.cancellation != null)
                throw new InvalidOperationException("Already started");

            if (Event.GetInvocationList().Length == 0)
                throw new InvalidOperationException("No listeners assigned");

            this.cancellation = new CancellationTokenSource();
            this.client.OpenReadTaskAsync(Uri)
                  .ContinueWith(async t =>
            {
                if (t.IsFaulted)
                {
                    Debug.WriteLine("[EventSourceClient] Faulted: " + t.Exception.Flatten());
                    return;
                }

                if (t.IsCanceled)
                {
                    Debug.WriteLine("[EventSourceClient] Cancelled");
                    return;
                }

                try
                {
                    using (var reader = new StreamReader(t.Result, Encoding.UTF8))
                    {
                        //TODO: support events and multiline data..
                        string data = string.Empty;
                        while (!this.cancellation.IsCancellationRequested)
                        {
                            data = await reader.ReadLineAsync();
                            if (string.IsNullOrEmpty(data))
                                continue;

                            if (data.StartsWith(":heartbeat", StringComparison.InvariantCulture))
                            {
                                Debug.WriteLine("[EventSourceClient] Got heartbeat");
                                Heartbeat?.Invoke(this, EventArgs.Empty);
                                continue;
                            }

                            if (data.StartsWith("data:", StringComparison.InvariantCulture))
                            {
                                var json = data.Substring(6).Trim();
                                if (string.IsNullOrEmpty(json) || string.IsNullOrWhiteSpace(json))
                                    continue;

                                try
                                {
                                    var parsed = JsonConvert.DeserializeObject(json);
                                    Event?.Invoke(this, new EventSourceEventArgs(parsed));
                                }
                                catch (Exception e)
                                {
                                    Debug.WriteLine("[EventSourceClient] Failed parsing json: '" + json + "': " + e.Message);
                                }
                                continue;
                            }

                            Debug.WriteLine("[EventSourceClient] Got something else: " + data);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("[EventSourceClient] Exception: " + e);
                }
            });
        }

        public void Stop()
        {
            this.cancellation?.Cancel();
        }
    }
}

