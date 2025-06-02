using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public class ChatCommunicator : ISocketClient, IChatCommunicator //wrapper for Socket
    {
        private Socket _socket {get; set;}

        public event Action<string> StatusMessage;

        public ChatCommunicator()
        {
            //shall not create socket in constructor
        }

        public void CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
        {
            _socket = new Socket(addressFamily, socketType, protocolType);
            
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
            return await _socket.ReceiveAsync(buffer, SocketFlags.None);
        }
        public async Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags flags)
        {
            return await _socket.SendAsync(buffer, flags);
        }

        public void Close() => _socket.Close();

        public void Dispose() 
        {
            if (_socket is not null)
            {
                _socket.Dispose();
                _socket = null;
            }
        }


    }
}
