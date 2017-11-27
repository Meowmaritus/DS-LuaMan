using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS_LuaMan
{
    public class LUABNDRef
    {
        public string Name { get; set; } = null;
        public LUABND BND { get; set; } = new LUABND();

        public LUABNDRef(string Name, LUABND BND)
        {
            this.Name = Name;
            this.BND = BND;
        }
    }
}
