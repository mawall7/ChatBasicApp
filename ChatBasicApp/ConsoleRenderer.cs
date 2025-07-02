using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public class ConsoleRenderer : IUIRenderer
    {
        public IUI UI { get; set;}

        public void ReRender(string inputresult, StringBuilder WriteBuffer)
        {
            var CursorPos = Console.GetCursorPosition();

            if (inputresult == "<Space>") //TODO  fix bug writing a letter and then space adds characters to the end
            {
                CursorPos = Console.GetCursorPosition();

                WriteBuffer = WriteBuffer.Insert(CursorPos.Left, " ");
                Console.SetCursorPosition(CursorPos.Left + 1, CursorPos.Top);
                CursorPos = Console.GetCursorPosition();
                DeleteCurrentConsoleLine();
                Console.Write(WriteBuffer.ToString());
                Console.SetCursorPosition(CursorPos.Left, CursorPos.Top);
                return;
            }

            else if (inputresult == "<BackSpace>")
            {

                WriteBuffer = WriteBuffer.Remove(CursorPos.Left - 1, 1);
                DeleteCurrentConsoleLine();
                Console.Write(WriteBuffer.ToString());
                Console.SetCursorPosition(CursorPos.Left - 1, CursorPos.Top);
                return;

            }


            else if (inputresult == "<Left>")
            {
                Console.SetCursorPosition(CursorPos.Left - 1, CursorPos.Top);
                return;
            }
            else if (inputresult == "<Right>")
            {
                Console.SetCursorPosition(CursorPos.Left + 1, CursorPos.Top);
                return;
            }
            else 
            {
                
                Console.Write(inputresult);
            }

        }

        public static void DeleteCurrentConsoleLine()
        {
            int currentLine = Console.CursorTop;
            Console.SetCursorPosition(0, currentLine);
            Console.Write(new string(' ', Console.WindowWidth)); // Overwrite the whole line with spaces
            Console.SetCursorPosition(0, currentLine); // Reset cursor to start
        }
    }
}
