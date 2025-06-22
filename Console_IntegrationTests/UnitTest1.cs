using System;
using Xunit;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Console_IntegrationTests
{
    /* example of test setup :
     * [TestClass]
 [Category("Integration")]
 public class MyIntegrationTest
 {
     private MyApp app;

     [TestInitialize]
     public void Setup()
     {
         app = new MyApp(new ConsoleLineInput()); // inject real behavior for integration
     }
 }*/
    public class UnitTest1
    {

        [Fact]
        [Trait("Category", "Integration")] 
        public async Task TestSimpleChatSession()   //TODO: Make both integration unit tests work for now both cannot be run. Integation test are Expected to fail if Settings.json RunMode is "Development" and not "IntegrationTest";
        {
            var fileName = "C:\\Users\\matte\\source\\repos\\ChatBasicApp\\ChatBasicApp\\bin\\Debug\\net5.0\\ChatBasicApp.exe";

            var serverStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = "server",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var serverProcess = new Process { StartInfo = serverStartInfo };
            serverProcess.Start();

            var clientStartInfo = new ProcessStartInfo()
            {
                FileName = fileName,
                Arguments = "client",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var clientProcess = new Process { StartInfo = clientStartInfo };
            clientProcess.Start();

            // Allow time for client and server to start and connect
            await Task.Delay(2000);

            using var serverWriter = serverProcess.StandardInput;
            using var serverReader = serverProcess.StandardOutput;

            // Send test messages to server
            await serverWriter.WriteLineAsync("Hello<|EOM|>");
            await serverWriter.FlushAsync();

            // Allow some time for message exchange
            await Task.Delay(1000);

            // Read from client process output, not only server
            using var clientReader = clientProcess.StandardOutput;

            bool found = false;
            string? line;

            // Read few lines to not block forever
            for (int i = 0; i < 10; i++)
            {
                line = await clientReader.ReadLineAsync();
                if (line == null) break;

                Console.WriteLine("Client output: " + line);
                if (line.Contains("Hello"))
                {
                    found = true;
                    break;
                }
            }

            Assert.True(found, "Did not find expected output");

            // Cleanup
            serverWriter.Close();
            serverProcess.Kill();
            clientProcess.Kill();
        }
    }
}
