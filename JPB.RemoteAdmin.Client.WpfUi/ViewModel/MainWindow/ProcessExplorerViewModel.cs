using JPB.RemoteAdmin.Common.Messages;
using JPB.WPFBase.MVVM.DelegateCommand;
using JPB.WPFBase.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JPB.RemoteAdmin.Client.WpfUi.ViewModel.MainWindow
{
	public class ProcessExplorerViewModel : AsyncViewModelBase
	{
		private SingelClientViewModel _singelClientViewModel;
		public ProcessExplorerViewModel(SingelClientViewModel singelClientViewModel)
		{
			_singelClientViewModel = singelClientViewModel;
			StartPullTaskListCommand = new DelegateCommand(StartPullTaskList, CanStartPullTaskList);
			StopPullTaskListCommand = new DelegateCommand(StopPullTaskList);
            RemoteProcesses = new ThreadSaveObservableCollection<ProcessModel>();
            KillSelectedCommand = new DelegateCommand(ExecuteKillSelected, CanExecuteKillSelected);
            SelfDestructCommand = new DelegateCommand(ExecuteSelfDestruct, CanExecuteSelfDestruct);
			_stopRequested = new CancellationTokenSource();
		}

		public ThreadSaveObservableCollection<ProcessModel> RemoteProcesses { get; set; }
	    private ProcessModel _processModel;

	    public ProcessModel ProcessModel
	    {
	        get { return _processModel; }
	        set
	        {
	            _processModel = value;
	            SendPropertyChanged(() => ProcessModel);
	        }
	    }

	    public DelegateCommand SelfDestructCommand { get; private set; }

	    public void ExecuteSelfDestruct(object sender)
        {
            _singelClientViewModel.ClientInstance.SelfDestruct();
	    }

	    public bool CanExecuteSelfDestruct(object sender)
	    {
	        return true;
	    }

		private Task _runner;
		private CancellationTokenSource _stopRequested;

		public DelegateCommand StartPullTaskListCommand { get; set; }

		private void StartPullTaskList(object sender)
		{
			_runner = new Task(async () =>
			{
				while (!_stopRequested.Token.IsCancellationRequested)
				{
					var requestProc = _singelClientViewModel.ClientInstance.GetProcess();
					Thread.Yield();
					Thread.Sleep(1000);
					ProcessModel[] proc;
					try
					{
						proc = await requestProc;
					}
					catch (Exception)
					{
						continue;
					}
					
					foreach (var item in proc)
					{
						var fod = RemoteProcesses.FirstOrDefault(s => s.TaskId == item.TaskId);
						if(fod == null)
						{
							RemoteProcesses.Add(item);
						}
					}

					foreach (var item in RemoteProcesses.ToArray())
					{
						var fod = proc.FirstOrDefault(s => s.TaskId == item.TaskId);
						if(fod == null)
						{
							RemoteProcesses.Remove(item);
						}
					}
				}
			    _runner = null;
			}, _stopRequested.Token, TaskCreationOptions.LongRunning);
			_runner.Start();
		}

	    public DelegateCommand KillSelectedCommand { get; private set; }

	    public void ExecuteKillSelected(object sender)
	    {
	        _singelClientViewModel.ClientInstance.KillProcess(ProcessModel.TaskId);
	    }

	    public bool CanExecuteKillSelected(object sender)
	    {
            return ProcessModel != null;
	    }

		public bool CanStartPullTaskList(object sender)
		{
            return base.CheckCanExecuteCondition() && _runner == null;
		}

		public DelegateCommand StopPullTaskListCommand { get; set; }

		private void StopPullTaskList(object sender)
		{
			_stopRequested.Cancel();
		}
	}
}
