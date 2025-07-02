using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkServer;
namespace ChatBasicApp
{
    public interface IUI
    {
        public void Output(string input, MessageType messageType, bool newLine = true);

        public string ReadInput();
        public bool HasKey();
        public bool IsConsoleUI();

    }
}

