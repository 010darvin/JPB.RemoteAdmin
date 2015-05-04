
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace JPB.RemoteAdmin.Client.WpfUi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {

        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName,
                                         MoveFileFlags dwFlags);

        [Flags]
        public enum MoveFileFlags
        {
            MOVEFILE_REPLACE_EXISTING = 0x00000001,
            MOVEFILE_COPY_ALLOWED = 0x00000002,
            MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004,
            MOVEFILE_WRITE_THROUGH = 0x00000008,
            MOVEFILE_CREATE_HARDLINK = 0x00000010,
            MOVEFILE_FAIL_IF_NOT_TRACKABLE = 0x00000020
        }

        private static DateTime _buildTime;
        private static TimeSpan ValidAsLongAs = new TimeSpan(12, 0, 0);

        public static bool CheckEval()
        {
            //#if DEBUG
            //            return false;
            //#endif

            if (_buildTime == default(DateTime))
                _buildTime = GetBuildDateTime(Assembly.GetExecutingAssembly());

            if (DateTime.Now - _buildTime <= ValidAsLongAs)
            {
                return false;
            }
            return true;
        }

        private string ValitUntil()
        {
            var db = new DateTime((_buildTime + ValidAsLongAs).Ticks);
            return db.ToString();
        }

        private void BreakDown()
        {
            MessageBox.Show(string.Format("Evaluation is depleted '{0}'. Call owner for new Version. Asseambly will Selfdestruct", ValitUntil()), "Err", MessageBoxButton.OK);
            ProcessStartInfo Info = new ProcessStartInfo();

            var command = "\"" + Assembly.GetExecutingAssembly().Location + "\"";
            foreach (var item in Directory.EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "*.dll"))
            {
                command += " & Del \"" + item + "\"";
            }

            Info.Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + command;
            Info.WindowStyle = ProcessWindowStyle.Hidden;
            Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);

            Current.Shutdown();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            //var first = e.Args.FirstOrDefault();

            //if (first != null)
            //{
            //    if (first == "--start")
            //    {
            //        var prc = new Program();
            //        var task = prc.GoGoGatdet();
            //        task.Wait();
            //        return;
            //    }
            //}

            var evl = CheckEval();
            if (evl)
            {
                BreakDown();
            }
            else
            {
                MessageBox.Show(string.Format("Evaluation. Valid until: {0}", ValitUntil()));
                var timer = new DispatcherTimer { Interval = new TimeSpan(0, 0, 20) };
                timer.Tick += timer_Tick;
                timer.IsEnabled = true;

                new MainWindow().Show();
            }
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (CheckEval())
            {
                BreakDown();
            }
        }

        struct _IMAGE_FILE_HEADER
        {
            public ushort Machine;
            public ushort NumberOfSections;
            public uint TimeDateStamp;
            public uint PointerToSymbolTable;
            public uint NumberOfSymbols;
            public ushort SizeOfOptionalHeader;
            public ushort Characteristics;
        };

        static DateTime GetBuildDateTime(Assembly assembly)
        {
            if (File.Exists(assembly.Location))
            {
                var buffer = new byte[Math.Max(Marshal.SizeOf(typeof(_IMAGE_FILE_HEADER)), 4)];
                using (var fileStream = new FileStream(assembly.Location, FileMode.Open, FileAccess.Read))
                {
                    fileStream.Position = 0x3C;
                    fileStream.Read(buffer, 0, 4);
                    fileStream.Position = BitConverter.ToUInt32(buffer, 0); // COFF header offset
                    fileStream.Read(buffer, 0, 4); // "PE\0\0"
                    fileStream.Read(buffer, 0, buffer.Length);
                }
                var pinnedBuffer = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                try
                {
                    var coffHeader = (_IMAGE_FILE_HEADER)Marshal.PtrToStructure(pinnedBuffer.AddrOfPinnedObject(), typeof(_IMAGE_FILE_HEADER));

                    return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1) + new TimeSpan(coffHeader.TimeDateStamp * TimeSpan.TicksPerSecond));
                }
                finally
                {
                    pinnedBuffer.Free();
                }
            }
            var version = assembly.GetName().Version;
            return new DateTime(2000, 1, 1).Add(new TimeSpan(
                TimeSpan.TicksPerDay * version.Build + // days since 1 January 2000
                TimeSpan.TicksPerSecond * 2 * version.Revision));
        }
    }
}
