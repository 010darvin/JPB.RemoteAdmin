using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using JPB.RemoteAdmin.Common.Contracts;

namespace JPB.RemoteAdmin.Common.Messages.Dialog
{
    [Serializable]
    public class WindowsMessageBox : MessageBoxContractBase
    {
        public WindowsMessageBox()
        {
            Params.Add(new MessageBoxParameter("Buttons", Enum.GetValues(typeof(MessageBoxButtons)).Cast<MessageBoxButtons>()));
            Params.Add(new MessageBoxParameter("Image", Enum.GetValues(typeof(MessageBoxIcon)).Cast<MessageBoxIcon>()));
            Params.Add(new MessageBoxParameter("DefaultButton", Enum.GetValues(typeof(MessageBoxDefaultButton)).Cast<MessageBoxDefaultButton>()));
            Params.Add(new MessageBoxParameter("MessageBoxOptions", Enum.GetValues(typeof(MessageBoxOptions)).Cast<MessageBoxOptions>()));
            
            Params.Add(new MessageBoxParameter("Text"));
            Params.Add(new MessageBoxParameter("Header"));
        }
        
        public override string Id { get { return MessageBoxContractManager.MessageBoxContract; }}

        public MessageBoxButtons Buttons { get; set; }
        public MessageBoxIcon Image { get; set; }

        public MessageBoxDefaultButton DefaultButton { get; set; }
        public MessageBoxOptions MessageBoxOptions { get; set; }
        public string Text { get; set; }
        public string Header { get; set; }
    }
}
