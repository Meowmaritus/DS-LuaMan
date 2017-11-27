using MeowDSIO.DataTypes.LUAGNL;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DS_LuaMan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public LuaManData LData { get; set; } = new LuaManData();

        public LUABNDRef CurrentLUABNDRef
        {
            get => MainTabs.SelectedItem as LUABNDRef;
        }

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists("Interroot.txt"))
            {
                LData.Interroot = File.ReadAllText("Interroot.txt");
                LData.LUABNDs = LUABND.LoadAllFromInterroot(LData.Interroot, LData.ScriptIncludeListFolder);
                MainTabs.SelectedIndex = 0;
            }
            else
            {
                MessageBox.Show("Please click Browse and browse to your DARKSOULS.exe " + 
                    "(your path is saved automatically for future use so you don't have " + 
                    "to do this every time you open this program).");
            }
        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                AddExtension = false,
                CheckPathExists = true,
                CheckFileExists = true,
                Filter = "Executable files (*.EXE)|*.EXE",
                InitialDirectory = LData.Interroot,
                FileName = "DARKSOULS.exe",
                Title = "Select your DARKSOULS.exe"
            };

            if (dlg.ShowDialog() == true)
            {
                LData.Interroot = new FileInfo(dlg.FileName).Directory.FullName;
                if (File.Exists("Interroot.txt"))
                {
                    File.Delete("Interroot.txt");
                }
                File.WriteAllText("Interroot.txt", LData.Interroot);
            }
        }

        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to load files and lose any unsaved changes?", 
                "Reload?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LData.LUABNDs = LUABND.LoadAllFromInterroot(LData.Interroot, LData.ScriptIncludeListFolder);
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            LUABND.SaveAllToInterroot(LData.Interroot, LData.LUABNDs, LData.ScriptIncludeListFolder);
        }

        private void GoalDataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.V &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {

                // 2-dim array containing clipboard data
                string[][] clipboardData =
                    ((string)Clipboard.GetData(DataFormats.Text)).Split('\n')
                    .Select(row =>
                        row.Split('\t')
                        .Select(cell =>
                            cell.Length > 0 && cell[cell.Length - 1] == '\r' ?
                            cell.Substring(0, cell.Length - 1) : cell).ToArray())
                    .Where(a => a.Any(b => b.Length > 0)).ToArray();

                // the index of the first DataGridRow          
                int startRow = GoalDataGrid.ItemContainerGenerator.IndexFromContainer(
                   (DataGridRow)GoalDataGrid.ItemContainerGenerator.ContainerFromItem
                   (GoalDataGrid.SelectedCells[0].Item));
                int targetRowCount = GoalDataGrid.SelectedCells.Count;

                // the destination rows 
                //  (from startRow to either end or length of clipboard rows)
                DataGridRow[] rows =
                    Enumerable.Range(
                        startRow, Math.Min(GoalDataGrid.Items.Count, targetRowCount))
                    .Select(rowIndex =>
                        GoalDataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow)
                    .Where(a => a != null).ToArray();

                // the destination columns 
                //  (from selected row to either end or max. length of clipboard colums)
                DataGridColumn[] columns =
                    GoalDataGrid.Columns.OrderBy(column => column.DisplayIndex)
                    .SkipWhile(column => column != GoalDataGrid.CurrentCell.Column)
                    .Take(clipboardData.Max(row => row.Length)).ToArray();

                for (int rowIndex = 0; rowIndex < rows.Length; rowIndex++)
                {
                    string[] rowContent = clipboardData[rowIndex % clipboardData.Length];
                    for (int colIndex = 0; colIndex < columns.Length; colIndex++)
                    {
                        string cellContent =
                            colIndex >= rowContent.Length ? "" : rowContent[colIndex];
                        columns[colIndex].OnPastingCellClipboardContent(
                            rows[rowIndex].Item, cellContent);
                    }
                }

            }
        }

        private bool CheckDelete(string thingToDelete, string useDeleteSynonym = "delete")
        {
            return MessageBox.Show($"Are you sure you want to {useDeleteSynonym} {thingToDelete}?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }

        private void GNLDelete_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (GlobalNameListGrid.Items.Count == 1)
            {
                GlobalNameListGrid.SelectedIndex = -1;
            }

            var selectedCount = GlobalNameListGrid.SelectedCells.Count;

            if (selectedCount == 0)
            {
                var selectedItem = GlobalNameListGrid.SelectedItem as StringRef;
                if (CheckDelete($"'{selectedItem.Value}'"))
                {
                    CurrentLUABNDRef.BND.GlobalVariableNames.Remove(GlobalNameListGrid.SelectedItem as StringRef);

                    GlobalNameListGrid.Items.Refresh();
                }
            }
            else
            {
                if (CheckDelete(selectedCount == 1
                        ? $"'{(GlobalNameListGrid.SelectedItems[0] as StringRef).Value}'"
                        : $"the {selectedCount} selected items"))
                {
                    foreach (var c in GlobalNameListGrid.SelectedCells)
                    {
                        CurrentLUABNDRef.BND.GlobalVariableNames.Remove(c.Item as StringRef);
                    }

                    GlobalNameListGrid.Items.Refresh();
                }

            }



            
        }

        private void GNLDelete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = GlobalNameListGrid.SelectedCells.Count > 0;
        }

        private void CustomAiDelete_Execute(object sender, ExecutedRoutedEventArgs e)
        {
            if (CustomAiGrid.Items.Count == 1)
            {
                CustomAiGrid.SelectedIndex = -1;
            }

            var selectedCount = CustomAiGrid.SelectedCells.Count;

            if (selectedCount == 0)
            {
                var selectedItem = CustomAiGrid.SelectedItem as StringRef;
                if (CheckDelete($"'{selectedItem.Value}'", "remove"))
                {
                    CurrentLUABNDRef.BND.CustomAiScriptIncludes.Remove(GlobalNameListGrid.SelectedItem as StringRef);

                    CustomAiGrid.Items.Refresh();
                }
            }
            else
            {
                if (CheckDelete(selectedCount == 1
                        ? $"'{(CustomAiGrid.SelectedItems[0] as StringRef).Value}'"
                        : $"the {selectedCount} selected scripts"))
                {
                    foreach (var c in CustomAiGrid.SelectedCells)
                    {
                        CurrentLUABNDRef.BND.CustomAiScriptIncludes.Remove(c.Item as StringRef);
                    }

                    CustomAiGrid.Items.Refresh();
                }

            }




        }

        private void CustomAiDelete_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = CustomAiGrid.SelectedCells.Count > 0;
        }

        private static void LUAC(string inputFile, string outputFile, Action<string> onError)
        {
            string inputDir = new FileInfo(inputFile).DirectoryName;
            string outputDir = new FileInfo(outputFile).DirectoryName;

            if (!Directory.Exists(inputDir))
                Directory.CreateDirectory(inputDir);

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            var luacProcInfo = new ProcessStartInfo();

            luacProcInfo.FileName = Util.Frankenpath(Environment.CurrentDirectory, @"Resources\luac50.exe");
            luacProcInfo.Arguments = $"-o \"{outputFile}\" \"{inputFile}\"";
            luacProcInfo.CreateNoWindow = true;
            luacProcInfo.UseShellExecute = false;
            luacProcInfo.RedirectStandardError = true;
            luacProcInfo.RedirectStandardOutput = true;
            

            var luacProc = new Process() { StartInfo = luacProcInfo };

            luacProc.Start();

            if (!luacProc.WaitForExit(5000))
            {
                onError("LUAC process stopped responding.");
            }

            string output = luacProc.StandardOutput.ReadToEnd();
            string error = luacProc.StandardError.ReadToEnd();

            if (!string.IsNullOrWhiteSpace(error))
                onError(error);
        }

        private void ImportAllAiScriptsFromLuaBnd(LUABNDRef luaBndRef, ref StringBuilder report)
        {
            int errorCount = 0;

            List<string> scriptsAdded = new List<string>();
            List<string> scriptsUpdated = new List<string>();
            List<string> scriptsFailed = new List<string>();

            foreach (var scriptRef in luaBndRef.BND.CustomAiScriptIncludes)
            {
                var script = scriptRef.Value;
                try
                {
                    if (File.Exists(script))
                    {
                        var scriptShortName = new FileInfo(script).Name;

                        var internalFrpgScriptName = Util.Frankenpath(LUABND.FRPG_AI_DIR, scriptShortName);

                        string compiledScriptFile = Util.Frankenpath(LData.ScriptCompileFolder, scriptShortName);

                        bool errorHappened = false;

                        LUAC(script, compiledScriptFile, err =>
                        {
                            MessageBox.Show($"Error while importing script '{script}' into {luaBndRef.Name}:\n\n{err}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            Dispatcher.Invoke(() => errorHappened = true);
                        });

                        if (!errorHappened)
                        {
                            if (!File.Exists(compiledScriptFile))
                            {
                                MessageBox.Show("Despite LUAC not outputting any error message(s) " +
                                    $"whilst compiling custom script '{scriptShortName}', " +
                                    "no compiled script file was found within the compiled script " +
                                    $"file directory ('{LData.ScriptCompileFolder}').",
                                    "Uncanny Critical Error+10 Occurred",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);

                                scriptsFailed.Add(scriptShortName);
                            }
                            else
                            {
                                byte[] newScriptBytecode = File.ReadAllBytes(compiledScriptFile);

                                bool? addOrUpdate = CurrentLUABNDRef.BND.AddOrUpdateScript(scriptShortName, newScriptBytecode);


                                if (addOrUpdate == true)
                                    scriptsAdded.Add(scriptShortName);
                                else if (addOrUpdate == false)
                                    scriptsUpdated.Add(scriptShortName);
                            }
                        }
                        else
                        {
                            scriptsFailed.Add(scriptShortName);
                        }

                    }
                    else
                    {
                        MessageBox.Show($"Custom script in {luaBndRef.Name}'s import list does not exist: '{script}'",
                            "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while importing custom script '{script}' into {luaBndRef.Name}:\n\n" + ex.Message);
                    errorCount++;
                }
            }

            bool didNothing_trashcat = true;

            if (scriptsAdded.Count > 0)
            {
                report.AppendLine($"Added {scriptsAdded.Count} new AI script(s) to {luaBndRef.Name}:");
                foreach (var s in scriptsAdded)
                    report.AppendLine("\t" + s);
                report.AppendLine();
                didNothing_trashcat = false;
            }

            if (scriptsUpdated.Count > 0)
            {
                report.AppendLine($"Imported changes to {scriptsUpdated.Count} AI script(s) in {luaBndRef.Name}:");
                foreach (var s in scriptsUpdated)
                    report.AppendLine("\t" + s);
                report.AppendLine();
                didNothing_trashcat = false;
            }

            if (scriptsFailed.Count > 0)
            {
                report.AppendLine($"Failed to compile {scriptsFailed.Count} AI script(s) meant to be imported into {luaBndRef.Name}:");
                foreach (var s in scriptsFailed)
                    report.AppendLine("\t" + s);
                report.AppendLine();
                report.AppendLine("\tNOTE: Because the script(s) failed to compile, it is unknown whether there were any new changes to import or not!");
                report.AppendLine();
                didNothing_trashcat = false;
            }

            if (didNothing_trashcat)
            {
                report.AppendLine($"No scripts added or changed in {luaBndRef.Name}.luabnd; already up-to-date.");
            }
        }

        private void ButtonImportAiAll_Click(object sender, RoutedEventArgs e)
        {
            var dlg = MessageBox.Show("Import the custom AI scripts in all of the LUABNDs to their respective LUABNDs?", 
                "Import All?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dlg == MessageBoxResult.Yes)
            {
                var report = new StringBuilder();

                foreach (var luaBndRef in LData.LUABNDs)
                {
                    ImportAllAiScriptsFromLuaBnd(luaBndRef, ref report);
                }

                ScriptDataGrid.Items.Refresh();

                MessageBox.Show(report.ToString(), "Import Summary");
            }

        }

        private void ButtonImportAiCurrent_Click(object sender, RoutedEventArgs e)
        {
            var dlg = MessageBox.Show($"Import the custom AI scripts listed into {CurrentLUABNDRef.Name}.luabnd?",
                "Import?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (dlg == MessageBoxResult.Yes)
            {
                var report = new StringBuilder();
                ImportAllAiScriptsFromLuaBnd(CurrentLUABNDRef, ref report);

                ScriptDataGrid.Items.Refresh();

                MessageBox.Show(report.ToString(), "Import Summary");
            }

            
        }

        private void ButtonAiImportHelp_Click(object sender, RoutedEventArgs e)
        {
            var help = new StringBuilder();

            help.AppendLine("The Custom AI Import List is simply a list of files from anywhere");
            help.AppendLine("on your device which are to be imported into the LUABND for use in-game.");
            help.AppendLine();
            help.AppendLine("Drag and drop files onto the list to REQUEST that they be imported.");
            help.AppendLine();
            help.AppendLine("To actually IMPORT the scripts in the list, you must click either");
            help.AppendLine("of the import buttons below the list. Both of the buttons do the");
            help.AppendLine("same thing except the 'Import All' one imports the files listed in");
            help.AppendLine("each LUABND's import list, one-by-one, the same as if you'd clicked");
            help.AppendLine("the regular import button on each one of them sequentially.");
            help.AppendLine();
            help.AppendLine("After doing an import operation, you will be shown a summary of what");
            help.AppendLine("all scripts got newly-added and/or updated as well as any errors that");
            help.AppendLine("may have been encountered during the operation.");
            help.AppendLine();
            help.AppendLine("Also: to remove a script from this list, highlight it and press the Delete key.");

            MessageBox.Show(help.ToString(), "Info on Custom Script List");
        }

        private void CustomAiGrid_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach (var v in files)
                {
                    CurrentLUABNDRef.BND.CustomAiScriptIncludes.Add(new StringRef() { Value = v });
                }

                CustomAiGrid.Items.Refresh();
            }
            catch
            {

            }
            
        }
    }
}
