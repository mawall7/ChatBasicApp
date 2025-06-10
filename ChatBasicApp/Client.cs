using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkServer;

namespace ChatBasicApp
{
    public class Client //: IDisposable
    {
        private readonly IPEndPoint IpEndPoint;
        public IChatCommunicator _chatCommunicator { get; set; }
        //public Socket _client = null; 
        private IUI _ui;
        public StringBuilder WriteBuffer { get; set; }


       
        public Client(IPEndPoint ipEndPoint, IUI userinput, IChatCommunicator chatCommunicator)
        {
            IpEndPoint = ipEndPoint;
            _ui = userinput;
            _chatCommunicator = chatCommunicator;
            _chatCommunicator.StatusMessage += (msg) => _ui.Output(msg, MessageType.Status); // may cause problems both task are using the same event handler ? racing condition 
             WriteBuffer = new StringBuilder();
        }

        public async Task Connect(CancellationToken cancellation) 
        {
           
                //_chatCommunicator.StatusMessage += (msg) => _ui.Output(msg, MessageType.Status); //second parameter is allowed if it's not put in the (). 

                _chatCommunicator.CreateSocket(IpEndPoint.AddressFamily,
                   SocketType.Stream,
                   ProtocolType.Tcp);

                bool connected = false;

                while (!connected && !cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await _chatCommunicator.ConnectAsync(IpEndPoint);  //await _client.ConnectAsync(IpEndPoint);
                        connected = true;
                        //_ui.Output("Connection accepted from server.", MessageType.Status ); use event inside ChatCommunicator class instead.
                    }
                    catch (Exception e)
                    {
                        _ui.Output("Will try again to connect to server ." + e.Message, MessageType.Status);
                        await Task.Delay(2000);
                    }
                }
        }

        //public void Dispose()
        //{
        //    if (_chatCommunicator is not null)//(_client is not null)
        //    {
        //        _chatCommunicator.Dispose(); //hmmmmm move Dispose to the wrapper class shouldnt be in this class anymore
        //        _chatCommunicator = null;
        //    }
        //}

