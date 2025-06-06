using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetworkServer;

namespace ChatBasicApp
{
    public class Server //:IDisposable
    {
        
        IPEndPoint iPEndPoint;
        private IUI _ui;
        private IChatCommunicator _chatCommunicator { get; set; }

        public Server(IPEndPoint iPendPoint, IUI userinput, IChatCommunicator ChatCommunicator)
        {
            iPEndPoint = iPendPoint;
            _ui = userinput;
            _chatCommunicator = ChatCommunicator;
        }

        public async Task Connect()
        {
            _chatCommunicator.StatusMessage += (msg) => _ui.Output(msg, MessageType.Status); // may cause problems both task are using the same event handler ? racing condition 
            _chatCommunicator.CreateSocket(iPEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp);
            
            _chatCommunicator.Bind(iPEndPoint); // binds server
            _chatCommunicator.Listen(100); // this will put server socket into Listening mode for clients connect attempts.
           
            var connecttask = _chatCommunicator.AcceptAsync();//_socket.AcceptAsync();

            try
            { 
                await connecttask;

                //Checked chatgpt for these errors : 
                //DualMode system.not supported Exception
                //EnableBroadCast socketException
                //MultiCastLoopBack socketException
                //those internal errors usually don’t interfer and doesn't have to be handled, they happen just because you havn't opted for these props.

            }
            catch (Exception e)
            {

                _ui.Output("Connection with client failed.", MessageType.Error);
            }

        }


        public async Task Listen(CancellationToken token)
        {
            int received = 0;

            _chatCommunicator.StatusMessage += (msg) => _ui.Output(msg, MessageType.Status); // may cause problems both task are using the same event handler ? racing condition 


            while (!token.IsCancellationRequested)
            {
                
                var buffer = new byte[1_024];

                // Receive message
                try
                {
                    received = await _chatCommunicator.ReceiveAsync(buffer, SocketFlags.None); //to do gör en buffer och kolla att TCP fått ett komplpp meddelande innehåller "|EOF|" eller ACK eller <|PRINT|>  - en process messages metod ?

                }
                catch (SocketException ex)
                {

                    _ui.Output("The remote client seems to ungracefully have disconnected. " + ex.Message, MessageType.Warning);
                   
                }
                
                if (received == 0) // 0 is returned on a  gracefull disconnect, but other diconnects have to be caught (gives SocketException) with will not detect crashes etc or if the remote close program with ctrl-c. 
                {
                    _ui.Output("Remote client disconnected.", MessageType.Status);
                    break;
                }

                var response = Encoding.UTF8.GetString(buffer, 0, received);

                var eom = "<|EOM|>";
                if (response.IndexOf(eom) > -1 && !response.Contains("<|QUIT|>"))//received end of message and Q is not pressed
                {

                    _ui.Output(
                        $"Socket server received message: {response}", MessageType.Status);
                   
                    _ui.Output($"{response.Replace(eom, "")}", MessageType.General);

                    var ackMessage = "<|ACK|>";
                    var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
                    await _chatCommunicator.SendAsync(echoBytes, 0); //await _socket.SendAsync(echoBytes, 0);
                    
                    _ui.Output(
                        $"Socket server sent acknowledgment: \"{ackMessage}\"", MessageType.Status);//send
                }

                else if (response.IndexOf("<|PRINT|>") > -1)
                {
                    _ui.Output("Writing...", MessageType.General);
                }
                if (response.Contains("<|QUIT|>"))
                {
                    _ui.Output("Remote client ended the chat session.", MessageType.Status);
                    break;
                }
                
            }
            _ui.Output("Press any key to quit.", MessageType.Status);
            if (_ui is ConsoleUI)
            {
                Console.ReadLine();
            }
            else _ui.ReadInput();
        }

        public void SendMessage(string message)
        {
            var echoBytes = Encoding.UTF8.GetBytes(message);
            _chatCommunicator.SendAsync(echoBytes, 0);  
            //_socket.SendAsync(echoBytes, 0);
        }

