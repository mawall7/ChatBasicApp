using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using System.Threading.Tasks;
using ChatBasicApp;
public class InMemoryChatCommunicator : IChatCommunicator
{
    public InMemoryChatCommunicator _peer { get; private set; }
    private readonly Channel<byte[]> _inboundChannel; //que 
    private bool _isConnected = false;

    public event Action<string> StatusMessage;

    public InMemoryChatCommunicator()
    {
        _inboundChannel = Channel.CreateUnbounded<byte[]>();
    }

    // For connecting two peers
    public InMemoryChatCommunicator ConnectToPeer(InMemoryChatCommunicator peer)
    {
        _peer = peer;
        _isConnected = true;
        peer._peer = this;
        peer._isConnected = true;
        return this;
    }

    public void CreateSocket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
    {
        // No-op in memory
    }

    public void Bind(EndPoint localEndPoint)
    {
        // No-op in memory
    }

    public void Listen(int backlog)
    {
        // No-op in memory
    }

    public Task AcceptAsync()
    {
        if (!_isConnected)
            throw new InvalidOperationException("Peer not connected yet.");
        return Task.CompletedTask;
    }

    public Task ConnectAsync(EndPoint remoteEndPoint)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Peer not connected yet.");
        return Task.CompletedTask;
    }

    public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
    {
        if (!_isConnected || _peer == null)
            throw new InvalidOperationException("Peer not connected.");

        // Copy buffer data
        byte[] data = new byte[buffer.Count];
        Buffer.BlockCopy(buffer.Array, buffer.Offset, data, 0, buffer.Count);

        // Simulate sending to peer's inbound channel
        _peer._inboundChannel.Writer.TryWrite(data);
        return Task.FromResult(buffer.Count);
    }

    public async Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
    {
        if (!_isConnected)
            throw new InvalidOperationException("Not connected.");

        byte[] received = await _inboundChannel.Reader.ReadAsync();

        int bytesToCopy = Math.Min(received.Length, buffer.Count);
        Buffer.BlockCopy(received, 0, buffer.Array, buffer.Offset, bytesToCopy);
        return bytesToCopy;
    }

    public void Dispose()
    {
        _inboundChannel.Writer.TryComplete();
    }

    public Task ConnectAsync(IPEndPoint ipEndPoint)
    {
        throw new NotImplementedException();
    }

    
    public void Bind(IPEndPoint iPEndPoint)
    {
        throw new NotImplementedException();
    }

    Task IChatCommunicator.AcceptAsync()
    {
        throw new NotImplementedException();
    }

    
    public void Close()
    {
        throw new NotImplementedException();
    }
}
