using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace JPB.RemoteAdmin.Common
{

    public class MessageBoxParameter
    {
        private object _value;
        private string _name;

        public MessageBoxParameter()
        {

        }

        public MessageBoxParameter(string name, params object[] param)
        {
            Name = name;
            LookupValues = new List<object>();
            foreach (var item in param)
            {
                if (item is IEnumerable)
                {
                    foreach (var item2 in item as IEnumerable)
                    {
                        LookupValues.Add(item2);
                    }
                }
                else
                {
                    LookupValues.Add(item);
                }
            }
        }

        public MessageBoxParameter(string name)
        {
            Name = name;
            LookupValues = new List<object>();
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        [XmlIgnore()]
        public List<object> LookupValues { get; private set; }
    }
}
