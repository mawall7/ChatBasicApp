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
        public void Output(string input, MessageType messageType) 
        {
            Console.WriteLine(input);
        }

        public string ReadInput()
        {
            
            var input = Console.ReadKey();
            string result = "";

            if(input.Key == ConsoleKey.Enter)
            {
                result = "<|EOM|>"; 
            }
            if(input.Key == ConsoleKey.Backspace)
            {
                result = "<BackSpace>";
            }
            if (input.Key == ConsoleKey.F1)
            {
                result = "<|Quit|>";
            }
            if(input.Key == ConsoleKey.Spacebar)
            {
                result = " ";
            }

            else if(char.IsLetterOrDigit(input.KeyChar ) || char.IsSymbol(input.KeyChar))
            {
                result = input.KeyChar.ToString();
            }
            return result;
        }

        

    }
}
