using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("ChatBasicApp_Tests")] //so that internal cts token is reachable for test

namespace ChatBasicApp
{
    public class ChatApplication
    {
        private static Task servertask;
        private static Task clienttask;
        internal static CancellationTokenSource cts;
        internal static CancellationToken token;
        private static IUI _ui;
        public static async Task Run(string[] args, IUI ui)
        {
            //while (!cancellation.IsCancellationRequested)
            //{
                _ui = ui;
                IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 8081);// change to IPAdress.Any()
                Console.WriteLine("ChatApplication ChatBasicApplication v.1 is running...)");
                cts = new CancellationTokenSource();
                token = cts.Token;
                

                if (args[0].ToLower() == "server")
                {
                    Server chatserver = new Server(endPoint, _ui);

                    await chatserver.Connect();

                    var listentask = Task.Run(() => chatserver.Listen(cts.Token));
                    var writetask = Task.Run(() => chatserver.Write(cts.Token));

                    await Task.WhenAny(listentask, writetask);

                    Console.WriteLine(" Server task ended...");
                    //Console.ReadKey();

                }

                else if (args[0].ToLower() == "client")
                {
                    Client client = new Client(endPoint, _ui, new ChatCommunicator()); //injects the ISocketClient

                    await client.Connect(token); //creates the Socket and connects to server 

                    var clientlisten = Task.Run(() => client.Listen(cts.Token));
                    var clientwrite = Task.Run(() => client.Write(cts.Token));
                    //try
                    //{
                    //    await clienttask;
                    //}

                    //catch (OperationCanceledException e)
                    //{
                    //    Console.WriteLine("Cancellation requested from remote server. This client task ended.");

                    //}
                    //catch(SocketException e)
                    //{
                    //    Console.WriteLine("The session was closed. Connection with server was lost." + " " +e.Message);
                    //}
                    await Task.WhenAny(clientlisten, clientwrite);
                    Console.WriteLine("Client task ended...");
                }
                else { _ui.Output("You need to include arguments : server or client, to start the application", NetworkServer.MessageType.General); }


                Console.WriteLine("A task ended (Press Any Key to quit).");
                await Task.Delay(500);


            //}
        }

    }

}
