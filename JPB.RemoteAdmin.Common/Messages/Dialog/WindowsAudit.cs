using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPB.RemoteAdmin.Common.Contracts;

namespace JPB.RemoteAdmin.Common.Messages.Dialog
{
    [Serializable]
    public class WindowsAudit : MessageBoxContractBase
    {
        public WindowsAudit()
        {
            Params.Add(new MessageBoxParameter("Header"));
            Params.Add(new MessageBoxParameter("Message"));
        }
        public override string Id
        {
            get { return MessageBoxContractManager.WindowsAuditContract; }
        }

        public string Header { get; set; }
        public string Message { get; set; }
    }
}
