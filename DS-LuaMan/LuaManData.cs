using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DS_LuaMan
{
    public class LuaManData : INotifyPropertyChanged
    {
        private string _interroot = Environment.CurrentDirectory;

        public string Interroot
        {
            get => _interroot;
            set
            {
                _interroot = value;
                NotifyPropertyChanged(nameof(Interroot));
                NotifyPropertyChanged(nameof(ScriptLoadFolder));
                NotifyPropertyChanged(nameof(ScriptIncludeListFolder));
                NotifyPropertyChanged(nameof(ScriptCompileFolder));
            }
        }

        //NotifyPropertyChanged handled in Interroot
        public string ScriptLoadFolder => Util.Frankenpath(Interroot, @"\script\ai\out\");

        //NotifyPropertyChanged handled in Interroot
        public string ScriptIncludeListFolder => Util.Frankenpath(Interroot, @"\script\DSLuaManIncludeLists\");

        //NotifyPropertyChanged handled in Interroot
        public string ScriptCompileFolder => Util.Frankenpath(Interroot, @"\script\ai\out\bin\");

        private List<LUABNDRef> _luabnds = new List<LUABNDRef>();

        public List<LUABNDRef> LUABNDs
        {
            get => _luabnds;
            set
            {
                _luabnds = value;
                NotifyPropertyChanged(nameof(LUABNDs));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
