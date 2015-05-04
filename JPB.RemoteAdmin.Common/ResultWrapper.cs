using System;
using System.IO;
using System.Threading;
using System.Timers;

namespace JPB.RemoteAdmin.Common
{
    public class ResultWrapper : IDisposable
    {
        public string SourceIp { get; private set; }
        public ManualResetEventSlim EventSlim { get; private set; }
        public decimal RequestedSize { get; set; }
        public string ResultData { get; set; }
        public Action<int> ProcessReport { get; private set; }
        public Func<Stream> Source { get; set; }

        System.Timers.Timer _timer;

        public ResultWrapper(long requestedSize, ManualResetEventSlim eventSlim, string sourceIp)
        {
            SourceIp = sourceIp;
            RequestedSize = requestedSize;
            EventSlim = eventSlim;
        }

        public void SetProcessReporter(Action<int> processReport)
        {
            if (processReport != null)
            {
                ProcessReport = processReport;

                _timer = new System.Timers.Timer();
                _timer.Elapsed += OnTimerCall;
                _timer.AutoReset = true;
                _timer.Interval = 1000;
                _timer.Start();
            }
        }

        private void OnTimerCall(object state, ElapsedEventArgs elapsedEventArgs)
        {
            if (EventSlim.IsSet)
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    _timer = null;
                    ProcessReport(100);
                }
                return;
            }
            try
            {
                if(Source == null)
                    return;

                var stream = Source() as FileStream;

                if (stream == null || stream.Length == 0)
                {
                    return;
                }

                decimal pc = stream.Length / RequestedSize;
                decimal processValue = pc * 100;
                ProcessReport((int)processValue);
            }
            catch (ObjectDisposedException ex)
            {
                if (_timer != null)
                {
                    _timer.Stop();
                    _timer.Dispose();
                }
                ProcessReport(100);
                _timer = null;
            }
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }
}