using NUnit.Framework;
using ChatBasicApp;
using System.Net;
using Moq;
using NetworkServer;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System;

namespace ChatBasicApp_Tests
{
    public class Tests
    {
        private Mock<IUI> mockUI;
        private Client client;
        private Mock<IChatCommunicator> mockcommunicator;
        //private Mock<ISocketClient> mocksocket;
        
        [SetUp]
        public void Setup()
        {
           
            IPEndPoint ipendpoint = new(IPAddress.Parse("127.0.0.1"), 8081);
            mockUI = new Mock<IUI>();
            //mocksocket = new Mock<ISocketClient>();
            mockcommunicator = new Mock<IChatCommunicator>();
            
            mockUI.Setup(mock => mock.Output(It.IsAny<string>(), It.IsAny<MessageType>()));


            mockcommunicator.Setup(mock => mock.ReceiveAsync(It.IsAny<System.ArraySegment<byte>>(), SocketFlags.None))
                .ThrowsAsync(new SocketException());
                
            client = new ChatBasicApp.Client(ipendpoint, mockUI.Object, mockcommunicator.Object);
            
        }

        [Test]
        public async Task ListenShouldOutputSocketExceptionWhenMockCommunicatorSocketExceptionIsThrown()
        {
        
            await client.Listen(new CancellationTokenSource().Token); 

            mockUI.Verify(
                ui => ui.Output(
                    It.Is<string>(s => s.Contains("ungracefully")),
                    MessageType.Error
                    ),
                    Times.AtLeast(1)
            );
        }

        [Test]
        public async Task AppShouldStartAsClientWithoutErrors() //just reaches while loop at Connect if no connection is made from remote it has no timeout at the moment
        {
            var args = new string[] {"client"};

                var runtask = ChatBasicApp.ChatApplication.Run(args, mockUI.Object);
                await Task.Delay(5000);
                ChatBasicApp.ChatApplication.cts.Cancel();

            try
            {
                await runtask;
            }
            catch (OperationCanceledException)
            {
                // This is expected — ignore
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception: {ex.Message}");
            }

            //Assert.DoesNotThrowAsync(async () =>
            //   {
            //       await runtask;
            //   });

            //Assert.That(async () => await ChatBasicApp.ChatApplication.Run(args, mockUI.Object), Throws.Exception);
        }
    }
}