        public async Task Listen(CancellationToken token)
        {
            _ui.Output("Write message to send.", MessageType.Status);

            StringBuilder messageBuffer = new();
            string response = "";
            byte[] receivedFromRemote = new byte[1024];
            StringBuilder receivedBuffer = new();

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(100);

                
                int received = 0;
                try
                {
                
                    received = await _chatCommunicator.ReceiveAsync(receivedFromRemote, SocketFlags.None); 
                }
               

                catch (SocketException ex)
                {
                    _ui.Output("The remote server seems to have ungracefully disconnected. " + ex.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    break;
                    
                }
                    
                    if(received == 0)
                    {
                       _ui.Output("The remote server disconnected.", MessageType.Status);
                       _chatCommunicator.Dispose();
                       break;
                    }

                    response = Encoding.UTF8.GetString(receivedFromRemote, 0, received);


                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        receivedBuffer.Append(response);
                        _ui.Output($"\n[Received] {response}", MessageType.Status);
                        _ui.Output(">" + messageBuffer.ToString(), MessageType.General);
                    }


                // Handle input
                if (response.Contains("<|QUIT|>"))
                {
                    _ui.Output("Server quit.", MessageType.Status);
                    break;
                }
                
                if (response.Contains("<|ACK|>"))
                {
                    _ui.Output(
                    $"client received acknowledgment from server", MessageType.Status);
                    receivedBuffer.Replace("<|ACK|>", "");
                }
         
                if (response.Contains("<|EOM|>"))
                {
                    int index = response.IndexOf("<|EOM|>");
                   
                    if(WriteBuffer.Length != 0)
                    {
                        ProcessResponse.CurrentLine(); //wrong name this simply clears line , should include message but keep output separate. 
                    }
                    
                    string message = response.Substring(0, index);
                    
                    _ui.Output($"Message received: {message}", MessageType.General);
                    //move to ProcessResponse 
                    
                    receivedBuffer = receivedBuffer.Remove(0, index + "<|EOM|>".Length);
                    Array.Clear(receivedFromRemote, 0, receivedFromRemote.Length);
                }
                else if (response.Contains("<|PRINT|>"))
                {
                    _ui.Output("writing...", MessageType.General);
                }
                
                if(WriteBuffer.Length > 0)
                {
                    Console.Write(WriteBuffer.ToString());
                }
                

                //if (Console.KeyAvailable)
                //{
                //    var key = Console.ReadKey(true);

                //    //send writing...
                //    byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
                //    await _client.SendAsync(PrintingBytes, SocketFlags.None);


                //    if (key.Key == ConsoleKey.Enter || key.Key == ConsoleKey.F1)
                //    {
                //        string messageToSend = messageBuffer.ToString() + "<|EOM|>";
                        
                //        if(key.Key == ConsoleKey.F1)
                //        {
                //            messageToSend = "<|QUIT|>";
                //        }
                //        //Send message or quit message
                //        byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                //        await _client.SendAsync(messageBytes, SocketFlags.None);

                //        if(key.Key == ConsoleKey.F1) 
                //        {
                //            Console.WriteLine("You quit the chat session.");
                //            break;
                //        }
                //        else 
                //        { 
                //            Console.WriteLine(System.Environment.NewLine +
                //                $"Sent: {messageToSend.Replace("<|EOM|>","")}");

                           
                //            //await WaitForAckAsync();

                //            messageBuffer.Clear();
                //            Console.WriteLine("Message acknowledged by server.\nWrite another message:");
                //        }
                //    }

                //    else
                //    {
                //        messageBuffer.Append(key.KeyChar);
                //        Console.Write(key.KeyChar);
                //    }
                //}
            }
        }

        public async Task Write(CancellationToken token)
        {
            
            WriteBuffer = new StringBuilder();

            while (!token.IsCancellationRequested) 
            {
                if (_ui is ConsoleUI)
                {

                    if (Console.KeyAvailable)
                    {
                        
                        string inputresult = _ui.ReadInput();

                        //Handle the processed inputresult

                        if (inputresult == "<|Quit|>")
                        {
                            _ui.Output("You quit the chat session.", MessageType.General);
                            break;
                        }

                        if (inputresult == "<BackSpace>")
                        {
                            WriteBuffer = WriteBuffer.Remove(WriteBuffer.Length - 1, 1);
                            Console.Write(" \b"); //erase last char and move back
                        }

                        if (inputresult.Contains("<|EOM|>")) // On Enter input (send the message) 
                        {
                            string messageToSend = inputresult == "<|EOM|>" ? WriteBuffer.ToString() + "<|EOM|>" : inputresult;  //EOM means enter input or else sent quit message
                            byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                            try
                            {
                                await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);

                                _ui.Output(System.Environment.NewLine +
                                  $"Sent: {inputresult.Replace("<|EOM|>", "")}", MessageType.General);

                                //await WaitForAckAsync();
                                WriteBuffer.Clear();
                                _ui.Output("\nWrite another message:", MessageType.General);

                            }
                            catch (SocketException e)
                            {

                                _ui.Output("Message from client could not be sent properly. " + e.Message, MessageType.Error);
                            }

                           

                            //if (inputresult.Contains("<|EOM|>") && inputresult !="<BackSpace>")
                            //{
                                
                            //    _ui.Output(System.Environment.NewLine +
                            //       $"Sent: {inputresult.Replace("<|EOM|>", "")}", MessageType.General);

                            //    //await WaitForAckAsync();
                            //    WriteBuffer.Clear();
                            //    _ui.Output("\nWrite another message:", MessageType.General);
                            //}
                        }
                        //If input and not Enter pressed just Append to buffer and send writing...
                        else
                        {
                            if (inputresult != "<BackSpace>")
                            {

                                WriteBuffer.Append(inputresult);
                            //send code for writing... 
                            byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
                            await _chatCommunicator.SendAsync(PrintingBytes, SocketFlags.None);//_client.SendAsync(PrintingBytes, SocketFlags.None);
                            }
                            //_ui.Output(key, MessageType.General);
                        }
                    }
                }
                else if (_ui is not ConsoleUI) //WPF UI or other MAUI
                {
                    string msg = _ui.ReadInput(); // TO DO: wtf handler can make sure to only execute this on return press so don't need that part (unlike for console) for writing this has be fixed in some way
                    if (msg.Contains("<|PRINT|>")) //FIX : change this ?
                    {
                        msg = "<|PRINT|>";
                    }
                    
                    byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
                    await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);

                }
            }
        }

       
            //string response = "";
            //_ui.Output("Write message to send.");
            //var message = "Hi friends 👋!<|EOM|>";
            //var messagebuffer = "";
            //var messageBytes = Encoding.UTF8.GetBytes(message);
            //Task<int> receivedtask = default;
            //byte[] buffer = default;

            //ConsoleKeyInfo? input = null;

            //while (true)
            //{

            //        //await Task.Delay(500);
            //        //buffer = new byte[1_024];
            //        //receivedtask = _client.ReceiveAsync(buffer, SocketFlags.None); //this is awaited when response is ready below. since await will wait and block priting isCompleted is checked. s0 that ui is not blocked from printing.

            //        if (Console.KeyAvailable)
            //            {
            //                input = Console.ReadKey(true);


            //                if(input.Value.Key != ConsoleKey.Enter) ///here we see the need to also listen for messages inside of the loop;
            //                {
            //                    messagebuffer+= input.Value.KeyChar.ToString(); //use stringbuilder instead

            //                    if (!string.IsNullOrEmpty(response)) //check if there's a received message. if so take the current ongoing written message and print it after the recieved message.  
            //                    {
            //                        DeleteCurrentConsoleLine();

            //                        Console.WriteLine(response); //print receieved message
            //                        //print current ongoing written message afterwards
            //                        Console.WriteLine(messagebuffer);
            //                    }
            //                    Console.Write(input.Value.KeyChar); 
            //                }
            //                else if(input.Value.Key == ConsoleKey.Enter)
            //                { //this only sends on Enter press


            //                    messagebuffer +="<|EOM|>";
            //                    messageBytes = Encoding.UTF8.GetBytes(messagebuffer);
            //                    _ = await _client.SendAsync(messageBytes, SocketFlags.None);
            //                    Console.WriteLine($"{System.Environment.NewLine}Socket client sent message: \"{messagebuffer.Replace("<|EOM|>", "")}\"");

            //                    messagebuffer = "";
            //                    input = null;
            //                    await Task.Delay(500);
            //                    break;
            //                }
            //            }

            //    //     Socket client sent message: "Hi friends 👋!<|EOM|>"
            //    //     Socket client received acknowledgment: "<|ACK|>"
            //}

            //        while (true)
            //        {
            //            buffer = new byte[1_024];
            //            receivedtask = _client.ReceiveAsync(buffer, SocketFlags.None); //this is awaited when response is ready below. since await will wait and block priting isCompleted is checked. s0 that ui is not blocked from printing.
            //            if (receivedtask.IsCompleted)
            //            {
            //                response = Encoding.UTF8.GetString(buffer, 0, await receivedtask);

            //                if (response == "<|ACK|>")
            //                {
            //                    Console.WriteLine(
            //                    $"Socket client received acknowledgment: \"{response}\"");
            //                    Console.ReadLine();
            //                    break;
            //                }
            //            }
            //            // Sample output:
            //        }

            //    Console.WriteLine("Press any key you like to quit.");
            //     Console.ReadKey();
            //    _client.Shutdown(SocketShutdown.Both);
            //     Console.ReadKey();
        

        private void DeleteCurrentConsoleLine() 
        {
            int currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth)); // Overwrite the whole line with spaces
            Console.SetCursorPosition(0, currentLine); // Reset cursor to start
        }

        
    }
}
