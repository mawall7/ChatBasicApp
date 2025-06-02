using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ChatBasicApp
{
    public interface ISocketClient
    {
        public event Action<string> StatusMessage; 

        public Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags flags);
        public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags flags);
        public void CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);
        Task ConnectAsync(IPEndPoint ipEndpoint);
        void Close();
        void Dispose();

    }
}


