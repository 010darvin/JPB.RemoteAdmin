using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class ScreenOptionsMessage
    {
        public List<ScreenOption> EncoderOptions { get; set; }
        public string DisplayGuid { get; set; }
    }

    [Serializable]
    public class ScreenOption
    {
        public string Name { get; set; }
        public long Value { get; set; }
        public bool PredifinedValues { get; set; }
    }
}
