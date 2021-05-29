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
    public class ExplicitApprover
    {
        private readonly ExplicitNamer _namer;

        public ExplicitApprover([CallerFilePath]string currentTestFile = "", [CallerMemberName]string currentTestMethod = "")
        {
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

        private static async Task VerifyHttpResponse(IApprovalNamer namer, HttpResponseMessage responseMessage, params string[] ignoredPaths)
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

        private static void VerifyJson(IApprovalNamer namer, string payload, string[] ignoredPaths)
        {
            var maskedPayload = MaskIgnoredPaths(payload, ignoredPaths);
            Approvals.Verify(WriterFactory.CreateTextWriter(maskedPayload), namer, Approvals.GetReporter());
        }


        private static string MaskIgnoredPaths(string jsonPayload, params string[] ignoredPaths)
        {
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
