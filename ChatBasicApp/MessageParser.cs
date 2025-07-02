using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public static class MessageParser
    {
        private readonly static HashSet<string> ConsoleCommands = new()
        { 
            "<Space>", "<BackSpace>","<Right>","<Left>" 
        };
        public static bool IsQuit(string input) => input.Contains("<|Quit|>");
        public static bool IsEOM(string input) => input.Contains("<|EOM|>");
        public static bool IsPrint(string input) => input.Contains("<|PRINT|>");
        public static bool IsRenderCommand(string input) => ConsoleCommands.Contains(input);
 
    }
}
           
            
           
