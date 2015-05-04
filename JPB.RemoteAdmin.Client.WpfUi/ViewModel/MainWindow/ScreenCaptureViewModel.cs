using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using AForge.Video.FFMPEG;
using JPB.Communication.ComBase.Messages;
using JPB.RemoteAdmin.Client.WpfUi.View;
using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using System.Collections.Generic;
using JPB.RemoteAdmin.Common;
using System.IO.Compression;
using Application = System.Windows.Application;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
    public class ScreenCaptureViewModel : AsyncViewModelBase
    {
        private readonly SingelClientViewModel _singelClientViewModel;
        public ScreenCaptureOptionsViewModel OptionsViewModel { get; set; }

        public ScreenCaptureViewModel(SingelClientViewModel singelClientViewModel)
        {
            OptionsViewModel = new ScreenCaptureOptionsViewModel(this);
            _singelClientViewModel = singelClientViewModel;
            _singelClientViewModel.ClientInstance.OnImageIncomming += this.ScreenCaptureIncomming;
            StartCaptureCommand = new DelegateCommand(ExecuteStartCapture, CanExecuteStartCapture);
            StopCaptureCommand = new DelegateCommand(ExecuteStopCapture, CanExecuteStopCapture);
            LoadDevicesCommand = new DelegateCommand(ExecuteLoadDevices, CanExecuteLoadDevices);
            OpenOptionsWindowCommand = new DelegateCommand(ExecuteOpenOptionsWindow, CanExecuteOpenOptionsWindow);
            Devices = new ThreadSaveObservableCollection<string[]>();
            timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.DataBind, OnFpsCounterCallback, Dispatcher);
            base.BeginThreadSaveAction(() =>
            {
                Application.Current.Exit += Current_Exit;
            });
        }

        private void OnFpsCounterCallback(object sender, EventArgs eventArgs)
        {
            SendPropertyChanged(() => Fps);
            Fps = 0;
        }

        void Current_Exit(object sender, ExitEventArgs e)
        {
            if (_screenOpWindow != null)
                _screenOpWindow.Close();
        }

        public Bitmap ReduceBitmap(Bitmap original, int reducedWidth, int reducedHeight)
        {
            var reduced = new Bitmap(reducedWidth, reducedHeight);
            using (var dc = Graphics.FromImage(reduced))
            {
                // you might want to change properties like
                dc.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                dc.DrawImage(original, new RectangleF(0, 0, reducedWidth, reducedHeight), new RectangleF(0, 0, original.Width, original.Height), GraphicsUnit.Pixel);
            }

            return reduced;
        }

        private ThreadSaveObservableCollection<string[]> _devices;

        public ThreadSaveObservableCollection<string[]> Devices
        {
            get { return _devices; }
            set
            {
                _devices = value;
                SendPropertyChanged(() => Devices);
            }
        }

        private string _selectedDevice;

        public string SelectedDevice
        {
            get { return _selectedDevice; }
            set
            {
                _selectedDevice = value;
                base.ThreadSaveAction(() => SendPropertyChanged(() => SelectedDevice));
            }
        }

        Window _screenOpWindow;

        public DelegateCommand OpenOptionsWindowCommand { get; private set; }

        public void ExecuteOpenOptionsWindow(object sender)
        {
            var windowContent = new ScreenCaptureOptionsView { DataContext = OptionsViewModel };
            _screenOpWindow = new Window
            {
                Content = windowContent,
                Topmost = true,
                WindowStyle = WindowStyle.ToolWindow,
                Title = _singelClientViewModel.TargetIP,
                SizeToContent = SizeToContent.WidthAndHeight
            };
            _screenOpWindow.Show();
            _screenOpWindow.Closed += (o, args) => _screenOpWindow = null;
        }

        public bool CanExecuteOpenOptionsWindow(object sender)
        {
            return _screenOpWindow == null;
        }

        public DelegateCommand LoadDevicesCommand { get; private set; }

        public void ExecuteLoadDevices(object sender)
        {
            Devices.Clear();

            base.SimpleWork(async () =>
            {
                var sendRequstMessage = await this._singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender.SendRequstMessage<string[][]>(
                    new RequstMessage()
                    {
                        InfoState = InfoState.GetWebcamsRequest,
                        ExpectedResult = 1337
                    }, this._singelClientViewModel.TargetIP);

                if (sendRequstMessage != null)
                {
                    foreach (var webcam in sendRequstMessage)
                    {
                        Devices.Add(webcam);
                    }
                    SelectedDevice = Devices.First()[0];
                }
            });
        }

        public bool CanExecuteLoadDevices(object sender)
        {
            return IsNotWorking && !this._singelClientViewModel.ClientInstance.PullImages && this._singelClientViewModel.ClientInstance.HostAddress.TCPNetworkSender != null;
        }

        private VideoFileWriter writer;
        string fileLoc;
        int heigh;
        int wight;
        private DateTime started;

        private int _fPS;

        public int Fps
        {
            get { return _fPS; }
            set
            {
                _fPS = value;
            }
        }

        private void ScreenCaptureIncomming(object sender, byte[] requstMessage)
        {
            using (var memStream = new MemoryStream(requstMessage))
            using (var decompressedStream = new MemoryStream())
            {
                using (var defaltStream = new DeflateStream(memStream, CompressionMode.Decompress, true))
                {
                    defaltStream.CopyTo(decompressedStream);
                }
                _content = decompressedStream.ToArray();
            }
            requstMessage = null;
            Fps++;

            SendPropertyChanged(() => ImageSource);

            if (Delay > 0)
                Thread.Sleep(Delay);


            //if (requstMessage.Sender != _singelClientViewModel.TargetIP)
            //    return null;

            //var screenCaptureMessage = requstMessage.Message as ScreenCaptureMessage;
            //if (screenCaptureMessage != null)
            //{
            //    _content = screenCaptureMessage.ImageChunck;
            //}
            //else
            //{
            //    _content = requstMessage.Message as byte[];
            //}
            //if (SaveToFile)
            //{
            //    if (writer == null)
            //    {
            //        writer = new VideoFileWriter();
            //        fileLoc = Path.GetTempFileName();
            //        using (var memStream = new MemoryStream(_content))
            //        {
            //            var fromStream = Image.FromStream(memStream);
            //            heigh = fromStream.Height;
            //            wight = fromStream.Width;
            //        }
            //        writer.Open(fileLoc, wight, heigh);
            //    }

            //    using (var memStream = new MemoryStream(_content))
            //    {
            //        var fromStream = Image.FromStream(memStream);
            //        if (started == default(DateTime))
            //        {
            //            started = DateTime.Now;
            //        }

            //        var timeSpan = DateTime.Now - started;
            //        if (writer.IsOpen)
            //        {
            //            writer.WriteVideoFrame(ReduceBitmap(new Bitmap(fromStream), wight, heigh), timeSpan);
            //        }
            //    }
            //}
        }

        private int _delay;

        public int Delay
        {
            get { return _delay; }
            set
            {
                _delay = value;
                SendPropertyChanged(() => Delay);
            }
        }

        private bool _saveToFile;

        public bool SaveToFile
        {
            get { return _saveToFile; }
            set
            {
                _saveToFile = value;
                SendPropertyChanged(() => SaveToFile);
            }
        }

        private DispatcherTimer timer;

        public DelegateCommand StartCaptureCommand { get; private set; }

        public void ExecuteStartCapture(object sender)
        {
            base.SimpleWork(() =>
            {
                var op = OptionsViewModel.CreateSerilizableOptions();
                op.DisplayGuid = SelectedDevice;
                this._singelClientViewModel.ClientInstance.StartPulling(op);
            });
        }

        public bool CanExecuteStartCapture(object sender)
        {
            return !this._singelClientViewModel.ClientInstance.PullImages && !string.IsNullOrEmpty(SelectedDevice) && this["ExecuteStartCapture"];
        }

        public DelegateCommand StopCaptureCommand { get; private set; }

        public void ExecuteStopCapture(object sender)
        {
            base.SimpleWork(() =>
            {

                this._singelClientViewModel.ClientInstance.StopPulling();
                //if (writer != null)
                //{
                //    writer.Close();
                //    File.Move(fileLoc, (Path.Combine(_singelClientViewModel.ClientInstance.GetTargetDir(), Path.ChangeExtension(Path.GetFileName(fileLoc), "avi"))));
                //}
                started = default(DateTime);
            });
        }

        public bool CanExecuteStopCapture(object sender)
        {
            return this._singelClientViewModel.ClientInstance.PullImages && this["ExecuteStopCapture"];
        }

        private BitmapSource empty;
        private BitmapImage currentImage;
        private BitmapImage lastImage;
        private byte[] _content;

        private void CreateEmptyBitmap()
        {
            int width = 128;
            int height = width;
            int stride = width / 8;
            byte[] pixels = new byte[height * stride];

            // Try creating a new image with a custom palette.
            var colors = new List<System.Windows.Media.Color>();
            colors.Add(Colors.Red);
            colors.Add(Colors.Blue);
            colors.Add(Colors.Green);
            var myPalette = new BitmapPalette(colors);

            // Creates a new empty image with the pre-defined palette
            empty = BitmapSource.Create(width, height, 96, 96, PixelFormats.Indexed1, myPalette, pixels, stride);
        }

        public ImageSource ImageSource
        {
            get
            {
                if (_content == null)
                {
                    if (empty == null)
                    {
                        CreateEmptyBitmap();
                    }
                    return empty;
                }

                if (currentImage != null)
                {
                    if (currentImage == lastImage)
                    {
                        return currentImage;
                    }
                }

                currentImage = new BitmapImage();
                using (var memStream = new MemoryStream(_content))
                {
                    currentImage.BeginInit();
                    currentImage.StreamSource = memStream;
                    currentImage.CacheOption = BitmapCacheOption.OnLoad;
                    currentImage.EndInit();
                }

                return currentImage;
            }
        }

        internal void EmitMouseClick(System.Windows.Input.MouseButton mouseButton, bool down, System.Windows.Point point, double Xreal, double Yreal)
        {
            var inp = MouseOperations.MouseEventFlags.Absolute;
            switch (mouseButton)
            {
                case System.Windows.Input.MouseButton.Left:
                    if (down)
                    {
                        inp = MouseOperations.MouseEventFlags.LeftDown;
                    }
                    else
                    {
                        inp = MouseOperations.MouseEventFlags.LeftUp;
                    }
                    break;
                case System.Windows.Input.MouseButton.Middle:
                    if (down)
                    {
                        inp = MouseOperations.MouseEventFlags.MiddleDown;

                    }
                    else
                    {
                        inp = MouseOperations.MouseEventFlags.MiddleUp;
                    }
                    break;
                case System.Windows.Input.MouseButton.Right:
                    if (down)
                    {
                        inp = MouseOperations.MouseEventFlags.RightDown;
                    }
                    else
                    {
                        inp = MouseOperations.MouseEventFlags.RightUp;
                    }
                    break;
                case System.Windows.Input.MouseButton.XButton1:
                    break;
                case System.Windows.Input.MouseButton.XButton2:
                    break;
                default:
                    break;
            }
            this._singelClientViewModel.ClientInstance.SendMouseClick(inp, point.X, point.Y, Xreal, Yreal);
        }

        internal void EmitKeybordAction(System.Windows.Input.Key key)
        {
            var keyCompiled = KeyInterop.VirtualKeyFromKey(key);
            this._singelClientViewModel.ClientInstance.SendKeybordAction(keyCompiled);
        }
    }
}
