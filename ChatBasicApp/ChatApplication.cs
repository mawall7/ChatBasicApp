﻿using System;
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
        
        
        internal static CancellationTokenSource cts;
        internal static CancellationToken token;
        private static IUI _ui;
        private readonly IChatPeer chatPeer;
        //private static IInputProcessor _inputProcessor;
        public static async Task Run(string[] args, IUI ui, IInputProcessor inputProcessor,
            IInputHandler inputHandler, IUIRenderer renderer, IChatPeer chatPeer)
        {
            
                _ui = ui;
                //IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 8081);// change to IPAdress.Any()
                //Console.WriteLine("ChatApplication ChatBasicApplication v.1 is running...)");
                _ui.Output("ChatBasicApplication v.1 is running...",NetworkServer.MessageType.Status);
                cts = new CancellationTokenSource();
                token = cts.Token;
                //add check ui and add appropriate Iinput processor 

                if (args[0].ToLower() == "server")
                {
                //Server chatserver = new Server(endPoint, _ui, new ChatCommunicator());
                IChatPeer chatserver = chatPeer; //new ChatPeer(endPoint, _ui, new ChatCommunicator());

                    await chatserver.ConnectAsServerAsync();

                    var listentask = Task.Run(() => chatserver.ListenAsync(cts.Token));
                    var writetask = Task.Run(() => chatserver.WriteAsync(cts.Token, inputProcessor, renderer, inputHandler));

                    await Task.WhenAny(listentask, writetask);

                    _ui.Output(" Server task ended...", NetworkServer.MessageType.Status);
                    //Console.ReadKey();

                }

                else if (args[0].ToLower() == "client")
                {
                    IChatPeer client = chatPeer;//new ChatPeer(endPoint, _ui, new ChatCommunicator()); //injects the ISocketClient

                    await client.ConnectAsClientAsync(token); //creates the Socket and connects to server 

                    var clientlisten = Task.Run(() => client.ListenAsync(cts.Token));
                    var clientwrite = Task.Run(() => client.WriteAsync(cts.Token, inputProcessor, renderer, inputHandler));
                    //try
                    //{
                    //    await clienttask;
                    //}

                    //catch (OperationCanceledException e)
                    //{
                    //    Console.WriteLine("Cancellation requested from remote server. This client task ended.");

                  
                    await Task.WhenAny(clientlisten, clientwrite);
                    _ui.Output("Client task ended...", NetworkServer.MessageType.Status);
                }
                else { _ui.Output("You need to include arguments : server or client, to start the application", NetworkServer.MessageType.General); }


                _ui.Output("A task ended (Press Any Key to quit).", NetworkServer.MessageType.Status);
                await Task.Delay(500);

        }

    }

}
