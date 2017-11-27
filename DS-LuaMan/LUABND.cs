using MeowDSIO;
using MeowDSIO.DataFiles;
using MeowDSIO.DataTypes.BND3;
using MeowDSIO.DataTypes.LUAGNL;
using MeowDSIO.DataTypes.LUAINFO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DS_LuaMan
{
    public class LUABND
    {
        private BND3Header header;
        private LUAGNL GNL = null;
        private LUAINFO INFO = null;

        public List<Goal> Goals
        {
            get => INFO?.Goals;
            set
            {
                if (INFO != null)
                    INFO.Goals = value;
            }
        }

        public List<StringRef> GlobalVariableNames
        {
            get => GNL?.GlobalVariableNames;
            set
            {
                if (GNL != null)
                    GNL.GlobalVariableNames = value;
            }
        }

        public List<ScriptRef> Scripts { get; set; } = new List<ScriptRef>();

        public List<StringRef> CustomAiScriptIncludes { get; set; } = new List<StringRef>();

        //True if new entry added, false if existing entry updated, null if existing entry already had the same bytecode.
        public bool? AddOrUpdateScript(string scriptShortName, byte[] bytecode)
        {
            int existingScriptIndex = Scripts.FindIndex(x => x.Name.ToUpper() == scriptShortName.ToUpper());

            if (existingScriptIndex >= 0)
            {
                if (Scripts[existingScriptIndex].Bytecode.SequenceEqual(bytecode))
                {
                    return null;
                }
                else
                {
                    Scripts[existingScriptIndex].Bytecode = bytecode;
                    return false;
                }
            }
            else
            {
                Scripts.Add(new ScriptRef(scriptShortName, bytecode));
                return true;
            }
        }

        public static class ID
        {
            public const int ScriptListStart = 1000;
            public const int GNL = 1000000;
            public const int INFO = 1000001;
        }

        public const string FRPG_SCRIPT_DIR = @"N:\FRPG\data\INTERROOT_win32\script\";
        public const string FRPG_AI_DIR = FRPG_SCRIPT_DIR + @"ai\out\bin\";

        public static void SaveToInterroot(LUABND luabnd, string interrootDir, string luaBndName, string customImportPath)
        {
            var newBnd = new BND3() { Header = luabnd.header };

            int currentScriptID = ID.ScriptListStart;

            foreach (var scriptRef in luabnd.Scripts)
            {
                newBnd.Entries.Add(new BND3Entry(currentScriptID++, scriptRef.Name, null, scriptRef.Bytecode));
            }

            if (luabnd.GNL != null)
                newBnd.Entries.Add(new BND3Entry(ID.GNL, Util.Frankenpath(FRPG_SCRIPT_DIR, $@"{luaBndName}.luagnl"), null, DataFile.SaveAsBytes(luabnd.GNL)));

            if (luabnd.INFO != null)
                newBnd.Entries.Add(new BND3Entry(ID.INFO, Util.Frankenpath(FRPG_SCRIPT_DIR, $@"{luaBndName}.luainfo"), null, DataFile.SaveAsBytes(luabnd.INFO)));

            DataFile.SaveToFile(newBnd, Util.Frankenpath(interrootDir, $@"script\{luaBndName}.luabnd"));

            SaveCustomScriptList(customImportPath, luaBndName, luabnd.CustomAiScriptIncludes);
        }

        private static List<StringRef> LoadCustomScriptList(string customImportPath, string luaBndName)
        {
            if (!Directory.Exists(customImportPath))
                Directory.CreateDirectory(customImportPath);

            return File
                .ReadAllLines(Util.Frankenpath(customImportPath, luaBndName + ".scriptlist"))
                .Select(x => new StringRef() { Value = x })
                .ToList();
        }

        private static void SaveCustomScriptList(string customImportPath, string luaBndName, List<StringRef> customImportList)
        {
            if (!Directory.Exists(customImportPath))
                Directory.CreateDirectory(customImportPath);

            File.WriteAllLines(Util.Frankenpath(customImportPath, luaBndName + ".scriptlist"), 
                customImportList
                .Select(x => x.Value)
                .ToArray());
        }

        public static LUABND LoadFromInterroot(string interrootDir, string luaBndName, string customImportPath)
        {
            string fileName = Util.Frankenpath(interrootDir, $@"script\{luaBndName}.luabnd");

            var result = new LUABND();

            using (var bndFile = DataFile.LoadFromFile<BND3>(fileName))
            {
                result.header = bndFile.Header;

                result.Scripts.Clear();

                foreach (var entry in bndFile)
                {
                    if (entry.ID == ID.GNL)
                    {
                        result.GNL = entry.ReadDataAs<LUAGNL>();
                    }
                    else if (entry.ID == ID.INFO)
                    {
                        result.INFO = entry.ReadDataAs<LUAINFO>();
                    }
                    else
                    {
                        result.Scripts.Add(new ScriptRef(entry.Name, entry.GetBytes()));
                    }
                }
            }

            string customImportListFileName = Util.Frankenpath(customImportPath, luaBndName + ".scriptlist");

            if (!File.Exists(customImportListFileName))
            {
                result.CustomAiScriptIncludes = new List<StringRef>();
                SaveCustomScriptList(customImportPath, luaBndName, result.CustomAiScriptIncludes);
            }
            else
            {
                result.CustomAiScriptIncludes = LoadCustomScriptList(customImportPath, luaBndName);
            }

            return result;
        }


        public static List<LUABNDRef> LoadAllFromInterroot(string interrootDir, string customImportPath)
        {
            var result = new List<LUABNDRef>();
            foreach (var luabndFullPath in Directory.GetFiles(interrootDir.Trim('\\') + "\\script\\", "*.luabnd", SearchOption.TopDirectoryOnly))
            {
                string luabnd = luabndFullPath.Substring(luabndFullPath.LastIndexOf(Path.DirectorySeparatorChar) + 1);
                luabnd = luabnd.Substring(0, luabnd.LastIndexOf(".luabnd"));
                result.Add(new LUABNDRef(luabnd, LoadFromInterroot(interrootDir, luabnd, customImportPath)));
            }
            return result;
        }

        public static void SaveAllToInterroot(string interrootDir, List<LUABNDRef> bnds, string customImportPath)
        {
            int errorCount = 0;

            foreach (var bndRef in bnds)
            {
                try
                {
                    SaveToInterroot(bndRef.BND, interrootDir, bndRef.Name, customImportPath);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Error while saving {bndRef.Name}.luabnd:\n\n" + e.Message);
                    errorCount++;
                }
            }
            

            MessageBox.Show($"Finished saving LUABNDs with {errorCount} error(s).\n\nThis message is a placeholder until a proper " +
                "save-and-then-the-save-button-gets-grayed-out-until-you-have-unsaved-changes system is implemented.");
        }
    }
}
