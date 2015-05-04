using System;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class DynamicCodeExcuter
    {
        public byte[] ExecuterWithMain { get; set; }
        public string TargetFileName { get; set; }
    }

    [Serializable]
    public class DynmaicProgramExecuter
    {
        public bool ShouldWait { get; set; }
    }
}
