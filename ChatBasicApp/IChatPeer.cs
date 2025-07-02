using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public interface IChatPeer
    {
        public Task ConnectAsServerAsync();
        public Task ConnectAsClientAsync(CancellationToken token);
        public Task ListenAsync(CancellationToken token);
        public Task WriteAsync(CancellationToken token, IInputProcessor inputProcessor, IUIRenderer renderer, IInputHandler inputHandler);

    }
}
