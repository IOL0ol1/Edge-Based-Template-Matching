using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EdgeBasedTemplateMatching.Utility
{
    /// <summary>
    /// Stopwatch Extension Class
    /// </summary>
    public static class StopwatchEx
    {
        /// <summary>
        /// Retrieves the frequency of the performance counter.
        /// </summary>
        /// <param name="lpFrequency">Hz</param>
        /// <returns>
        /// <para>If the installed hardware supports a high-resolution performance counter, the return value is nonzero.</para>
        /// <para>If the function fails, the return value is zero.</para>
        /// </returns>
        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        /// <summary>
        /// Get high-resolution elapsed milliseconds
        /// </summary>
        /// <returns>milliseconds</returns>
        /// <exception cref="NotSupportedException"/>
        public static double GetElapsedMilliseconds(this Stopwatch stopwatch)
        {
            long freq = 1;
            if (!QueryPerformanceFrequency(out freq))
                throw new NotSupportedException();
            return (double)stopwatch.ElapsedTicks * 1000 / freq;
        }
    }
}