using System;
using System.Diagnostics;

namespace ConsoleApp3
{
    public class SectionTimer  : IDisposable
    {
        private readonly string _prefix;
        private readonly Stopwatch _stopwatch;

        public SectionTimer(string prefix)
        {
            _prefix = prefix;
            Debug.WriteLine(_prefix);
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            Debug.WriteLine($"{_prefix} took {_stopwatch.ElapsedMilliseconds} ms");
        }

        public long ElapsedMilliseconds => _stopwatch.ElapsedMilliseconds;
    }
}