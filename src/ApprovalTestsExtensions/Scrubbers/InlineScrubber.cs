using System;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    public class InlineScrubber : IScrubber
    {
        private readonly Func<string, string> _scrubber;

        public InlineScrubber(Func<string,string> scrubber)
        {
            _scrubber = scrubber;
        }

        public string Scrub(string input)
        {
            return _scrubber.Invoke(input);
        }
    }
}