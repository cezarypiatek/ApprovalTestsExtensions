using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     Scrubber that replaces all matches of a given pattern with a placeholder.
    ///     The placeholder is generated based on the replacementPrefix and the order of the matches.
    ///     The same match will always be replaced with the same placeholder.
    /// </summary>
    public class PatternScrubber : IScrubber, IEnumerable<PatternScrubberRule>
    {
        public string Scrub(string input)
        {
            var result = input;
            foreach (var rule in _rules)
            {
                result = ScrubMatches(result, rule.Pattern, rule.ReplacementPrefix);
            }
            return result;
        }
        
        private static string ScrubMatches(string input, Regex pattern, string replacementPrefix)
        {
            var placeholderMap = new Dictionary<string, string>();
            return pattern.Replace(input, match =>
            {
                var key = match.Value;
                if (placeholderMap.TryGetValue(key, out var placeholder) == false)
                {
                    placeholderMap[key] = placeholder =  $"__{replacementPrefix}_{placeholderMap.Count}__";
                }
                return placeholder;
            });
        }
        
        private readonly List<PatternScrubberRule> _rules = new();
        
        public void Add(PatternScrubberRule rule)
        {
            _rules.Add(rule);
        }

        public IEnumerator<PatternScrubberRule> GetEnumerator()
        {
            return _rules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class PatternScrubberRule
    {
        public Regex Pattern { get; set; }
        public string ReplacementPrefix { get; set; }
    }
}