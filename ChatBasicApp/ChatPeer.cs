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
    public class ChatPeer : IChatPeer//: IDisposable
    {
        private readonly IPEndPoint _iPEndPoint;
        public IChatCommunicator _chatCommunicator { get; set; }

        private IUI _ui;
        public static StringBuilder WriteBuffer { get; set; }

        public Dictionary<string, Delegate> ProcessInputDict = new()
        {
            { "<BackSpace>", new Func<int, int, StringBuilder>((x, y) => { return WriteBuffer.Remove(1, 2); }) },
            { "<Space>", new Action<string?>((s) => Console.Write(WriteBuffer.ToString())) }
        };

        public ChatPeer(IPEndPoint ipEndPoint, IUI userinput, IChatCommunicator chatCommunicator)
        {
            _iPEndPoint = ipEndPoint;
            _ui = userinput;
            _chatCommunicator = chatCommunicator;
            _chatCommunicator.StatusMessage += (msg) => _ui.Output(msg, MessageType.Status);
            WriteBuffer = new StringBuilder(); //Console Buffer
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
                    //TODO: possible refactoring 
                    //
                    //{
                    //  string Message = ReceiveMessage(); handle errors in method
                    //  if(string.IsNotNullOrEmpty(Message)
                    //  received.Add(RecievedMessage)
                    //}
                    received = await _chatCommunicator.ReceiveAsync(receivedFromRemote, SocketFlags.None);
                }


                catch (SocketException ex)
                {
                    _ui.Output("The remote peer seems to have ungracefully disconnected. " + ex.Message, MessageType.Error);
                    _chatCommunicator.Dispose();
                    break;

                }

                if (received == 0)
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

                    if (WriteBuffer.Length != 0)
                    {
                        ConsoleRenderer.DeleteCurrentConsoleLine(); 
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

            }
        }

        public async Task WriteAsync(CancellationToken token, IInputProcessor inputProcessor, IUIRenderer renderer, IInputHandler inputHandler)//TODO: Make Readable make input 
        {
          
             Task responseTask = default;
             string inputresult;
             WriteBuffer = new StringBuilder();
            
            _ui.Output("Write a message to send.", MessageType.Status);

            while (!token.IsCancellationRequested)
            {
                await Task.Delay(100);

                        
                        inputresult = inputHandler.ReadInput(_ui); //will never return Print message!
                        
                        if(MessageParser.IsQuit(inputresult))
                        {
                            _ui.Output("You quit the chat session.", MessageType.General);    
                            break;
                        }
                        else if (MessageParser.IsRenderCommand(inputresult)) //space or bs 
                        {
                            renderer.ReRender(inputresult, WriteBuffer); //TODO: possible change. renders text for console (for now). Possible to make work with WPF if wanted.
                        }
                       
                        
                        else if (MessageParser.IsEOM(inputresult)) 
                        {
                            responseTask = inputProcessor.ProcessFullMessage(inputresult, WriteBuffer); // TODO: possible change. internalize WriteBuffer.
                            //ToDO intenalize this : if(receivedBuffer.Length > 0) { renderer.ReRenderLines()}
                        }
                        
                        else if (MessageParser.IsPrint(inputresult))
                        {
                            responseTask = inputProcessor.ProcessPrintMessage(inputresult, WriteBuffer);
                        }
                        else 
                        {
                            throw new ParseInputException();
                        }

                        try
                        {
                            await responseTask;
                        }
                        catch (SocketException e)
                        {
                            _ui.Output("Exception thrown on WriteAsync." + e.Message, MessageType.Error);
                        }
                       



                        //if (inputresult.Contains("<|EOM|>")) // On Enter input (send the message) 
                        //{
                        //    string messageToSend = inputresult == "<|EOM|>" ? WriteBuffer.ToString() + "<|EOM|>" : inputresult;  //EOM means enter input or else sent quit message
                        //    byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
                        //    try
                        //    {
                        //        await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None); //TODO: TimeOut
                        //        messageToSend = messageToSend.Replace("<|EOM|>", "");
                        //        _ui.Output(System.Environment.NewLine +
                        //          $"Sent: {messageToSend}", MessageType.General);

                        //        //await WaitForAckAsync();
                        //        WriteBuffer.Clear();
                        //        _ui.Output("\nWrite another message:", MessageType.General);

                        //    }
                        //    catch (SocketException e)
                        //    {

                        //        _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                        //        _chatCommunicator.Dispose();
                        //        break;
                        //    }

                        //}
                        ////If input and not Enter pressed just Append to buffer and send writing...

                        //else if (inputresult.Length == 1) // == is not token
                        //{

                        //    WriteBuffer.Append(inputresult);
                        //    //send code for writing... 
                        //    byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
                        //    try
                        //    {
                        //        await _chatCommunicator.SendAsync(PrintingBytes, SocketFlags.None);
                        //    }

                        //    catch (SocketException e)
                        //    {
                        //        _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
                        //        _chatCommunicator.Dispose();
                        //        break;

                        //    }
                        //}
                        //if (inputresult == "<|Quit|>")
                        //{
                        //    _ui.Output("You quit the chat session.", MessageType.General); //TODO: change. wrong! should quit token should be sent;
                        //    break;
                        //}



                //    }

                //}
                ////ProcessInput(_ui)
                //else if (_ui.IsConsoleUI() == false)//_ui is not ConsoleUI) //WPF UI or other MAUI
                //{
                //    string msg = _ui.ReadInput(); // TO DO: wpf handler can make sure to only execute this on return press so don't need that part (unlike for console) for writing this has be fixed in some way
                //    if (msg.Contains("<|PRINT|>")) //FIX : change this ?
                //    {
                //        msg = "<|PRINT|>";
                //    }
                //    if (msg.Contains("<|Quit|>"))
                //        break;

                //    byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
                //    await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);

                //}
            }
        }

        //ConsolePrinter

        //public void RenderConsoleWindow(string inputresult, StringBuilder WriteBuffer)
        //{
        //    var CursorPos = Console.GetCursorPosition();

        //    if (inputresult == "<Space>") //TODO  bug writing a letter and then space adds characters to the end
        //    {
        //        CursorPos = Console.GetCursorPosition();

        //        WriteBuffer = WriteBuffer.Insert(CursorPos.Left, " ");
        //        Console.SetCursorPosition(CursorPos.Left + 1, CursorPos.Top);
        //        CursorPos = Console.GetCursorPosition();
        //        DeleteCurrentConsoleLine();
        //        Console.Write(WriteBuffer.ToString());
        //        Console.SetCursorPosition(CursorPos.Left, CursorPos.Top);
        //        return;
        //    }

        //    else if (inputresult == "<BackSpace>")
        //    {

        //        WriteBuffer = WriteBuffer.Remove(CursorPos.Left - 1, 1);
        //        DeleteCurrentConsoleLine();
        //        Console.Write(WriteBuffer.ToString());
        //        Console.SetCursorPosition(CursorPos.Left - 1, CursorPos.Top);
        //        return;

        //        //Console.Write("\b \b"); //erase last char and move back
        //    }


        //    else if (inputresult == "<Left>")
        //    {
        //        Console.SetCursorPosition(CursorPos.Left - 1, CursorPos.Top); //TO DO doesn work 
        //        return;
        //    }
        //    else if (inputresult == "<Right>")
        //    {
        //        Console.SetCursorPosition(CursorPos.Left + 1, CursorPos.Top);
        //        return;
        //    }
        //    else
        //        Console.Write(inputresult);

        //}



        //private void DeleteCurrentConsoleLine()
        //{
        //    int currentLine = Console.CursorTop;
        //    Console.SetCursorPosition(0, currentLine);
        //    Console.Write(new string(' ', Console.WindowWidth)); // Overwrite the whole line with spaces
        //    Console.SetCursorPosition(0, currentLine); // Reset cursor to start
        //}



        //public string inputHandler(string input, IUI _ui)
        //{
        //    string inputresult = null;

        //    if (_ui.IsConsoleUI())
        //    {

        //        if (_ui.HasKey())//TODO ! Make testable and clean. 
        //        {

        //            inputresult = _ui.ReadInput();
        //        }
        //    }
        //    else if (!_ui.IsConsoleUI())
        //    {
        //        inputresult = _ui.ReadInput();
        //    }
            
        //    return inputresult;
        //}

        //public async Task InputProcessor(string inputresult, IUI _ui, IUIRenderer ConsoleRenderer, IChatCommunicator _chatCommunicator, StringBuilder WriteBuffer) //TO DO : erase Quit , have a input handler/ConsoleHelper return type. check quit before process input and sending.
        //{
           
        //            ConsoleRenderer.ReRender(inputresult, WriteBuffer);

        //            //TODO: change 
        //            //if (inputresult == "<|Quit|>")
        //            //{
        //            //    _ui.Output("You quit the chat session.", MessageType.General); //TODO: change. wrong! should quit token should be sent;
        //            //    break;
        //            //}

        //    if(_ui is ConsoleUI) {

        //        if (inputresult.Contains("<|EOM|>")) // On Enter input (send the message) 
        //        {
        //            string messageToSend = inputresult == "<|EOM|>" ? WriteBuffer.ToString() + "<|EOM|>" : inputresult;  //EOM means enter input or else sent quit message
        //            byte[] messageBytes = Encoding.UTF8.GetBytes(messageToSend);
        //            try
        //            {
        //                await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None); //TODO: TimeOut
        //                messageToSend = messageToSend.Replace("<|EOM|>", "");
        //                _ui.Output(System.Environment.NewLine +
        //                  $"Sent: {messageToSend}", MessageType.General);

        //                //await WaitForAckAsync();
        //                WriteBuffer.Clear();
        //                _ui.Output("\nWrite another message:", MessageType.General);

        //            }
        //            catch (SocketException e)
        //            {

        //                _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
        //                _chatCommunicator.Dispose();
        //                throw new SocketException();
        //            }

        //            if (inputresult.Length == 1) // == is not token
        //            {

        //                WriteBuffer.Append(inputresult);
        //                //send code for writing... 
        //                byte[] PrintingBytes = Encoding.UTF8.GetBytes("<|PRINT|>");
        //                try
        //                {
        //                    await _chatCommunicator.SendAsync(PrintingBytes, SocketFlags.None);
        //                }

        //                catch (SocketException e)
        //                {
        //                    _ui.Output("Message could not be sent properly. " + e.Message, MessageType.Error);
        //                    _chatCommunicator.Dispose();
        //                    throw new SocketException();

        //                }
        //            }
        //            if (inputresult == "<|Quit|>")
        //            {
        //                _ui.Output("You quit the chat session.", MessageType.General); //TODO: change. wrong! should quit token should be sent;
        //                throw new TaskCanceledException();
        //            }



        //            //}
        //            //}
        //            //ProcessInput(_ui)
        //        }
        //        else if (_ui.IsConsoleUI() == false)//_ui is not ConsoleUI) //WPF UI or other MAUI
        //        {
        //            string msg = _ui.ReadInput(); // TO DO: wpf handler can make sure to only execute this on return press so don't need that part (unlike for console) for writing this has be fixed in some way
        //            if (msg.Contains("<|PRINT|>")) //FIX : change this ?
        //            {
        //                msg = "<|PRINT|>";
        //            }
        //            if (msg.Contains("<|Quit|>"))
        //                throw new TaskCanceledException();

        //            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
        //            await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);

        //        }
        //    }
        //}
    } }

