using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    class JsonPathScrubber: IScrubber
    {
        private readonly string[] _ignoredPaths;

        public JsonPathScrubber(string[] ignoredPaths)
        {
            _ignoredPaths = ignoredPaths;
        }

        public string Scrub(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var json = JToken.Parse(input);
            if (_ignoredPaths is { Length: > 0 })
            {
                foreach (var ignoredPath in _ignoredPaths)
                {
                    foreach (var token in json.SelectTokens(ignoredPath))
                    {
                        switch (token)
                        {
                            case JValue jValue:
                                jValue.Value = "_IGNORED_VALUE_";
                                break;
                            case JArray jArray:
                                jArray.Clear();
                                jArray.Add("_IGNORED_VALUE_");
                                break;
                            case JObject jObject:
                                jObject.Replace(new JValue("_IGNORED_VALUE_"));
                                break;
                        }
                    }
                }
            }
            return json.ToString(Formatting.Indented);
        }
    }
}