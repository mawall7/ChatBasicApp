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
    public class ChatPeer //: IDisposable
    {
        private readonly IPEndPoint _iPEndPoint;
        public IChatCommunicator _chatCommunicator { get; set; }
        
        private IUI _ui;
        public StringBuilder WriteBuffer { get; set; }


       
        public ChatPeer(IPEndPoint ipEndPoint, IUI userinput, IChatCommunicator chatCommunicator)
        {
            _iPEndPoint = ipEndPoint;
            _ui = userinput;
            _chatCommunicator = chatCommunicator;
            _chatCommunicator.StatusMessage += (msg) => _ui.Output(msg, MessageType.Status); 
             WriteBuffer = new StringBuilder();
        }

        public async Task ConnectAsServerAsync() //TODO: future suggestion adding support for multiple clients.
        {

            _chatCommunicator.CreateSocket(_iPEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);

            _chatCommunicator.Bind(_iPEndPoint); // binds server
            _chatCommunicator.Listen(100); // this will put server socket into Listening mode for clients connect attempts.

            var connecttask = _chatCommunicator.AcceptAsync();//_socket.AcceptAsync();

            try
            {
                await connecttask;

            }
            catch (Exception e)
            {

                _ui.Output("Connection with remote failed. " + e.Message, MessageType.Error);
            }

        }
        public async Task ConnectAsClientAsync(CancellationToken cancellation) 
        {

                _chatCommunicator.CreateSocket(_iPEndPoint.AddressFamily,
                   SocketType.Stream,
                   ProtocolType.Tcp);

                bool connected = false;

                while (!connected && !cancellation.IsCancellationRequested)
                {
                    try
                    {
                        await _chatCommunicator.ConnectAsync(_iPEndPoint);  //await _client.ConnectAsync(IpEndPoint);
                        connected = true;
                        //_ui.Output("Connection accepted from server.", MessageType.Status ); use event inside ChatCommunicator class instead.
                    }
                    catch (Exception e)
                    {
                        _ui.Output("Will try again to connect to remote ." + e.Message, MessageType.Status);
                        await Task.Delay(2000);
                    }
                }
        }


        public async Task ListenAsync(CancellationToken token) //TODO: Make Readable
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
                    _ui.Output("The remote peer seems to have ungracefully disconnected. " + ex.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    break;
                    
                }
                    
                    if(received == 0)
                    {
                       _ui.Output("The remote peer disconnected.", MessageType.Status);
                       _chatCommunicator.Dispose();
                       break;
                    }

                    response = Encoding.UTF8.GetString(receivedFromRemote, 0, received);


                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        receivedBuffer.Append(response);
                        _ui.Output($"\n[Received] {response}", MessageType.Status); //TODO proccess messagebuffer
                        _ui.Output(">" + messageBuffer.ToString(), MessageType.General);
                    }


                // Handle input
                if (response.Contains("<|QUIT|>"))
                {
                    _ui.Output("remote quit.", MessageType.Status);
                    break;
                }
                
                if (response.Contains("<|ACK|>"))
                {
                    _ui.Output(
                    $"Received acknowledgment", MessageType.Status);
                    receivedBuffer.Replace("<|ACK|>", "");
                }
         
                if (response.Contains("<|EOM|>"))
                {
                    byte[] messageBytes = Encoding.UTF8.GetBytes("<|ACK|>");
                    await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);
                    int index = response.IndexOf("<|EOM|>");
                   
                    if(WriteBuffer.Length != 0)
                    {
                        ProcessResponse.CurrentLine(); //TODO: wrong name this simply clears line , should include message but keep output separate. 
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
                
            }
        }

        public async Task WriteAsync(CancellationToken token)//TODO: Make Readble
        {
            
            WriteBuffer = new StringBuilder();

            while (!token.IsCancellationRequested) 
            {
                await Task.Delay(100);//, token);
                
                if (_ui.IsConsoleUI())
                {

                    if (_ui.HasKey())  //TODO ! Make testable
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
                                await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None); //TODO: TimeOut
                                messageToSend = messageToSend.Replace("<|EOM|>", "");
                                _ui.Output(System.Environment.NewLine +
                                  $"Sent: {messageToSend}", MessageType.General);

                                //await WaitForAckAsync();
                                WriteBuffer.Clear();
                                _ui.Output("\nWrite another message:", MessageType.General);

                            }
                            catch (SocketException e)
                            {

                                _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                                _chatCommunicator.Dispose();
                                break;
                            }

                        }
                        //If input and not Enter pressed just Append to buffer and send writing...
                        else
                        {
                            if (inputresult != "<BackSpace>")
                            {

                                WriteBuffer.Append(inputresult);
                                //send code for writing... 
                                byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
                                try
                                {
                                     await _chatCommunicator.SendAsync(PrintingBytes, SocketFlags.None); 
                                }

                                catch (SocketException e)
                                {
                                    _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                                    _chatCommunicator.Dispose();
                                    break;

                                }
                            }
                            
                        }
                    }
                   
                    
                    
                }
                else if (_ui.IsConsoleUI() == false)//_ui is not ConsoleUI) //WPF UI or other MAUI
                {
                    string msg = _ui.ReadInput(); // TO DO: wpf handler can make sure to only execute this on return press so don't need that part (unlike for console) for writing this has be fixed in some way
                    if (msg.Contains("<|PRINT|>")) //FIX : change this ?
                    {
                        msg = "<|PRINT|>";
                    }
                    
                    byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
                    await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);

                }
            }
        }

       

        private void DeleteCurrentConsoleLine() 
        {
            int currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth)); // Overwrite the whole line with spaces
            Console.SetCursorPosition(0, currentLine); // Reset cursor to start
        }

        
    }
}
