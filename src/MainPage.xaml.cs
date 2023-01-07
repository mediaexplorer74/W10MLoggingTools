// MainPage

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Telnet;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ndtklib;


// Logging_Enabler
namespace Logging_Enabler
{
    // MainPage class
    public sealed partial class MainPage : Page
    {

        string LocalPath = ApplicationData.Current.LocalFolder.Path;
        static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        TelnetClient client = new TelnetClient(TimeSpan.FromSeconds(1), cancellationTokenSource.Token);
        bool IsBootlogEnabled;
        bool IsUefiLogEnabled;
        NRPC rpc = new NRPC();

        // MainPage
        public MainPage()
        {
            this.InitializeComponent();
            
            AppBusy(true);

            MainWindowPivot.IsEnabled = false;
            
                        
            HomeText.Text = "Welcome, this app will let you configure basic logging settings for this device";
            try
            {
                rpc.Initialize();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception: " + ex.Message);

                Exceptions.CustomMessage("Error initializing Interop Capabilities");
            }

            //RnD it
            CheckLoggingStatus();

            AppBusy(false);
        }

        /// <summary>
        /// Check all the values for each logging option
        /// </summary>
        public async void CheckLoggingStatus()
        {
            try
            {
                //await 
                    ApplicationData.Current.LocalFolder.CreateFileAsync("cmdstring.txt",
                    CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception 1: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            try
            {
                //await 
                    ApplicationData.Current.LocalFolder.CreateFileAsync("ntbtlog.txt",
                    CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception 2: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            try
            {
                //await 
                    ApplicationData.Current.LocalFolder.CreateFileAsync("ImgUpd.log", 
                    CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception 3: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            try
            {
                //await 
                    ApplicationData.Current.LocalFolder.CreateFileAsync("ImgUpd.log.cbs.log", 
                    CreationCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception 4: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            try
            {
                await client.Connect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception C: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            // Boot logging check
            try
            {
                await 
                client.Send("bcdedit /enum {default} > " + $"\"{LocalPath}\\cmdstring.txt\"");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception 5: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            //RnD
            await Task.Delay(2000);

            string results = "";

            try
            {
                results = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception 6: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            if (results.Contains("bootlog                 Yes"))
            {
                try
                {
                    rpc.FileCopy(@"C:\Windows\ntbtlog.txt", $"{LocalPath}\\ntbtlog.txt", 0);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ex] Exception 7: " + ex.Message);
                    Exceptions.ThrowFullError(ex);
                    return;
                }

                IsBootlogEnabled = true;
                BootLogTog.IsOn = true;
                SaveLogBtn.IsEnabled = true;
                ViewLogBtn.IsEnabled = true;
            }
            else
            {
                IsBootlogEnabled = false;
                BootLogTog.IsOn = false;
                SaveLogBtn.IsEnabled = false;
                ViewLogBtn.IsEnabled = false;
            }

            // UEFI logging check
            //await 
            client.Send("if exist \"C:\\EFIESP\\Windows\\System32\\Boot\\UEFIChargingLogToDisplay.txt\" echo EXISTS > " + $"\"{LocalPath}\\cmdstring.txt\" 2>&1");

            await Task.Delay(2000);

            string results2 = "";
            try
            {
                results2 = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception A: "+ ex.Message);
                Exceptions.ThrowFullError(ex);
                return;
            }

            if (results2.Contains("EXISTS"))
            {
                UefiTog.IsOn = true;
                IsUefiLogEnabled = true;
            }
            else
            {
                UefiTog.IsOn = false;
                IsUefiLogEnabled = false;
            }

            try
            {
                string dumpfolder;

                //await 

                client.Send("reg query \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" > " + $"{LocalPath}\\cmdstring.txt");

                string localDumpsKey = "";

                await Task.Delay(2000);

                try
                {
                    localDumpsKey = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                }
                catch (Exception ex2)
                {
                    Debug.WriteLine("[ex] Exception D: " + ex2.Message);
                    Exceptions.ThrowFullError(ex2);
                    return;
                }

                // Check if the system was unable to find the specified registry key or value...
                if (localDumpsKey.Contains("ERROR: The system was unable to find the specified registry key or value."))
                {
                    // * Error *
                   dumpfolder = "";                    
                }
                else
                {
                    // * Ok *
                    //await 
                    client.Send("reg query \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" /v DumpType > " + $"{LocalPath}\\cmdstring.txt");

                    await Task.Delay(2000);

                    string dumptypeResult = File.ReadAllText($"{LocalPath}\\cmdstring.txt");

                    if (dumptypeResult.Contains("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps"))
                    {
                        string tempname = dumptypeResult.Replace("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "");
                        string tempname2 = tempname.Replace("DumpType    REG_DWORD", "");
                        string tempresult = Regex.Replace(tempname2, @"\s+", "");
                        switch (tempresult)
                        {
                            case "0x0":
                                DumpTypeCombo.SelectedIndex = 0;
                                break;
                            case "0x1":
                                DumpTypeCombo.SelectedIndex = 1;
                                break;
                            case "0x2":
                                DumpTypeCombo.SelectedIndex = 2;
                                break;
                            default:
                                DumpTypeCombo.SelectedIndex = 0;
                                break;
                        }

                    }
                    else
                    {
                        DumpTypeCombo.SelectedIndex = 0;
                    }


                   //await 
                   client.Send("reg query \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" /v DumpCount > " + $"{LocalPath}\\cmdstring.txt");

                   string dumpcountResult = "";

                   await Task.Delay(2000);

                   try
                   {
                       dumpcountResult = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                   }
                   catch (Exception ex2)
                   {
                        Debug.WriteLine("[ex] Exception E: " + ex2.Message);
                        Exceptions.ThrowFullError(ex2);
                       return;
                   }

                   if (dumpcountResult.Contains("DumpCount    REG_DWORD"))
                   {

                        string tempname = dumpcountResult.Replace(
                            "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", 
                            "");
                        string tempname2 = tempname.Replace("DumpCount    REG_DWORD", "");
                        string tempresult = Regex.Replace(tempname2, @"\s+", "");
                      
                        switch (tempresult)
                        {
                            case "0x0":
                                DumpCountCombo.SelectedIndex = 0;
                                break;
                            case "0x1":
                                DumpCountCombo.SelectedIndex = 1;
                                break;
                            case "0x2":
                                DumpCountCombo.SelectedIndex = 2;
                                break;
                            case "0x3":
                                DumpCountCombo.SelectedIndex = 3;
                                break;
                            case "0x4":
                                DumpCountCombo.SelectedIndex = 4;
                                break;
                            case "0x5":
                                DumpCountCombo.SelectedIndex = 5;
                                break;
                            case "0x6":
                                DumpCountCombo.SelectedIndex = 6;
                                break;
                            case "0x7":
                                DumpCountCombo.SelectedIndex = 7;
                                break;
                            case "0x8":
                                DumpCountCombo.SelectedIndex = 8;
                                break;
                            case "0x9":
                                DumpCountCombo.SelectedIndex = 9;
                                break;
                            case "0xa":
                                DumpCountCombo.SelectedIndex = 10;
                                break;
                            default:
                                DumpCountCombo.SelectedIndex = 0;
                                break;
                        }
                   }
                   else
                   {
                        DumpCountCombo.SelectedIndex = 0;
                   }

                   //await 
                   client.Send(
                       "reg query \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" /v DumpFolder > " 
                       + $"{LocalPath}\\cmdstring.txt");

                   await Task.Delay(2000);

                   string dumpfolderResult = "";
                   try
                   {
                      dumpfolderResult = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                   }
                   catch (Exception ex2)
                   {
                        Debug.WriteLine("[ex] Exception F: " + ex2.Message);
                        Exceptions.ThrowFullError(ex2);
                       return;
                   }


                   if (dumpfolderResult.Contains("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps"))
                   {

                       string tempname = dumpfolderResult.Replace("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "");
                       string tempname2 = tempname.Replace("DumpFolder    REG_EXPAND_SZ", "");
                       dumpfolder = Regex.Replace(tempname2, @"\s+", "");
                   }
                   else
                   {
                       Exceptions.CustomMessage(dumpfolderResult +
                       "\n***LocalDumps not found in cmdstring.txt***");
                       dumpfolder = "";
                   }

                   DumpsLocationBox.Text = dumpfolder;
                }

                rpc.FileCopy("C:\\Data\\SystemData\\NonETWLogs\\ImgUpd.log", 
                    $"{LocalPath}\\ImgUpd.log", 0);

                rpc.FileCopy("C:\\Data\\SystemData\\NonETWLogs\\ImgUpd.log.cbs.log", 
                    $"{LocalPath}\\ImgUpd.log.cbs.log",
                    0);

                BootLogTog.Toggled += BootLogTog_Toggled;
                UefiTog.Toggled += UefiTog_Toggled;
                DumpCountCombo.SelectionChanged += DumpCountCombo_SelectionChanged;
                DumpTypeCombo.SelectionChanged += DumpTypeCombo_SelectionChanged;
                MainWindowPivot.IsEnabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception G: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                AppBusy(false);
            }

        }//CheckLoggingStatus


        // ViewLogBtn_Click
        private void ViewLogBtn_Click(object sender, RoutedEventArgs e)
        {
            AppBusy(true);

            string bootLogText = File.ReadAllText($"{LocalPath}\\ntbtlog.txt");

            //RnD
            BootLogDisplay.Text = "[" + bootLogText + "]";
            AppBusy(false);

        }//ViewLogBtn_Click


        // SaveLogBtn_Click
        private async void SaveLogBtn_Click(object sender, RoutedEventArgs e)
        {
            AppBusy(true);
            try
            {
                StorageFile bootLogToSave = await ApplicationData.Current.LocalFolder.GetFileAsync("ntbtlog.txt");
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add(".txt");
                StorageFolder bootSaveFolder = await folderPicker.PickSingleFolderAsync();
                if (bootSaveFolder == null)
                {
                    //
                }
                else
                {
                    await bootLogToSave.CopyAsync(bootSaveFolder);
                    Exceptions.CustomMessage("Saved log to " + bootSaveFolder.Path + "\\" + bootLogToSave.Name);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] Exception H: " + ex.Message);
                Exceptions.ThrowFullError(ex);
                AppBusy(false);
            }
            AppBusy(false);

        }//SaveLogBtn_Click


        // BootLogTog_Toggled
        private async void BootLogTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppBusy(true);
            if (BootLogTog.IsOn)
            {
                try
                {
                    //await 
                    client.Send("bcdedit /set {default} bootlog Yes > " 
                    + $"\"{LocalPath}\\cmdstring.txt\"");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ex] BootLogTog_Toggled Exception: " + ex.Message);
                }

                await Task.Delay(2000);

                string result = "";

                try
                {
                    result = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ex] BootLogTog_Toggled Exception: " + ex.Message);
                }

                if (result.Contains("The operation completed successfully."))
                {
                    IsBootlogEnabled = true;
                    Exceptions.CustomMessage("Enable boot logging successful");
                    SaveLogBtn.IsEnabled = true;
                    ViewLogBtn.IsEnabled = true;
                }
                else
                {
                    IsBootlogEnabled = false;
                    SaveLogBtn.IsEnabled = false;
                    ViewLogBtn.IsEnabled = false;
                    Exceptions.CustomMessage("There was an error enabling Boot Logging");
                }
            }
            else
            {
                if (IsBootlogEnabled == true)
                {
                    //await 
                    client.Send("bcdedit /set {default} bootlog No > " + $"\"{LocalPath}\\cmdstring.txt\"");

                    await Task.Delay(2000);

                    string result = "";

                    try
                    {
                        result = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[ex] BootLogTog_Toggled Exception: " + ex.Message);
                    }

                    if (result.Contains("The operation completed successfully."))
                    {
                        IsBootlogEnabled = false;
                        SaveLogBtn.IsEnabled = false;
                        ViewLogBtn.IsEnabled = false;
                        Exceptions.CustomMessage("Disable boot logging successful");
                    }
                    else
                    {
                        IsBootlogEnabled = true;
                        SaveLogBtn.IsEnabled = true;
                        ViewLogBtn.IsEnabled = true;
                        Exceptions.CustomMessage("There was an error disabling Boot Logging");
                    }
                }
            }
            AppBusy(false);

        }//BootLogTog_Toggled 


        // UefiTog_Toggled
        private async void UefiTog_Toggled(object sender, RoutedEventArgs e)
        {
            AppBusy(true);
            if (UefiTog.IsOn)
            {
                //await 
                client.Send("echo \"Created with Windows Logging Tools by Empyreal96\" > C:\\EFIESP\\Windows\\System32\\Boot\\UEFIChargingLogToDisplay.txt");


                //await 
                client.Send("if exist \"C:\\EFIESP\\Windows\\System32\\Boot\\UEFIChargingLogToDisplay.txt\" echo EXISTS > " + $"\"{LocalPath}\\cmdstring.txt\" 2>&1");

                await Task.Delay(2000);

                string results2 = "";

                try
                {
                    results2 = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[ex] BootLogTog_Toggled Exception: " + ex.Message);
                }

                if (results2.Contains("EXISTS"))
                {
                    IsUefiLogEnabled = true;
                    Exceptions.CustomMessage("Enabled UEFI logging successfully");
                }
                else
                {
                    IsUefiLogEnabled = false;
                    Exceptions.CustomMessage("Error enabling UEFI logging");
                }
            }
            else
            {
                if (IsUefiLogEnabled == true)
                {
                    //await 
                    client.Send("del C:\\EFIESP\\Windows\\System32\\Boot\\UEFIChargingLogToDisplay.txt");

                    IsUefiLogEnabled = false;
                    Exceptions.CustomMessage("Disabled UEFI logging successfully");
                }
            }
            AppBusy(false);

        }//UefiTog_Toggled


        // DumpTypeCombo_SelectionChanged
        private async void DumpTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppBusy(true);
            int value = DumpTypeCombo.SelectedIndex;

            //await 
            client.Send($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" /v DumpType /t REG_DWORD /d {value} /f > \"{LocalPath}\\cmdstring.txt\"");

            await Task.Delay(2000);

            string results = "";

            try
            {
                results = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] BootLogTog_Toggled Exception: " + ex.Message);
            }

            if (results.Contains("The operation completed successfully."))
            {
                //
            }
            else
            {
                Exceptions.CustomMessage("Error setting value for DumpType\n\n" + results);
                AppBusy(false);
            }
            AppBusy(false);

        }//DumpTypeCombo_SelectionChanged


        // DumpCountCombo_SelectionChanged
        private async void DumpCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AppBusy(true);
            int result = DumpCountCombo.SelectedIndex;
            //await 
            client.Send($"reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" /v DumpCount /t REG_DWORD /d {result} /f > \"{LocalPath}\\cmdstring.txt\"");

            await Task.Delay(2000);

            string results = "";

            try
            {
                results = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] DumpCount Change exception: " + ex.Message);
            }
            
            if (results.Contains("The operation completed successfully."))
            {
                //
            } 
            else
            {
                Exceptions.CustomMessage("Error setting value for DumpCount\n\n" + results);
                AppBusy(false);
            }
            AppBusy(false);

        }//DumpCountCombo_SelectionChanged


        // CrashBrowsebtn_Click
        private async void CrashBrowsebtn_Click(object sender, RoutedEventArgs e)
        {
            AppBusy(true);
            FolderPicker openFolder = new FolderPicker();
            openFolder.FileTypeFilter.Add(".dmp");
            StorageFolder savedFolder = await openFolder.PickSingleFolderAsync();
            if (savedFolder == null)
            {
                return;
            }
            else
            {
                try
                {
                    string savedPath = savedFolder.Path;
                    
                    //NativeRegistry.WriteMultiString(RegistryHive.HKLM, "SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps", "DumpFolder", savedPath);
                    
                    //await 
                    client.Send("reg add \"HKLM\\SOFTWARE\\Microsoft\\Windows\\Windows Error Reporting\\LocalDumps\" /v DumpFolder /t REG_EXPAND_SZ /d " + $"{savedPath} /f > \"{LocalPath}\\cmdstring.txt\"");

                    await Task.Delay(2000);

                    string results3 = "";

                    try
                    {
                        results3 = File.ReadAllText($"{LocalPath}\\cmdstring.txt");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("[ex] CrashBrowse btn_click Exception: " + ex.Message);
                    }
                    
                    if (results3.Contains("The operation completed successfully."))
                    {
                        DumpsLocationBox.Text = savedPath;
                    }
                    else
                    {
                        Exceptions.CustomMessage("An error occured while setting DumpFolder value");
                    }

                }
                catch (Exception ex)
                {
                    Exceptions.ThrowFullError(ex);
                    AppBusy(false);
                }
            }
            AppBusy(false);

        }//CrashBrowsebtn_Click


        // SaveUpdateBasicLog_Click
        private async void SaveUpdateBasicLog_Click(object sender, RoutedEventArgs e)
        {
            AppBusy(true);
            try
            {
                StorageFile UpdLogToSave = await ApplicationData.Current.LocalFolder.GetFileAsync("ImgUpd.log");
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add(".log");
                StorageFolder UpdSaveFolder = await folderPicker.PickSingleFolderAsync();
                if (UpdSaveFolder == null)
                {

                }
                else
                {
                    //await 
                    UpdLogToSave.CopyAsync(UpdSaveFolder);
                    Exceptions.CustomMessage("Saved log to " + UpdSaveFolder.Path + "\\" + UpdLogToSave.Name);
                }
            }
            catch (Exception ex)
            {
                Exceptions.ThrowFullError(ex);
                AppBusy(false);
            }
            AppBusy(false);

        }//SaveUpdateBasicLog_Click


        // ViewUpdateBasicLog_Click
        private void ViewUpdateBasicLog_Click(object sender, RoutedEventArgs e)
        {
            AppBusy(true);

            //UpdateLogText.Text = "";
            //string UpdLogText = "";
            //try
            //{                
            string UpdLogText = File.ReadAllText($"{LocalPath}\\ImgUpd.log");
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("[ex] ViewUpdateBasicLog Exception: " + ex.Message);
            //    Exceptions.ThrowFullError(ex);
            //    AppBusy(false);
            //}

            if (UpdLogText.Length > 10000)
            {
                try
                {
                    UpdLogText = UpdLogText.Substring(0, 10000) + "...";
                }
                catch
                {
                }
            }

            UpdateLogText.Text = "[" + UpdLogText + "]";
            Debug.WriteLine("[" + UpdLogText + "]");

            AppBusy(false);

        }//ViewUpdateBasicLog_Click


        // SaveUpdateAdvLog_Click
        private async void SaveUpdateAdvLog_Click(object sender, RoutedEventArgs e)
        {
            AppBusy(true);
            try
            {
                StorageFile UpdLogToSave = await ApplicationData.Current.LocalFolder.GetFileAsync("ImgUpd.log.cbs.log");
                FolderPicker folderPicker = new FolderPicker();
                folderPicker.FileTypeFilter.Add(".log");
                StorageFolder UpdSaveFolder = await folderPicker.PickSingleFolderAsync();
                if (UpdSaveFolder == null)
                {

                }
                else
                {
                    //await 
                    UpdLogToSave.CopyAsync(UpdSaveFolder);

                    Exceptions.CustomMessage("Saved log to " + UpdSaveFolder.Path + "\\" + UpdLogToSave.Name);
                }
            }

            catch (Exception ex)
            {
                Exceptions.ThrowFullError(ex);
                AppBusy(false);
            }
            
            AppBusy(false);

        }//SaveUpdateAdvLog_Click


        // ViewUpdateAdvLog_Click
        private void ViewUpdateAdvLog_Click(object sender, RoutedEventArgs e)
        {
            UpdateLogText.Text = "";
            string UpdLogText = "";

            try
            {                
                UpdLogText = File.ReadAllText($"{LocalPath}\\ImgUpd.log.cbs.log");
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ex] ViewUpdateAdvLog_Click exception: " + ex.Message);
                Exceptions.ThrowFullError(ex);
            }

            UpdateLogText.Text = UpdLogText;

        }//ViewUpdateAdvLog_Click


        // AppBusy
        private void AppBusy(bool enable)
        {
            if (enable == true)
            {
                AppBusyBar.IsEnabled = true;
                AppBusyBar.Visibility = Visibility.Visible;
                AppBusyBar.IsIndeterminate = true;
            } 
            else
            {
                AppBusyBar.IsEnabled = false;
                AppBusyBar.Visibility = Visibility.Collapsed;
                AppBusyBar.IsIndeterminate = false;
            }

        }//AppBusy end

    }//mainPage class end

}//Logging_Enabler namespace end
