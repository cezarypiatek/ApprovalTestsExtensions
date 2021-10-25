using Newtonsoft.Json.Linq;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     This component allows for customizing JSON diff before verifying it with Approval
    /// </summary>
    public interface IJsonDiffFormatter
    {
        string Format(JToken diff);
    }
}