using ChatBasicApp;
using NetworkServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Console_IntegrationTests
{
    public class TestUI : IUI
    {
        public List<string> Outputs { get; } = new List<string>();
        public void Output(string message, MessageType type) => Outputs.Add(message);
        public bool HasKey() => false;
        public string ReadInput() => "";
        public bool IsConsoleUI() => false;
    }

}
