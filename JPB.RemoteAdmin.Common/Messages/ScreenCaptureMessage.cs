using System;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class ScreenCaptureMessage
    {
        public byte[] ImageChunck { get; set; }
        public byte[] SoundChunck { get; set; }
    }
}
