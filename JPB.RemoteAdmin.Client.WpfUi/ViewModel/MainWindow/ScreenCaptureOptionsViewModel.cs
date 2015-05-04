using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using JPB.RemoteAdmin.Common.Messages;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public static class EnumToObjectConverter
    {
        public static EnumWrapper Wrap(this object enumMember)
        {
            if (!enumMember.GetType().IsEnum)
                return null;

            return new EnumWrapper()
            {
                Original = enumMember,
                Name = enumMember.ToString(),
                Value = (int)enumMember
            };
        }
    }

    public class EnumWrapper
    {
        public object Original { get; set; }
        public string Name { get; set; }
        public int Value { get; set; }
    }

    public class ScreenCaptureOptionsViewModel : AsyncViewModelBase
    {
        private readonly ScreenCaptureViewModel _screenCaptureViewModel;

        public ScreenCaptureOptionsViewModel(ScreenCaptureViewModel screenCaptureViewModel)
        {
            _screenCaptureViewModel = screenCaptureViewModel;
            Options = new ObservableCollection<ScreenCaptureOptionPartViewModel>
            {
                ScreenCaptureOptionPartViewModel.FromGeneric(Encoder.ColorDepth, "ColorDepth", 40),
                ScreenCaptureOptionPartViewModel.FromGeneric(Encoder.Compression, "Compression", (int) EncoderValue.CompressionLZW,
                    EncoderValue.CompressionCCITT3.Wrap(),
                    EncoderValue.CompressionCCITT4.Wrap(),
                    EncoderValue.CompressionLZW.Wrap(),
                    EncoderValue.CompressionRle.Wrap(),
                    EncoderValue.CompressionNone.Wrap()),

               ScreenCaptureOptionPartViewModel.FromGeneric(Encoder.Transformation, "Transformation", 0,
                    EncoderValue.TransformFlipHorizontal.Wrap(),
                    EncoderValue.TransformFlipVertical.Wrap(),
                    EncoderValue.TransformRotate180.Wrap(),
                    EncoderValue.TransformRotate270.Wrap(),
                    EncoderValue.TransformRotate90.Wrap()),

               ScreenCaptureOptionPartViewModel.FromGeneric(Encoder.ScanMethod, "Transformation", (int)EncoderValue.ScanMethodNonInterlaced,
                    EncoderValue.ScanMethodInterlaced.Wrap(),
                    EncoderValue.ScanMethodNonInterlaced.Wrap()),

                ScreenCaptureOptionPartViewModel.FromGeneric(Encoder.Quality, "Quality", 40)
            };
            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
        }

        public DelegateCommand SaveCommand { get; private set; }

        public async void ExecuteSave(object sender)
        {
            _screenCaptureViewModel.ExecuteStopCapture(this);
            _screenCaptureViewModel.ExecuteStartCapture(true);
        }

        public bool CanExecuteSave(object sender)
        {
            return true;
        }

        public ObservableCollection<ScreenCaptureOptionPartViewModel> Options { get; set; }

        internal ScreenOptionsMessage CreateSerilizableOptions()
        {
            var opt = new ScreenOptionsMessage();
            opt.EncoderOptions = new List<ScreenOption>();

            foreach (var item in Options.Where(item => item.Value != 0 && item.Enabled))
            {
                opt.EncoderOptions.Add(new ScreenOption()
                {
                    Name = item.Name,
                    Value = item.Value,
                    PredifinedValues = item.LookupValues.Any()
                });
            }
            return opt;
        }
    }
}
