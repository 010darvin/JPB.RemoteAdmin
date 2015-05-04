namespace JPB.RemoteAdmin.Client.Native
{
    public class IdHolder
    {
        public IdHolder(string name, string lastKnownIp, string id, ushort lastKnownPort)
        {
            LastKnownPort = lastKnownPort;
            LastKnownIp = lastKnownIp;
            Name = name;
            Id = id;
        }

        private IdHolder()
        {
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string LastKnownIp { get; set; }
        public ushort LastKnownPort { get; set; }

        public static implicit operator string(IdHolder x)
        {
            return x.Id;
        }
    }
}