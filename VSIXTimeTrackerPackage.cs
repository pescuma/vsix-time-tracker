using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;

namespace VSIXTimeTracker
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "0.1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	//[ProvideAutoLoad(UIContextGuids.SolutionExists)]
	[ProvideAutoLoad(UIContextGuids.NoSolution)]
	public sealed class VSIXTimeTrackerPackage : Package, IVsDebuggerEvents
	{
		public const string PackageGuidString = "C0A8C0A3-C17E-44B3-BC5E-7812DD4C1536";

		private IVsOutputWindowPane outputWindow;
		private DTE2 dte;
		private Events events;
		private BuildEvents buildEvents;
		private DTEEvents dteEvents;
		private SolutionEvents solutionEvents;
		private IVsDebugger debugService;
		private IOperationState operationState;
		private Application application;
		private uint debugCookie;
		private VSStateMachine sm;

		protected override void Initialize()
		{
			base.Initialize();

			var serviceContainer = (IServiceContainer) this;
			dte = (DTE2) serviceContainer.GetService(typeof(SDTE));
			events = dte.Events;
			dteEvents = events.DTEEvents;

			dteEvents.OnStartupComplete += OnStartupComplete;
			dteEvents.OnBeginShutdown += OnBeginShutdown;
		}

		private void OnStartupComplete()
		{
			CreateOutputWindow();

			solutionEvents = events.SolutionEvents;
			buildEvents = events.BuildEvents;

			solutionEvents.Opened += OnSolutionOpened;
			solutionEvents.AfterClosing += OnSolutionClosed;

			buildEvents.OnBuildBegin += OnBuildBegin;
			buildEvents.OnBuildDone += OnBuildDone;

			debugService = (IVsDebugger) GetGlobalService(typeof(SVsShellDebugger));
			debugService.AdviseDebuggerEvents(this, out debugCookie);

			var componentModel = (IComponentModel) GetGlobalService(typeof(SComponentModel));
			operationState = componentModel.GetService<IOperationState>();
			operationState.StateChanged += OperationStateOnStateChanged;

			application = Application.Current;
			application.Activated += OnApplicationActivated;
			application.Deactivated += OnApplicationDeactivated;

			sm = new VSStateMachine();

			if (application.Windows.OfType<System.Windows.Window>()
					.All(w => !w.IsActive))
				sm.On(VSStateMachine.Events.LostFocus);

			if (dte.Solution.Count > 0)
				sm.On(VSStateMachine.Events.SolutionOpened);

			sm.StateChanged += s => Output("Current state: {0}", s.ToString());

			Output("Startup complete");
			Output("Current state: {0}", sm.CurrentState.ToString());
		}

		private void OnBeginShutdown()
		{
			Output("Begin shutdown");

			if (application != null)
			{
				application.Activated -= OnApplicationActivated;
				application.Deactivated -= OnApplicationDeactivated;
			}

			if (buildEvents != null)
			{
				buildEvents.OnBuildBegin -= OnBuildBegin;
				buildEvents.OnBuildDone -= OnBuildDone;
			}

			if (operationState != null)
				operationState.StateChanged -= OperationStateOnStateChanged;

			if (debugService != null)
				debugService.UnadviseDebuggerEvents(debugCookie);

			if (solutionEvents != null)
			{
				solutionEvents.Opened -= OnSolutionOpened;
				solutionEvents.AfterClosing -= OnSolutionClosed;
			}

			dteEvents.OnStartupComplete -= OnStartupComplete;
			dteEvents.OnBeginShutdown -= OnBeginShutdown;
		}

		private void OnApplicationActivated(object sender, EventArgs e)
		{
			Output("Application activated");
			sm.On(VSStateMachine.Events.ReceivedFocus);
		}

		private void OnApplicationDeactivated(object sender, EventArgs e)
		{
			Output("Application deactivated");
			sm.On(VSStateMachine.Events.LostFocus);
		}

		private void OperationStateOnStateChanged(object sender, OperationStateChangedEventArgs args)
		{
			if (args.State.HasFlag(TestOperationStates.TestExecutionStarted))
			{
				Output("Test execution started");
				sm.On(VSStateMachine.Events.TestStarted);
			}
			else if (args.State.HasFlag(TestOperationStates.TestExecutionFinished))
			{
				Output("Test execution finished");
				sm.On(VSStateMachine.Events.TestFinished);
			}
		}

		private void OnSolutionOpened()
		{
			Output("Solution opened");
			sm.On(VSStateMachine.Events.SolutionOpened);
		}

		private void OnSolutionClosed()
		{
			Output("Solution closed");
			sm.On(VSStateMachine.Events.SolutionClosed);
		}

		private void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
		{
			Output("Build begin");
			sm.On(VSStateMachine.Events.BuildStarted);
		}

		private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
		{
			Output("Build done");
			sm.On(VSStateMachine.Events.BuildFinished);
		}

		int IVsDebuggerEvents.OnModeChange(DBGMODE dbgmodeNew)
		{
			switch (dbgmodeNew)
			{
				case DBGMODE.DBGMODE_Run:
					Output("Debug started");
					sm.On(VSStateMachine.Events.DebugStarted);
					break;
				case DBGMODE.DBGMODE_Design:
					Output("Debug finished");
					sm.On(VSStateMachine.Events.DebugFinished);
					break;
				default:
					Output("Debug mode changed to {0}", dbgmodeNew);
					break;
			}
			return (int) dbgmodeNew;
		}

		private void Output(string format, params object[] args)
		{
			string message;
			if (args.Length > 0)
				message = string.Format(format, args);
			else
				message = format;

			message = string.Format("[{0}] {1}\n", DateTime.Now, message);

			Debug.Write(message);

			if (outputWindow != null)
				outputWindow.OutputString(message);
		}

		private void CreateOutputWindow()
		{
			var outWindow = (IVsOutputWindow) GetGlobalService(typeof(SVsOutputWindow));

			var customGuid = new Guid("7618E147-6436-4A9D-B6D5-06527EA915A8");
			outWindow.CreatePane(ref customGuid, "Time Tracker", 1, 1);

			outWindow.GetPane(ref customGuid, out outputWindow);
		}
	}
}