        public async Task Write(CancellationToken token) //to do Make Write testabble and do a common process commands class for server and client. 
        {
            var WriteBuffer = new StringBuilder();
            
            
            while (!token.IsCancellationRequested)
            {
                if (_ui is ConsoleUI)
                {

                    if (Console.KeyAvailable)
                    {
                        
                        string message = default;

                        try
                        {
                            var input = _ui.ReadInput(); 
                            
                        
                            message = input == "<|EOM|>"  
                                ? WriteBuffer.ToString() 
                                : (input == "<|Quit|>" 
                                ? input : "<|PRINT|>");

                            if (input != "<BackSpace>")
                            {
                                WriteBuffer.Append(input); //to do buffer and process tcp response to correct string to not get buffered strings like <|PRINT|hello<|PRINT|>> writing..etc. 
                            }
                            else 
                            {
                                if (WriteBuffer.Length > 0)
                                {
                                    WriteBuffer = WriteBuffer.Remove(WriteBuffer.Length - 1, 1);
                                }

                                ConsoleHelper.Backspace();
                            }
                            
                            SendMessage(message);
                            
                            if(input == "<|EOM|>")
                            {
                                WriteBuffer.Clear();
                                Console.Write(System.Environment.NewLine);
                            }
                        
                        }

                        catch (InvalidOperationException ex)
                        {
                            _ui.Output("error at input." + ex.Message, MessageType.Warning);
                        }
                        catch(SocketException e)
                        {
                            _ui.Output("error on sending message. " + e.Message, MessageType.Error);
                        }
                    }
                }
                else 
                {
                    try
                    {
                        string msg = _ui.ReadInput(); 
                        if (msg.Contains("<|PRINT|>")) 
                        {
                            msg = "<|PRINT|>";
                        }

                        byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
                        await _chatCommunicator.SendAsync(messageBytes, SocketFlags.None);
                    }

                    catch (Exception e)
                    {

                        _ui.Output("Error occured when trying to send message from Server. " + e.Message, MessageType.Error);
                    }
                }
            }

            
        }


        //public async Task Connect() 
        //{

        //    Socket sockethandler = new(
        //    iPEndPoint.AddressFamily,
        //    SocketType.Stream,
        //    ProtocolType.Tcp);

        //    sockethandler.Bind(iPEndPoint);
        //    sockethandler.Listen(100);

        //    var connecttask = sockethandler.AcceptAsync();

        //    while (!connecttask.IsCompleted)
        //    {
        //            Console.WriteLine("Connecting to client");
        //            await Task.Delay(2000);
        //    }
        //    _socket = await connecttask;

        //    Console.WriteLine("Server is connected to client.");
        //}

        //public void Dispose()
        //{
        //    if(_socket != null)
        //    {
        //        _socket.Dispose();
        //    }
        //}

        //public async Task Listen(CancellationToken token)
        //{
        //    int received = 0;

        //    while (!token.IsCancellationRequested)
        //    {
        //        // Receive message.
        //        var buffer = new byte[1_024];


        //                try
        //                {
        //                    received = await _socket.ReceiveAsync(buffer, SocketFlags.None); //to do gör en buffer och kolla att TCP fått ett helt meddelande innehåller "|EOF|" eller ACK eller <|PRINT|>  - en process messages metod // även om medelanden skickas separat från client tex. först PRINT sedan message så kanske receive blir "|<PRINT> message| eller bara "<PRI" sedan NT> sedan message , hur meddalandet skickas och tas emot går inte att kontrollera när TCP används.

        //                }
        //                catch (SocketException ex)
        //                {

        //                    Console.WriteLine("The remote client seems to ungracefully have disconnected. " + ex.Message);
        //                    Dispose();
        //                }

        //        //check disconnect //however Poll will only detect remote graceful disconnets deliberately but not crashes
        //        if (received == 0) //will not detect crashes etc or if the remote close program with ctrl-c. will have to be handled by catch SocketException
        //        {
        //            Console.WriteLine("Remote client disconnected.");
        //            Dispose(); //close the socket
        //            break;
        //        }

        //        var response = Encoding.UTF8.GetString(buffer, 0, received);

        //        //end of message should be sent after receiveing a message ending with eom suffix
        //        var eom = "<|EOM|>";
        //        if (response.IndexOf(eom) > -1 && !response.Contains("<|QUIT|>"))//received end of message and Q is not pressed
        //        {

