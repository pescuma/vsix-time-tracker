using System;
using System.Collections.Generic;
using System.Diagnostics;

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
			DebugFinished
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
				case Events.ReceivedFocus:
					states[States.NoFocus] = false;
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

		private void OnTransition(States oldState)
		{
			foreach (Stopwatch sw in stopwatches.Values)
				sw.Stop();

			States currentState = CurrentState;

			stopwatches[currentState].Start();

			if (currentState != oldState)
				StateChanged?.Invoke(currentState);
		}

		private static readonly States[] StatePriorities =
		{
			States.NoFocus,
			States.NoSolution,
			States.Building,
			States.Debugging,
			States.Testing
		};

		public States CurrentState
		{
			get
			{
				foreach (States priority in StatePriorities)
				{
					if (states[priority])
						return priority;
				}

				return States.Coding;
			}
		}
	}
}