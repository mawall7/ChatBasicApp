using ChatBasicApp.UI;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    class Program
    {
            public static IUI AppUI { get; set; }
            public static IInputHandler inputHandler { get; set; }
            public static IInputProcessor inputProcessor { get; set; }
            public static IUIRenderer uiRenderer { get; set; }

            public static IChatCommunicator communicator { get; set; }

        public static void Main(string[] args) 
        {
            if (args.Length > 1)
            {

                AppUI = (args[1]) switch //TODO: Add arguement for GUI 
                {
                    string ui when ui == "UnitTest" => new ConsoleUI(),
                    string ui when ui == "IntegrationTest" => new ReadLineConsoleUI(),
                    //string ui when ui == "other"
                    _ => new ConsoleUI()
                };

            }
                
                
            if(AppUI == null || AppUI is ConsoleUI) //TODO : refactor to follow DRY  
            {
                AppUI = new ConsoleUI();
                inputHandler = new ConsoleInputHandler();
                uiRenderer = new ConsoleRenderer();
                communicator = new ChatCommunicator();
                inputProcessor = new ProcessConsoleResponse(new ConsoleUI(), uiRenderer, communicator);
            }
            
            try
            {
                IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 8081);// change to IPAdress.Any()
                ChatApplication.Run(args, AppUI, inputProcessor, inputHandler, uiRenderer, new ChatPeer(endPoint, AppUI, communicator)).GetAwaiter().GetResult();

                //ChatApplication.Run(args, new ConsoleUI()).GetAwaiter().GetResult();

            }
            catch (Exception e)
            {
                Console.WriteLine("Unexpected Error. Application will close. " + e.Message);
            }
                
        }
            
      
    }
    //public class ChatApplication
    //{
    //    private static Task servertask;
    //    private static Task clienttask;
    //    internal static CancellationTokenSource cts;
    //    internal static CancellationToken token;
    //    private static IUI _ui;
    //    public static async Task Run(string[] args, IUI ui)
    //    {
    //        _ui = ui;
    //        IPEndPoint endPoint = new(IPAddress.Parse("127.0.0.1"), 8081);// change to IPAdress.Any()
    //        Console.WriteLine("Chat v1 Started!");
    //        cts = new CancellationTokenSource();
    //        token = cts.Token;

    //        if (args[0].ToLower() == "server")
    //        {
    //            Server chatserver = new Server(endPoint, _ui);
                
    //            await chatserver.Connect();

    //            var listentask = Task.Run(() => chatserver.Listen(cts.Token));

    //            var writetask = Task.Run(() => chatserver.Write(cts.Token));

    //            await Task.WhenAny(listentask, writetask);

    //            Console.WriteLine("A task ended.(Press any key to quit.)");
    //            Console.ReadKey();
    //            //try
    //            //{
    //            //    await servertask;
    //            //}

    //            //catch (OperationCanceledException e) //catches TokenCancellationSource cancellations
    //            //{
    //            //    Console.WriteLine("Cancellation requeste. This Server task ended." + " " +e.Message);

    //            //}
    //            //catch(SocketException e)
    //            //{
    //            //    Console.WriteLine("The session was closed. connnection with client was lost.");
    //            //}

    //        }

    //        else if (args[0].ToLower() == "client")
    //        {
    //            Client client = new Client(endPoint, _ui, new ChatCommunicator()); //injects the ISocketClient
    //            await client.Connect(); //creates the Socket and connects to server

    //            //ChatCommunicator _client = new ChatCommunicator()

    //            var clientlisten = Task.Run(() => client.Listen(cts.Token));
    //            var clientwrite = Task.Run(() => client.Write(cts.Token));
    //            //try
    //            //{
    //            //    await clienttask;
    //            //}

    //            //catch (OperationCanceledException e)
    //            //{
    //            //    Console.WriteLine("Cancellation requested from remote server. This client task ended.");

    //            //}
    //            //catch(SocketException e)
    //            //{
    //            //    Console.WriteLine("The session was closed. Connection with server was lost." + " " +e.Message);
    //            //}
    //            await Task.WhenAny(clientlisten, clientwrite);
    //        }


    //        Console.WriteLine("a task ended");
    //        await Task.Delay(500);


    //    }

    //}
}
