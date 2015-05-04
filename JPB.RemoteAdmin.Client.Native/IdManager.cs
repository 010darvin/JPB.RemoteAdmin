using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using JPB.Communication.ComBase;

namespace JPB.RemoteAdmin.Client.Native
{
    public class IdManager
    {
        private IdManager()
        {
            IdListHolder = new IdListHolder();
            if (File.Exists(PathToStore))
            {
                try
                {
                    using (var fs = new FileStream(PathToStore, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        var serializer = new XmlSerializer(typeof(IdListHolder), new[] { typeof(IdHolder) });
                        IdListHolder = (IdListHolder)serializer.Deserialize(fs);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine();
                }
            }
        }

        public const string PathToStore = "Clients.xml";

        private static IdManager _instance;
        public static IdManager Instance { get { return _instance ?? (_instance = new IdManager()); } }

        public IdHolder AddOrGet(string name, string lastIp, ushort port)
        {
            var fod = IdListHolder.Ids.FirstOrDefault(s => s.Id == name);

            if (fod != null)
                return fod;

            var idHolder = new IdHolder(name, lastIp, string.Empty, port);
            IdListHolder.Ids.Add(idHolder);
            return idHolder;
        }

        ~IdManager()
        {
            IdListHolder.LastKnownIp = NetworkInfoBase.GetPublicIp();

            using (var fs = new FileStream(PathToStore, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var serializer = new XmlSerializer(typeof(IdListHolder), new[] { typeof(IdHolder) });
                serializer.Serialize(fs, IdListHolder);
            }
        }

        public IdListHolder IdListHolder { get; set; }
    }
}