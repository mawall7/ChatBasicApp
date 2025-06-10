using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{ 
    //TODO: implement this and implement correct processing (and printing for console). Parly written message should be printed from writebuffer again with a received message, console has to be cleared.  
    public static class ProcessResponse
    {
        public static void CurrentLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);

        }
    }
}
