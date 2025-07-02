using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public static class ConsoleKeyInterpreter // TO DO : make testable. Change ConsoleKeyInfo into IKeyEvent 
    {
        public static string ReturnCodedKey(this ConsoleKeyInfo key) => key.Key switch
        {
            
            ConsoleKey.Enter => "<|EOM|>",
            ConsoleKey.Backspace => "<BackSpace>",
            ConsoleKey.Spacebar => "<Space>",
            ConsoleKey.RightArrow => "<Right>",
            ConsoleKey.LeftArrow => "<Left>",
            ConsoleKey.F1 => "<|Quit|>",
            _ => key.KeyChar.ToString()
        };
    }

    //public interface IKeyInterpreter
    //{
    //    public string ReturnCodedKey(ConsoleKey key);

    //}
}
