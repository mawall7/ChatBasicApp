using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkServer;
namespace ChatBasicApp
{
    public class ConsoleUI : IUI
    {
        private readonly object _consolelock = new object();

        public void Output(string input, MessageType messageType) 
        {
            lock (_consolelock)
            {
                Console.WriteLine(input);
            }
        }

        public string ReadInput()
        {
            string result = "";
            
            lock (_consolelock)
            {
                var input = Console.ReadKey();

                if (input.Key == ConsoleKey.Enter)
                {
                    result = "<|EOM|>";
                }
                if (input.Key == ConsoleKey.Backspace)
                {
                    result = "<BackSpace>";
                }
                if (input.Key == ConsoleKey.F1)
                {
                    result = "<|Quit|>";
                }
                if (input.Key == ConsoleKey.Spacebar)
                {
                    result = " ";
                }

                else if (char.IsLetterOrDigit(input.KeyChar) || char.IsSymbol(input.KeyChar))
                {
                    result = input.KeyChar.ToString();
                }
            }
            return result;
        }

        

    }
}
