using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public class ChatCommunicator : IChatCommunicator 
    {
        private Socket _socket { get; set; }// is either listnersocket for server or the single socket for a client.

        private Socket _remoteSocket { get; set; }  // is the clientsocket(/s) for server only that can Accepted by it with AcceptAsync

        public event Action<string> StatusMessage;

        public ChatCommunicator()
        {
            //Intentially left blank shall not create socket in constructor
        }

        public void CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new Socket(addressFamily, socketType, protocolType);

        }

        public void Bind(IPEndPoint iPEndPoint) => _socket.Bind(iPEndPoint);

        public void Listen(int backlog) => _socket.Listen(backlog);

        public async Task AcceptAsync() // to do. Timeout for _socket.Async you could pass a cancellation token to this method and then start both the connection task and a waittask and then use Task.WhenAny(task, waittask) , will cancel if connect task isnn't ready before the Task.Wait task.
        {
            
            var connecttask = _socket.AcceptAsync();

            while (!connecttask.IsCompleted)
            {
                StatusMessage.Invoke("Connecting to client");
                await Task.Delay(2000);
            }

            try
            {
                _remoteSocket = await connecttask;  

            }
            catch (Exception)
            {

                throw;
            }

            StatusMessage.Invoke("Connection accepted from client.");

        }


        public async Task ConnectAsync(IPEndPoint ipEndPoint)
        {

            try
            {
                await _socket.ConnectAsync(ipEndPoint);
                StatusMessage.Invoke("Connected succesfully to remote.");
            }

            catch (SocketException e)
            {
                StatusMessage.Invoke("Connection failed: " + e.Message);
                throw;
            }
        }
        public async Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags flags)
        {
            //byte[] receivedFromRemote = new byte[1024];
            var sockettouse = _remoteSocket ?? _socket;
            return await sockettouse.ReceiveAsync(buffer, SocketFlags.None);
        }
        public async Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags flags)
        {
            var sockettouse = _remoteSocket ?? _socket;
            return await sockettouse.SendAsync(buffer, flags);
        }

        public void Close()
        {
            try
            {
                _remoteSocket?.Shutdown(SocketShutdown.Both);
                _remoteSocket?.Close();
                _socket?.Close();
            }
     
            catch (SocketException e)
            {
                StatusMessage?.Invoke($"Error closing socket: {e.Message}");
                
            }
            finally
            {
                _remoteSocket = null;
                _socket = null;
            }
        }

        public void Dispose() 
        {

            try
            {
                _remoteSocket?.Dispose();
                _socket?.Dispose();
            }
            catch (Exception ex)
            {
                StatusMessage?.Invoke($"Error disposing socket: {ex.Message}");
            }
            finally
            {
                _remoteSocket = null;
                _socket = null;
            }
        }


    }
}
