using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public interface IInputProcessor
    {
        public Task HandleInputAsync(string inputresult, StringBuilder WriteBuffer); //TODO : possible to discard
        public Task ProcessFullMessage(string inputresult, StringBuilder WriteBuffer);
        public Task ProcessPrintMessage(string inputresult, StringBuilder WriteBuffer);


    }
}

