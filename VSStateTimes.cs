using System.Collections.Generic;

namespace VSIXTimeTracker
{
	internal class VSStateTimes
	{
		public Dictionary<States, long> ElapsedMs = new Dictionary<States, long>();
	}
}