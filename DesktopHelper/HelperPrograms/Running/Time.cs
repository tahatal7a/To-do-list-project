using System;
using System.Diagnostics;

namespace DesktopHelper
{
	public static class Time
	{
		static Time()
		{
			Time.timeKeeper.Start();
			Time.TickTime();
		}
		public static void TickTime()
		{
			Time.time = (float)Time.timeKeeper.Elapsed.TotalSeconds; // Keeps track of time elapsed, to be used in future reminder features.
		}
		public static Stopwatch timeKeeper = new Stopwatch();
		public static float time;
	}
}
