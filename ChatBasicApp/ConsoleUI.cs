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

        public void Output(string input, MessageType messageType, bool newLine = true)
        {
            lock (_consolelock)
            {
                if (newLine)
                {
                    Console.WriteLine(input);
                }
                else 
                    Console.Write(input);
            }
        }

       

        public string ReadInput()
        {
            ConsoleKeyInfo input = default; 
            
            lock (_consolelock)
            {
                input = Console.ReadKey(intercept: true);
                
              
            }
            return input.ReturnCodedKey(); 
        }

        public bool HasKey() => Console.KeyAvailable;

        public bool IsConsoleUI() => true;
        

    }
}
