using Newtonsoft.Json.Linq;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    internal class DefaultJsonDiffFormatter : IJsonDiffFormatter
    {
        public string Format(JToken? diff) => diff?.ToString() ?? string.Empty;
    }
}