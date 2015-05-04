using System.Collections.Generic;

namespace JPB.RemoteAdmin.Client.Native
{
    public class IdListHolder
    {
        public IdListHolder()
        {
            Ids = new List<IdHolder>();
        }

        public string LastKnownIp { get; set; }

        public List<IdHolder> Ids { get; set; }
    }
}