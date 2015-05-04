using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPB.RemoteAdmin.Common;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel
{
    public class MessageBoxParameterViewModel : ViewModelBase
    {
        public MessageBoxParameterViewModel(MessageBoxParameter source)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            Source = source;
        }

        public MessageBoxParameter Source { get; private set; }

        public string Name
        {
            get { return Source.Name; }
            set
            {
                Source.Name = value;
                SendPropertyChanged(() => Name);
            }
        }
        
        public object Value
        {
            get { return Source.Value; }
            set
            {
                Source.Value = value;
                SendPropertyChanged(() => Value);
            }
        }
        
        public IEnumerable<object> Params
        {
            get { return Source.LookupValues; }           
        }
    }
}
