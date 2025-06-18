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
    public class Tests //TO DO: Add more tests!// Add tests that exercise both client and server behavior via the new ChatPeer class.

/* Test edge cases:

Can it connect as server?

Can it connect as client?

Can it send and receive messages?

What happens on disconnect?

What happens on failed connections?

If possible, run integration tests where you have multiple ChatPeer instances talking to each other*/
//TODO Write Send when IUI is not Console
    {
        private Mock<IUI> mockUI;
        private ChatPeer client;
        private Mock<IChatCommunicator> mockcommunicator;
        
        
        [SetUp]
        public void Setup()
        {
           
            IPEndPoint ipendpoint = new(IPAddress.Parse("127.0.0.1"), 8081);
            mockUI = new Mock<IUI>();

            mockcommunicator = new Mock<IChatCommunicator>();
            
            mockUI.Setup(mock => mock.Output(It.IsAny<string>(), It.IsAny<MessageType>()));
                
            client = new ChatBasicApp.ChatPeer(ipendpoint, mockUI.Object, mockcommunicator.Object);
            
        }


        [Test]

        public async Task ConnectedAsServer_ShouldCreateBindListenAndAccept() // simpy test that the methods are called
        {
            var mockCommunicator = new Mock<IChatCommunicator>();
            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockCommunicator.Object);

            mockCommunicator.Setup(c => c.AcceptAsync()).Returns(Task.CompletedTask);

            await chatPeer.ConnectAsServerAsync();

            mockCommunicator.Verify(c => c.CreateSocket(It.IsAny<AddressFamily>(), It.IsAny<SocketType>(), It.IsAny<ProtocolType>()), Times.Once);
            mockCommunicator.Verify(c => c.Bind(fakeEndPoint), Times.Once);
            mockCommunicator.Verify(c => c.Listen(100), Times.Once);
            mockCommunicator.Verify(c => c.AcceptAsync(), Times.Once);

        }

        [Test]

        public async Task ConnectAsClient_ShouldCreateAndAccept() // simpy test that the methods are called
        {
            var mockCommunicator = new Mock<IChatCommunicator>();
            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockCommunicator.Object);

            mockCommunicator.Setup(c => c.ConnectAsync(fakeEndPoint)).Returns(Task.CompletedTask);

            await chatPeer.ConnectAsClientAsync(new CancellationToken());

            mockCommunicator.Verify(c => c.CreateSocket(It.IsAny<AddressFamily>(), It.IsAny<SocketType>(), It.IsAny<ProtocolType>()), Times.Once);
            mockCommunicator.Verify(c => c.ConnectAsync(fakeEndPoint), Times.Once);

        }


        [Test]
        public async Task ConnectAsClient_ShouldNotCallAcceptAsync()
        {
            var mockCommunicator = new Mock<IChatCommunicator>();
            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockCommunicator.Object);

            mockCommunicator.Setup(c => c.ConnectAsync(fakeEndPoint)).Returns(Task.CompletedTask);

            await chatPeer.ConnectAsClientAsync(new CancellationToken());
            mockCommunicator.Verify(c => c.AcceptAsync(), Times.Never);
        }
        
        //test messagesending
        [Test]
        public async Task ChatPeer_ShouldCallSendAsyncWhenSendingMessage() //TODO: make test ending task with cacellation token.
        {
            mockUI.Setup(c => c.HasKey())
                .Returns(true);
                 
            
            mockUI.Setup(c => c.IsConsoleUI()).Returns(true);
            mockUI.Setup(c => c.ReadInput()).Returns("test read input messsage<|EOM|>");
            mockcommunicator.SetupSequence(mock => mock.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>())).ReturnsAsync(123)
                .Throws(new SocketException());
            

            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockcommunicator.Object);

            var cTS = new CancellationTokenSource();
            var token = cTS.Token;

            var writetask = chatPeer.WriteAsync(token);
            //cTS.Cancel(); TODO: not working properly
            await writetask;
            
            mockcommunicator.Verify(c => c.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()), Times.Exactly(2));
            
        }

        [Test]
        public async Task ChatPeer_ListenShouldReceiveMessageFromChatCommunicator()
        {
            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockcommunicator.Object);

            mockcommunicator.SetupSequence(c => c.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()))
                .ReturnsAsync(123)
                .Throws(new SocketException());
            
            var cTS = new CancellationTokenSource();
            var cT = cTS.Token;
            await chatPeer.ListenAsync(cT);

            mockcommunicator.Verify(c => c.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()), Times.Exactly(2));
                
        }
        
        [Test]
        public async Task ChatPeer_ListenAsyncShouldEndOnCancellationRequested()
        {
            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockcommunicator.Object);

            var receiveCalled = new TaskCompletionSource<bool>();

            mockcommunicator.Setup(c => c.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()))
                .Returns(() => Task.FromResult(123))
                .Callback(() => receiveCalled.TrySetResult(true));

            var cTS = new CancellationTokenSource();
            var cT = cTS.Token;
            
            var task = chatPeer.ListenAsync(cT);
            await receiveCalled.Task; //await Task.Delay(100);
            cTS.Cancel();
            
            await task;
   
            mockcommunicator.Verify(c => c.ReceiveAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()), Times.AtLeastOnce());
        }

        [Test]
        public async Task ChatPeer_WriteAsyncShouldEndOnCancellationRequested() //TOOO : cancellation, problem cancellationRequested is never true;
         {
            var fakeEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);
            var chatPeer = new ChatPeer(fakeEndPoint, mockUI.Object, mockcommunicator.Object);

            mockUI.Setup(c => c.HasKey()).Returns(true);
            mockUI.Setup(c => c.IsConsoleUI()).Returns(true);
            string fakeInput = "Test From Write<|EOM|>";
            mockUI.Setup(c => c.ReadInput()).Returns(fakeInput);

            var sendCalled = new TaskCompletionSource<bool>();

            mockcommunicator.Setup(c => c.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()))
                .Returns(() => Task.FromResult(123))
                .Callback(() => sendCalled.TrySetResult(true));

            var cTS = new CancellationTokenSource();
            var cT = cTS.Token;

            var task = chatPeer.WriteAsync(cT);
            await sendCalled.Task; //await Task.Delay(100);
            cTS.Cancel();

            //Assert.ThrowsAsync<OperationCanceledException>(async () => await task);
            await task;

            mockcommunicator.Verify(c => c.SendAsync(It.IsAny<ArraySegment<byte>>(), It.IsAny<SocketFlags>()), Times.AtLeastOnce());
        }



        [Test]
        public async Task ListenShouldOutputSocketExceptionWhenMockCommunicatorSocketExceptionIsThrown()
        {
            mockcommunicator.Setup(mock => mock.ReceiveAsync(It.IsAny<System.ArraySegment<byte>>(), SocketFlags.None))
                .ThrowsAsync(new SocketException());

            await client.ListenAsync(new CancellationTokenSource().Token); 

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

            mockcommunicator.Setup(mock => mock.ReceiveAsync(It.IsAny<System.ArraySegment<byte>>(), SocketFlags.None))
               .ThrowsAsync(new SocketException());

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