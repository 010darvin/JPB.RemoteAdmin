using System;

namespace JPB.RemoteAdmin.Common.Messages
{
    [Serializable]
    public class DomainUser
    {
        private string _userName;

        public string UserName
        {
            get { return _userName; }
            set
            {
                string domainWithSlash = Domain + @"\";
                if (value.StartsWith(domainWithSlash))
                    value = value.Substring(domainWithSlash.Length);
                _userName = value;
            }
        }

        public string Password { get; set; }
        public string Domain { get; set; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Domain))
                return Domain + @"\" + UserName + " : " + Password;
            return UserName + " : " + Password;
        }
    }
}