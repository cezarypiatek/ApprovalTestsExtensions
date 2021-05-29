using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Core;
using ApprovalTests.Writers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     Main helper class that facilitate usage of ApprovalTests library.
    /// </summary>
    /// <remarks>
    ///     Use this class instead using directly static <see cref="Approvals"/>
    /// </remarks>
    public class ExplicitApprover
    {
        private readonly bool _selectedAutoApprover;
        private readonly ExplicitNamer _namer;

        /// <summary>
        ///     Set this field directly to true and run whole test suite if you want to mark all snapshots as approved
        /// </summary>
        /// <remarks>
        ///     For auto-approving snapshots from a given test, use constructor parameter instead of this field
        /// </remarks>
        public static bool UseAutoApprover { get; set; }

        public ExplicitApprover([CallerFilePath]string currentTestFile = "", [CallerMemberName]string currentTestMethod = "", bool? useAutoApprover = false)
        {
            _selectedAutoApprover = useAutoApprover ?? UseAutoApprover;
            var className = Path.GetFileNameWithoutExtension(currentTestFile);
            var directory = Path.GetDirectoryName(currentTestFile);
            _namer = new ExplicitNamer(directory!, $"{className}.{currentTestMethod}");
        }

        public async Task VerifyHttpResponse(HttpResponseMessage responseMessage, params string[] ignoredPaths)
        {
            await VerifyHttpResponse(_namer, responseMessage, ignoredPaths);
        }

        public async Task VerifyHttpResponseForScenario(string scenario, HttpResponseMessage responseMessage, params string[] ignoredPaths)
        {
            var scenarioNamer = _namer.ForScenario(scenario);
            await VerifyHttpResponse(scenarioNamer, responseMessage, ignoredPaths);
        }

        private  async Task VerifyHttpResponse(IApprovalNamer namer, HttpResponseMessage responseMessage, params string[] ignoredPaths)
        {
            var payload = await responseMessage.Content.ReadAsStringAsync();
            VerifyJson(namer, payload, ignoredPaths);
        }

        public void VerifyJson(string payload, string[] ignoredPaths)
        {
            VerifyJson(_namer, payload, ignoredPaths);
        }

        public void VerifyJsonForScenario(string scenario, string payload, string[] ignoredPaths)
        {
            var scenarioNamer = this._namer.ForScenario(scenario);
            VerifyJson(scenarioNamer, payload, ignoredPaths);
        }

        private  void VerifyJson(IApprovalNamer namer, string payload, string[] ignoredPaths)
        public void VerifyJsonDiffForScenario(string scenario, string payloadBefore, string payloadAfter)
        {
            var scenarioNamer = _namer.ForScenario(scenario);
            VerifyJsonDiff(scenarioNamer, payloadBefore, payloadAfter);
        }
        
        private void VerifyJsonDiff(IApprovalNamer namer, string payloadBefore, string payloadAfter)
        {
            var jdp = new JsonDiffPatch();
            var jsonBefore = JToken.Parse(payloadBefore);
            var jsonAfter = JToken.Parse(payloadAfter);
            var patch = jdp.Diff(jsonBefore, jsonAfter);
            var diffPayload = patch.ToString();
            VerifyJson(namer, diffPayload);
        }

        private  void VerifyJson(IApprovalNamer namer, string payload, params string[] ignoredPaths)
        {
            var maskedPayload = MaskIgnoredPaths(payload, ignoredPaths);
            var reporter = _selectedAutoApprover ? AutoApprover.INSTANCE : Approvals.GetReporter();
            Approvals.Verify(WriterFactory.CreateTextWriter(maskedPayload), namer, reporter);
        }

        private static string MaskIgnoredPaths(string jsonPayload, params string[] ignoredPaths)
        {
            if (ignoredPaths.Length == 0)
            {
                return jsonPayload;
            }
            var json = JToken.Parse(jsonPayload);
            foreach (var ignoredPath in ignoredPaths)
            {
                foreach (var token in json.SelectTokens(ignoredPath))
                {
                    switch (token)
                    {
                        case JValue jValue:
                            jValue.Value = "__IGNORED_VALUE__";
                            break;
                        case JArray jArray:
                            jArray.Clear();
                            jArray.Add("__IGNORED_VALUE__");
                            break;
                    }
                }
            }

            return json.ToString(Formatting.None);
        }
    }
}
