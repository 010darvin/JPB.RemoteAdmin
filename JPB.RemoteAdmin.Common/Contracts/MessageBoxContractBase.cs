using System;
using System.Collections.Generic;

namespace JPB.RemoteAdmin.Common.Contracts
{
    [Serializable]
    public abstract class MessageBoxContractBase
    {
        protected MessageBoxContractBase()
        {
            _params = new List<MessageBoxParameter>();
        }

        [NonSerialized]
        private List<MessageBoxParameter> _params;

        public abstract string Id { get; }

        public List<MessageBoxParameter> Params
        {
            get { return _params; }
        }
    }
}
