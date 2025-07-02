using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public class ConsoleInputHandler : IInputHandler // TO DO: erase ? this class doesn't do much that ConsoleUI can't do.
    {
       
            public string ReadInput(IUI _ui) 
            {
                string inputresult = null;

                while (inputresult == null)
                {
                    if (_ui.HasKey())
                    {
                        inputresult = _ui.ReadInput();
                    }
                }
                return inputresult;
               
            }
             
    }
}
