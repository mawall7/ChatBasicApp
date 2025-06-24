using System;
using Xunit;
using System.Diagnostics;
using System.Threading.Tasks;
using ChatBasicApp;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using Xunit.Abstractions;

namespace Console_IntegrationTests
{

    public class UnitTest1 
    {
        public UnitTest1(ITestOutputHelper output)
        {
            _output = output;
        }
        
       
        private readonly ITestOutputHelper _output;


        [Trait("Category", "IntegrationTest")]
        [Fact]

        public async Task ChatPeers_ShouldExchangeMessages()
        {
            var communicatorA = new InMemoryChatCommunicator();
            var communicatorB = new InMemoryChatCommunicator();
            communicatorA.ConnectToPeer(communicatorB);

            var serverUI = new TestUI();
            var clientUI = new TestUI();

            var serverEndPoint = new ChatPeer(new IPEndPoint(IPAddress.Loopback, 12345), serverUI, communicatorA);
            var clientEndPoint = new ChatPeer(new IPEndPoint(IPAddress.Loopback, 12346), clientUI, communicatorB);

            var cts = new CancellationTokenSource();

            string message = "Hello Server<|EOM|>";
            string messageAcknowledgement = "Message Sent <|ACK|>";
            var listenA = serverEndPoint.ListenAsync(cts.Token);
            var writeA = serverEndPoint.WriteAsync(cts.Token);
            var listenB = clientEndPoint.ListenAsync(cts.Token);
            var writeB = clientEndPoint.WriteAsync(cts.Token);

            await communicatorA.SendAsync(
                new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(message)), SocketFlags.None);
            
            await communicatorB.SendAsync(
                new ArraySegment<byte>(System.Text.Encoding.UTF8.GetBytes(messageAcknowledgement)), SocketFlags.None);
            
            await Task.Delay(5000); 

            cts.Cancel(); 

            await Task.WhenAll(listenA, writeA, listenB, writeB);

            
            _output.WriteLine(string.Join(System.Environment.NewLine ,clientUI.Outputs));
            _output.WriteLine(string.Join(System.Environment.NewLine ,serverUI.Outputs));
            
            Assert.Contains(clientUI.Outputs, o => o.Contains("Hello Server"));
            Assert.Contains(serverUI.Outputs, o => o.Contains("<|ACK|>"));
            
        }

        //Flaky test 

        //[Fact]
        ////[Trait("Category", "IntegrationTest")] //Difference from NUnit, XUnit doesn't support class Attribute so every method needs one. 
        //public async Task TestSimpleChatSession()   //TODO: Make both integration unit tests work for now both cannot be run. Integation test are Expected to fail if Settings.json RunMode is "Development" and not "IntegrationTest";
        //{

        //    var fileName = "C:\\Users\\matte\\source\\repos\\ChatBasicApp\\ChatBasicApp\\bin\\Debug\\net5.0\\ChatBasicApp.exe";

        //    var testArg = "IntegrationTest";

        //    var serverStartInfo = new ProcessStartInfo()
        //    {
        //        FileName = fileName,
        //        Arguments = $"server {testArg}",
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //    };

        //    using var serverProcess = new Process { StartInfo = serverStartInfo };
        //    serverProcess.Start();

        //    var clientStartInfo = new ProcessStartInfo()
        //    {
        //        FileName = fileName,
        //        Arguments = $"client {testArg}",
        //        RedirectStandardInput = true,
        //        RedirectStandardOutput = true,
        //        RedirectStandardError = true,
        //        UseShellExecute = false,
        //        CreateNoWindow = true,
        //    };

        //    using var clientProcess = new Process { StartInfo = clientStartInfo };
        //    clientProcess.Start();
        //    clientProcess.BeginErrorReadLine();

        //    // Allow time for client and server to start and connect
        //    await Task.Delay(4000);

        //    using var serverWriter = serverProcess.StandardInput; //automate a user writes something to Console
        //    //using var serverReader = serverProcess.StandardOutput; //automate a user input to Console Console.ReadLine()

        //    // Send test messages to server
        //    await serverWriter.WriteLineAsync("Hello<|EOM|>");
        //    await serverWriter.FlushAsync();
        //    await serverWriter.WriteLineAsync("<|Quit|>");
        //    await serverWriter.FlushAsync();

        //    // Allow some time for message exchange
        //    await Task.Delay(2000);

        //    // Read from client process output, not only server
        //    using var clientReader = clientProcess.StandardOutput;

        //    bool found = false;
        //    //string? line;

        //    // Read few lines to not block forever
        //    for (int i = 0; i < 10; i++)
        //    {
        //        Task<string> messageReadNextLine = clientReader.ReadLineAsync();
        //        //Task<string> messageReadConsoleOutpuLineTask =  clientReader.ReadLineAsync(); //TODO: after 3rd i = 2 debug exits why?? and test runs forever
        //        //Task delaytask = Task.Delay(5000);

        //        //Task completedTask = await Task.WhenAny(messageReadConsoleOutpuLineTask, delaytask);
        //        //line =  await clientReader.ReadLineAsync(); //TODO: after 3rd i = 2 debug exits why?? and test runs forever

        //        // if (completedTask == messageReadConsoleOutpuLineTask)
        //        //{
        //        //  if (completedTask == null) break;

        //        var line = await messageReadNextLine;//await messageReadConsoleOutpuLineTask;
        //        Console.WriteLine("Client output: " + messageReadNextLine);

        //        if (line.Contains("Hello"))
        //        {
        //            found = true;
        //            break;
        //        }
        //        await serverWriter.WriteLineAsync("<|Quit|>");
        //        await serverWriter.FlushAsync();
        //        //}
        //        //else
        //        //{
        //        //    Console.WriteLine("Timeout, Break out of loop ");
        //        //    break;
        //        //}
        //    }

        //    Assert.True(found, "Did not find expected output");

        //    // Cleanup
        //    serverWriter.Close();
        //    //serverReader.Close();
        //    clientReader.Close();

        //    serverProcess.Kill();
        //    clientProcess.Kill();
        //}



    }

}
