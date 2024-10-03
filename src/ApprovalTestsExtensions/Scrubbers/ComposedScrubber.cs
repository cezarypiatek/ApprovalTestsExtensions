using System.Linq;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    public class ComposedScrubber : IScrubber
    {
        private readonly IScrubber?[] _scrubbers;

        public ComposedScrubber(params IScrubber?[] scrubbers)
        {
            _scrubbers = scrubbers;
        }

        public string Scrub(string input)
        {
            return _scrubbers.OfType<IScrubber>().Aggregate(input, (current, scrubber) => scrubber.Scrub(current));
        }
    }
}