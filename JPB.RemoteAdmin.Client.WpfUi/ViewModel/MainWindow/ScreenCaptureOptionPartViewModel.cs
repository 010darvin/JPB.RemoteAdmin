using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using JPB.WPFBase.MVVM.ViewModel;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class ScreenCaptureOptionPartViewModel : ViewModelBase
    {
        private Encoder item;
        private string _name;
        private int _value;

        public static ScreenCaptureOptionPartViewModel FromGeneric(Encoder item, string name, int defaultValue)
        {
            var model = new ScreenCaptureOptionPartViewModel(item, name);
            model.Value = defaultValue;
            return model;
        }

        public static ScreenCaptureOptionPartViewModel FromGeneric(Encoder item, string name, int defaultValue, params EnumWrapper[] lookup)
        {
            var model = new ScreenCaptureOptionPartViewModel(item, name, lookup);
            model.Value = defaultValue;
            return model;
        }

        private ScreenCaptureOptionPartViewModel(Encoder item, string name)
        {
            // TODO: Complete member initialization
            this.item = item;
            Name = name;
            LookupValues = new List<EnumWrapper>();
        }

        private ScreenCaptureOptionPartViewModel(Encoder item, string name, IEnumerable<EnumWrapper> lookup)
            : this(item, name)
        {
            foreach (var items in lookup)
            {
                LookupValues.Add(items);
            }
        }

        public List<EnumWrapper> LookupValues { get; set; }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                SendPropertyChanged(() => Name);
            }
        }

        private bool _enabled;

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                SendPropertyChanged(() => Enabled);
            }
        }

        public int Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                SendPropertyChanged(() => Value);
            }
        }
    }
}