        //            Console.WriteLine(
        //                $"Socket server received message: \"{response.Replace(eom, "")}\"");

        //            var ackMessage = "<|ACK|>";
        //            var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
        //            await _socket.SendAsync(echoBytes, 0);
        //            Console.WriteLine(
        //                $"Socket server sent acknowledgment: \"{ackMessage}\"");//send
        //        }

        //        else if (response.IndexOf("<|PRINT|>") > -1)
        //        {
        //            Console.WriteLine("Writing...");
        //        }
        //        if (response.Contains("<|QUIT|>"))
        //        {
        //            Console.WriteLine("Remote client ended the chat session.");
        //            break;
        //        }

        //        //Recieve part
        //    }
        //    Console.WriteLine("Press any key to quit.");
        //    Console.ReadLine();
        //}

        //public void SendMessage(string message)
        //{
        //    var echoBytes = Encoding.UTF8.GetBytes(message);
        //    _socket.SendAsync(echoBytes, 0);
        //}

        //public void Write(CancellationToken token)
        //{
        //    while (!token.IsCancellationRequested)
        //    {

        //        if (Console.KeyAvailable)
        //        {

        //            try
        //            {
        //                var key = Console.ReadKey().KeyChar;
        //                //Console.WriteLine("Key available");
        //                SendMessage("<|PRINT|>");
        //            }
        //            catch (InvalidOperationException ex)
        //            {
        //                Console.WriteLine("error at input." + ex.Message);
        //            }
        //        }
        //    }

        //    Dispose();
        //}


        //public async Task Run()
        //{
        //    int received = 0;
        //    while (true)
        //    {
        //        // Receive message.
        //        var buffer = new byte[1_024];

        //        if (_socket.Available > 0)
        //        {
        //            if (_socket.Poll(0, SelectMode.SelectRead))
        //            {
        //                received = await _socket.ReceiveAsync(buffer, SocketFlags.None); //to do gör en buffer och kolla att TCP fått ett helt meddelande innehåller "|EOF|" eller ACK eller <|PRINT|>  - en process messages metod // även om medelanden skickas separat från client tex. först PRINT sedan message så kanske receive blir "|<PRINT> message| eller bara "<PRI" sedan NT> sedan message , hur meddalandet skickas och tas emot går inte att kontrollera när TCP används.
        //            }
        //        }

        //        //check disconnect //however Poll will only detect remote graceful disconnets deliberately but not crashes
        //        if(_socket.Available == 0 && _socket.Poll(0, SelectMode.SelectRead)) //will not detect crashes etc or if the remote close program with ctrl-c. 
        //        {
        //            Console.WriteLine("Remote Client disconnected.");
        //            break;
        //        }

        //            var response = Encoding.UTF8.GetString(buffer, 0, received);

        //            //end of message should be sent after receiveing a message ending with eom suffix
        //            var eom = "<|EOM|>";
        //            if (response.IndexOf(eom) > -1 && !response.Contains("<|QUIT|>"))//received end of message and Q is not pressed
        //            {

        //                Console.WriteLine(
        //                    $"Socket server received message: \"{response.Replace(eom, "")}\"");

        //                var ackMessage = "<|ACK|>";
        //                var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
        //                await _socket.SendAsync(echoBytes, 0);
        //                Console.WriteLine(
        //                    $"Socket server sent acknowledgment: \"{ackMessage}\"");//send
        //            }

        //            else if (response.IndexOf("<|PRINT|>") > -1)
        //            {
        //                Console.WriteLine("Writing...");
        //            }
        //            if (response.Contains("<|QUIT|>"))
        //            {
        //                Console.WriteLine("Remote client ended the chat session.");
        //                break;
        //            }

        //        //Recieve part

        //        if (Console.KeyAvailable)
        //        {
        //            var key = Console.ReadKey().KeyChar;
        //            //Console.WriteLine("Key available");
        //            SendMessage("<|PRINT|>");
        //        }

        //    }
        //    Console.WriteLine("Press any key to quit.");
        //    Console.ReadLine();
        //}

        //public void SendMessage(string message)
        //{
        //    var echoBytes = Encoding.UTF8.GetBytes(message);
        //    _socket.SendAsync(echoBytes, 0);
        //}
    }

}