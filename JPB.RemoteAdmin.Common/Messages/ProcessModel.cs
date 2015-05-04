using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class ProcessModel
    {
        public static ProcessModel GetModel(Process proc)
        {
            var model = new ProcessModel();
            model.Name = proc.ProcessName;
            model.TaskId = proc.Id;
            model.UserName = proc.StartInfo.Domain + "@" + proc.StartInfo.UserName;
            model.Location = proc.StartInfo.FileName;
            return model;
        }

        public string Name { get; set; }
        public int TaskId { get; set; }

        public string UserName { get; set; }
        public string Location { get; set; }
    }
}
