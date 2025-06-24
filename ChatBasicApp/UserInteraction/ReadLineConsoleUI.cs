using NetworkServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp.UI
{
    public class ReadLineConsoleUI : IUI
    {
        private readonly object _consolelock = new object();

        public ReadLineConsoleUI()
        {
            // Set AutoFlush = true once when the UI is created
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            });
        }
        public bool HasKey() => true;
        public bool IsConsoleUI() => true;

        public void Output(string input, MessageType messageType)
        {
           
                lock (_consolelock)
                {
                    Console.WriteLine(input);
                }
        }
        
        public string ReadInput()
        {
            lock (_consolelock)
            {
                return Console.ReadLine();
            }
        }
    }
}
