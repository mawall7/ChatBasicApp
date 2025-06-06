using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public interface IChatCommunicator
    {
        public event Action<string> StatusMessage;
        public Task ConnectAsync(IPEndPoint ipEndPoint);
        public void CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType);
        public void Bind(IPEndPoint iPEndPoint);
        public void Listen(int backlog);
        public Task AcceptAsync();
        public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags flags);
        public Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags flags);
        public void Close();
        public void Dispose();
    }
}
