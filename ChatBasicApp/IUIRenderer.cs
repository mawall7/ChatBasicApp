using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatBasicApp
{
    public interface IUIRenderer
    {
        public IUI UI { get; set; }
        public void ReRender(string inputresult, StringBuilder WriteBuffer);
    }
}
