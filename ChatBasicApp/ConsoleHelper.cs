using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public static class ConsoleHelper
    {
        public static void Backspace()
        {
            //if (Console.CursorLeft == 0)
            //{
            //    return;
            //}
            
            Console.Write(' ');
            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
        }
    }

}
