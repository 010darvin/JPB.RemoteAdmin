using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JPB.RemoteAdmin.Common.Contracts;

namespace JPB.RemoteAdmin.Common.Messages
{
    public class MessageBoxContractManager
    {
        public static IEnumerable<MessageBoxContractBase> GetContracts()
        {
            if (_enumerated != null)
                return _enumerated;

            return
                _enumerated =
                    Assembly.GetAssembly(typeof (MessageBoxContractBase))
                        .GetTypes()
                        .Where(s => typeof (MessageBoxContractBase).IsAssignableFrom(s) && s.IsClass && !s.IsAbstract)
                        .Select(s => Activator.CreateInstance(s) as MessageBoxContractBase);
        }

        private static IEnumerable<MessageBoxContractBase> _enumerated;
        public const string MessageBoxContract = "Generic-Messagebox";
        public const string WindowsAuditContract = "Windows-Audit";
    }
}