using System;
using Duplex.EventSource;

namespace Duplex.EventSourceConsole
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            using (var client = new EventSourceClient("http://localhost:8080/v1/testapp/testchannel"))
            {
                client.Event += (sender, e) => Console.WriteLine("Event: " + e.Data);
                client.Start();
                Console.ReadLine();
            }
        }
    }
}
