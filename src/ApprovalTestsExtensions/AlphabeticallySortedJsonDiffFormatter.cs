using System.Linq;
using Newtonsoft.Json.Linq;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     This formatter ensure that resulting diff has properties sorted in an alphabetical way
    /// </summary>
    public class AlphabeticallySortedJsonDiffFormatter : IJsonDiffFormatter
    {
        public string Format(JToken? diff)
        {
            if (diff == null)
            {
                return string.Empty;
            }

            if (diff is JObject jObject)
            {
                if(jObject.DeepClone() is JObject sorted)
                {
                    Sort(sorted);
                    return sorted.ToString();
                }

                return jObject.ToString();
            }
            return diff.ToString();
        }

        private static void Sort(JObject jObj)
        {
            var props = jObj.Properties().ToList();
            foreach (var prop in props)
            {
                prop.Remove();
            }

            foreach (var prop in props.OrderBy(p => p.Name))
            {
                jObj.Add(prop);
                if (prop.Value is JObject)
                    Sort((JObject)prop.Value);
            }
        }
    }
}