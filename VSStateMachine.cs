using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace VSIXTimeTracker
{
	internal class VSStateMachine
	{
		public enum Events
		{
			ReceivedFocus,
			LostFocus,
			SolutionOpened,
			SolutionClosed,
			BuildStarted,
			BuildFinished,
			TestStarted,
			TestFinished,
			DebugStarted,
			DebugFinished,
			SessionLocked,
			SessionUnlocked,
			ScreenSaverRunning,
			ScreenSaverNotRunning,
			MonitorOn,
			MonitorOff,
			LidOpened,
			LidClosed,
			SystemResumed,
			SystemSuspended
		}

		private readonly Dictionary<States, bool> states = new Dictionary<States, bool>();
		private readonly Dictionary<States, Stopwatch> stopwatches = new Dictionary<States, Stopwatch>();

		public event Action<States> StateChanged;

		public VSStateMachine()
		{
			foreach (States state in Enum.GetValues(typeof(States)))
			{
				states.Add(state, false);
				stopwatches.Add(state, new Stopwatch());
			}

			states[States.NoSolution] = true;
			stopwatches[States.NoSolution].Start();
		}

		public void On(Events trigger)
		{
			States oldState = CurrentState;

			switch (trigger)
			{
				case Events.SystemSuspended:
					states[States.SystemSuspended] = true;
					break;
				case Events.SystemResumed:
					states[States.SystemSuspended] = false;
					break;
				case Events.MonitorOff:
					states[States.MonitorOff] = true;
					break;
				case Events.MonitorOn:
					states[States.MonitorOff] = false;
					break;
				case Events.LidClosed:
					states[States.LidClosed] = true;
					break;
				case Events.LidOpened:
					states[States.LidClosed] = false;
					break;
				case Events.SessionLocked:
					states[States.SessionLocked] = true;
					break;
				case Events.SessionUnlocked:
					states[States.SessionLocked] = false;
					break;
				case Events.ScreenSaverRunning:
					states[States.ScreenSaverRunning] = true;
					break;
				case Events.ScreenSaverNotRunning:
					states[States.ScreenSaverRunning] = false;
					break;
				case Events.ReceivedFocus:
					states[States.NoFocus] = false;
					states[States.ScreenSaverRunning] = false;
					break;
				case Events.LostFocus:
					states[States.NoFocus] = true;
					break;
				case Events.SolutionOpened:
					states[States.NoSolution] = false;
					break;
				case Events.SolutionClosed:
					states[States.NoSolution] = true;
					break;
				case Events.BuildStarted:
					states[States.Building] = true;
					break;
				case Events.BuildFinished:
					states[States.Building] = false;
					break;
				case Events.TestStarted:
					states[States.Testing] = true;
					break;
				case Events.TestFinished:
					states[States.Testing] = false;
					break;
				case Events.DebugStarted:
					states[States.Debugging] = true;
					break;
				case Events.DebugFinished:
					states[States.Debugging] = false;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null);
			}

			OnTransition(oldState);
		}

		public bool IsOn(States state)
		{
			return states[state];
		}

		private void OnTransition(States oldState)
		{
			foreach (Stopwatch sw in stopwatches.Values)
				sw.Stop();

			States currentState = CurrentState;

			stopwatches[currentState].Start();

			if (currentState != oldState)
				StateChanged?.Invoke(currentState);
		}

		public States CurrentState
		{
			get
			{
				foreach (States priority in Enum.GetValues(typeof(States))
						.Cast<States>()
						.OrderBy(s => (int) s))
				{
					if (states[priority])
						return priority;
				}

				return States.Coding;
			}
		}

		public VSStateTimes ElapsedTimes
		{
			get
			{
				var result = new VSStateTimes();

				foreach (States state in Enum.GetValues(typeof(States)))
					result.ElapsedMs.Add(state, stopwatches[state].ElapsedMilliseconds);

				return result;
			}
		}
	}
}