using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestWindow.Extensibility;
using Color = System.Drawing.Color;
using Window = EnvDTE.Window;

namespace VSIXTimeTracker
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "0.1.0", IconResourceID = 400)]
	[Guid(PackageGuidString)]
	[ProvideAutoLoad(UIContextGuids.SolutionExists)]
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
		private WindowEvents windowEvents;
		private IVsDebugger debugService;
		private IOperationState operationState;
		private Application application;
		private uint debugCookie;
		private VSStateMachine sm;
		private PieChart chart;
		private DispatcherTimer timer;

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
			CreateStatusBarIcon();

			solutionEvents = events.SolutionEvents;
			buildEvents = events.BuildEvents;
			windowEvents = events.WindowEvents;

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
			windowEvents.WindowActivated += OnWindowActivated;

			VSColorTheme.ThemeChanged += OnThemeChanged;

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

			timer?.Stop();

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

			debugService?.UnadviseDebuggerEvents(debugCookie);

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

		private void OnWindowActivated(Window gotfocus, Window lostfocus)
		{
			Output("Window activated");
			sm.On(VSStateMachine.Events.ReceivedFocus);
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

		private void OnThemeChanged(ThemeChangedEventArgs e)
		{
			UpdateChartColors(CreateChartConfigs());
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

		private class ChartConfig
		{
			public readonly int Pos;
			public readonly States[] States;
			public readonly string Legend;
			public readonly Color Color;

			public ChartConfig(int pos, string legend, Color color, params States[] states)
			{
				Pos = pos;
				States = states;
				Legend = legend;
				Color = color;
			}
		}

		private void CreateStatusBarIcon()
		{
			var grip =
					(Control)
					FindControl(Application.Current.MainWindow, o => o is Control && ((Control) o).Name == "ResizeGripControl");

			var statusbar = (DockPanel) VisualTreeHelper.GetParent(grip);

			List<ChartConfig> chartConfigs = CreateChartConfigs();

			chart = new PieChart();
			chart.Name = "TimeTrackerStatusBarChart";
			chart.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));

			chart.Series = CreateChartSeries(chartConfigs);
			UpdateChartColors(chartConfigs);
			UpdateChartValues(chartConfigs);

			chart.AxisX.Clear();
			chart.AxisY.Clear();
			chart.InnerRadius = 2;
			chart.StartingRotationAngle = 0;
			chart.Hoverable = false;
			chart.LegendLocation = LegendLocation.None;
			chart.ChartLegend = null;
			chart.DataTooltip = null;
			chart.MinWidth = chart.MinHeight = statusbar.ActualHeight;
			chart.MaxWidth = chart.MaxHeight = statusbar.ActualHeight;
			chart.Width = chart.Height = statusbar.ActualHeight;

			DockPanel.SetDock(chart, Dock.Right);
			statusbar.Children.Insert(1, chart);


			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += (s, a) => UpdateChartValues(chartConfigs);
			timer.Start();

			//Control grip = FindControl(Application.Current.MainWindow, o => o.Name == "ResizeGripControl");

			//Debug.WriteLine("*************");
			//DebugElement(statusbar, "");
			//Debug.WriteLine("*************");
			//DebugElement(Application.Current.MainWindow, "");
		}

		private static SeriesCollection CreateChartSeries(List<ChartConfig> chartConfigs)
		{
			var result = new SeriesCollection();

			foreach (ChartConfig config in chartConfigs.OrderBy(c => c.Pos))
			{
				var series = new PieSeries
				{
					Title = config.Legend,
					Values = new ChartValues<ObservableValue>
					{
						new ObservableValue(0)
					},
					StrokeThickness = 1
				};
				result.Add(series);
			}

			int p = chartConfigs.Single(c => c.States.Contains(States.NoFocus))
					.Pos;
			((ObservableValue) result[p].Values[0]).Value = 1;

			return result;
		}

		private void UpdateChartValues(List<ChartConfig> chartConfigs)
		{
			chart.AxisX.Clear();
			chart.AxisY.Clear();

			if (sm == null)
				return;

			VSStateTimes times = sm.ElapsedTimes;

			bool hasValues = times.ElapsedMs.Values.Any(i => i > 0);

			foreach (ChartConfig config in chartConfigs)
			{
				var serie = (PieSeries) chart.Series[config.Pos];
				var val = (ObservableValue) serie.Values[0];

				long time = config.States.Sum(s => times.ElapsedMs[s]);

				if (!hasValues && config.States.Contains(States.NoFocus))
					time = 1;

				val.Value = time;
			}
		}

		private void UpdateChartColors(List<ChartConfig> chartConfigs)
		{
			foreach (ChartConfig config in chartConfigs)
			{
				var serie = (PieSeries) chart.Series[config.Pos];
				serie.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(config.Color.R, config.Color.G, config.Color.B));
			}
		}

		private static List<ChartConfig> CreateChartConfigs()
		{
			return new List<ChartConfig>
			{
				new ChartConfig(0, "Coding", VSColorTheme.GetThemedColor(EnvironmentColors.StatusBarDefaultColorKey), States.Coding),
				new ChartConfig(1, "Testing", Color.ForestGreen, States.Testing),
				new ChartConfig(2, "Debugging", VSColorTheme.GetThemedColor(EnvironmentColors.StatusBarDebuggingColorKey),
					States.Debugging),
				new ChartConfig(3, "Building", VSColorTheme.GetThemedColor(EnvironmentColors.StatusBarBuildingColorKey),
					States.Building),
				new ChartConfig(4, "Outside VS", Color.LightGray, States.NoFocus, States.NoSolution)
			};
		}

// ReSharper disable once UnusedMember.Local
		private static void DebugElement(DependencyObject o, string prefix)
		{
			var child = o as Control;
			if (child != null)
				Debug.WriteLine(prefix + child.Name + " : " + o.GetType()
						                .FullName);
			else
				Debug.WriteLine(prefix + o.GetType()
						                .FullName);

			foreach (DependencyObject c in ListChildren(o))
				DebugElement(c, prefix + "   ");
		}

		private DependencyObject FindControl(DependencyObject parent, Func<DependencyObject, bool> test)
		{
			foreach (DependencyObject child in ListChildren(parent))
			{
				if (child != null && test(child))
					return child;

				DependencyObject result = FindControl(child, test);
				if (result != null)
					return result;
			}

			return null;
		}

		private static IEnumerable<DependencyObject> ListChildren(DependencyObject parent)
		{
			int count = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < count; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);

				yield return child;
			}
		}
	}
}