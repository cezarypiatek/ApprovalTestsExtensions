using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ApprovalTests;
using ApprovalTests.Approvers;
using ApprovalTests.Core;
using ApprovalTests.Reporters;
using ApprovalTests.Writers;
using JsonDiffPatchDotNet;
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
        private readonly IJsonDiffFormatter _jsonDiffFormatter;
        private readonly IApprovalFailureReporter _failureReporter;
        private readonly bool _selectedAutoApprover;
        private readonly ExplicitNamer _namer;
        private readonly HashSet<string> _snapshotTracker = new HashSet<string>();

        public static IJsonSerializer Serializer = new NewtonsoftJsonSerializer();

        public static Func<IApprovalFailureReporter> DefaultFailureReporterFactory = () =>
            new FirstWorkingReporter(new BuildServerReporter(new EnhancedInlineDiffReporter()), new ContextAwareDiffToolReporter(), new DiffReporter());

        /// <summary>
        ///     Set this field directly to true and run whole test suite if you want to mark all snapshots as approved
        /// </summary>
        /// <remarks>
        ///     For auto-approving snapshots from a given test, use constructor parameter instead of this field
        /// </remarks>
        public static bool UseAutoApprover { get; set; }

        public ExplicitApprover([CallerFilePath]string currentTestFile = "", [CallerMemberName]string currentTestMethod = "", bool? useAutoApprover = null, IApprovalFailureReporter? failureReporter = null, IJsonDiffFormatter? jsonDiffFormatter = null)
        {
            _jsonDiffFormatter = jsonDiffFormatter ?? new DefaultJsonDiffFormatter();
            _failureReporter = failureReporter ?? DefaultFailureReporterFactory.Invoke();
            _selectedAutoApprover = useAutoApprover ?? UseAutoApprover;
            var className = Path.GetFileNameWithoutExtension(currentTestFile);
            var directory = Path.GetDirectoryName(currentTestFile);
            _namer = new ExplicitNamer(directory!, $"{className}.{currentTestMethod}");
        }

        /// <summary>
        ///     Read te response payload, format it as JSON and verify it with the snapshot
        /// </summary>
        ///<remarks>
        ///     If response payload contains dynamic content like Dates ord identifier then you can ignore those parts by providing
        ///     JSON paths for those elements as <see cref="ignoredPaths"/>.
        ///     More info about JSON Path syntax can be found here https://github.com/json-path/JsonPath
        /// </remarks>
        public async Task VerifyHttpResponse(HttpResponseMessage responseMessage, params string[] ignoredPaths)
        {
            await VerifyHttpResponse(_namer, responseMessage, ignoredPaths);
        }


        /// <summary>
        ///     Same as <see cref="VerifyHttpResponse"/> but should be use if there is more than shapshot to approve within a single test
        /// </summary>
        public async Task VerifyHttpResponseForScenario(string scenario, HttpResponseMessage responseMessage, params string[] ignoredPaths)
        {
            var scenarioNamer = _namer.ForScenario(scenario);
            await VerifyHttpResponse(scenarioNamer, responseMessage, ignoredPaths);
        }

        public async Task VerifyHttpResponseDiff(HttpResponseMessage firstResponseMessage, HttpResponseMessage secondResponseMessage,  params string[] ignoredPaths)
        {
            await NewMethod(_namer, firstResponseMessage, secondResponseMessage, ignoredPaths);
        }
        
        public async Task VerifyHttpResponseDiffForScenario(string scenario, HttpResponseMessage firstResponseMessage, HttpResponseMessage secondResponseMessage,  params string[] ignoredPaths)
        {
            var namer = _namer.ForScenario(scenario);
            await NewMethod(namer, firstResponseMessage, secondResponseMessage, ignoredPaths);
        }

        private async Task NewMethod(IApprovalNamer namer, HttpResponseMessage firstResponseMessage, HttpResponseMessage secondResponseMessage, string[] ignoredPaths)
        {
            var firstPayload = await firstResponseMessage.Content.ReadAsStringAsync();
            var secondPayload = await secondResponseMessage.Content.ReadAsStringAsync();
            VerifyJsonDiff(namer, firstPayload, secondPayload, ignoredPaths);
        }


        private  async Task VerifyHttpResponse(IApprovalNamer namer, HttpResponseMessage responseMessage, params string[] ignoredPaths)
        {
            var payload = await responseMessage.Content.ReadAsStringAsync();
            VerifyJson(namer, payload, ignoredPaths);
        }

        /// <summary>
        ///     Verify a given JSON payload with a snapshot.
        /// </summary>
        ///<remarks>
        ///     If JSON payload contains dynamic content like Dates ord identifier then you can ignore those parts by providing
        ///     JSON paths for those elements as <see cref="ignoredPaths"/>.
        ///     More info about JSON Path syntax can be found here https://github.com/json-path/JsonPath
        /// </remarks>
        public void VerifyJson(string payload, params string[] ignoredPaths) => VerifyJson(_namer, payload, ignoredPaths);


        /// <summary>
        ///     Same as <see cref="VerifyJson"/> but should be use if there is more than shapshot to approve within a single test
        /// </summary>
        public void VerifyJsonForScenario(string scenario, string payload, params string[] ignoredPaths)
        {
            var scenarioNamer = this._namer.ForScenario(scenario);
            VerifyJson(scenarioNamer, payload, ignoredPaths);
        }

        /// <summary>
        ///     Serialize provided object to JSON and verify with a snapshot.
        /// </summary>
        ///<remarks>
        ///     If object contains dynamic content like Dates or identifier then you can ignore those parts by providing
        ///     JSON paths for those elements as <see cref="ignoredPaths"/>.
        ///     More info about JSON Path syntax can be found here https://github.com/json-path/JsonPath
        /// </remarks>
        public void VerifyObject(object? data, params string[] ignoredPaths) => VerifyJson(_namer, SerializeObject(data), ignoredPaths);


        /// <summary>
        ///     Same as <see cref="VerifyObject"/> but should be use if there is more than shapshot to approve within a single test
        /// </summary>
        public void VerifyObjectForScenario(string scenario, object? data, params string[] ignoredPaths)
        {
            var scenarioNamer = this._namer.ForScenario(scenario);
            VerifyJson(scenarioNamer, SerializeObject(data), ignoredPaths);
        }

        private string SerializeObject(object? data) => Serializer.Serialize(data);

        /// <summary>
        ///     Calculate the diff between two json payloads and verify it with the snapshot
        /// </summary>
        /// <remarks>
        ///     More details about the JSON DIFF can be found here https://github.com/wbish/jsondiffpatch.net
        /// </remarks>
        public void VerifyJsonDiff(string payloadBefore, string payloadAfter, params string[] ignoredPaths) => VerifyJsonDiff(_namer, payloadBefore, payloadAfter, ignoredPaths);

        /// <summary>
        ///     Same as <see cref="VerifyJsonDiff"/> but should be use if there is more than shapshot to approve within a single test
        /// </summary>
        public void VerifyJsonDiffForScenario(string scenario, string payloadBefore, string payloadAfter, params string[] ignoredPaths)
        {
            var scenarioNamer = _namer.ForScenario(scenario);
            VerifyJsonDiff(scenarioNamer, payloadBefore, payloadAfter, ignoredPaths);
        }
        
        private void VerifyJsonDiff(IApprovalNamer namer, string payloadBefore, string payloadAfter, string[] ignoredPaths)
        {
            var jdp = new JsonDiffPatch();
            var jsonBefore = JToken.Parse(payloadBefore);
            var jsonAfter = JToken.Parse(payloadAfter);
            var diff = jdp.Diff(jsonBefore, jsonAfter);
            var diffPayload = _jsonDiffFormatter.Format(diff);
            VerifyJson(namer, diffPayload, ignoredPaths);
        }

        private void VerifyJson(IApprovalNamer namer, string payload, params string[] ignoredPaths)
        {
            EnsureSnapshotNotDuplicated(namer);
            var maskedPayload = MaskIgnoredPaths(payload, ignoredPaths);
            var reporter = _selectedAutoApprover ? AutoApprover.INSTANCE : _failureReporter;
            var writer = WriterFactory.CreateTextWriter(maskedPayload, "json");
            var approver = new FileApprover(writer, namer, true);
            Approvals.Verify(approver, reporter);
        }

        private static string MaskIgnoredPaths(string? jsonPayload, params string[] ignoredPaths)
        {
            if (string.IsNullOrWhiteSpace(jsonPayload))
            {
                return string.Empty;
            }

            var json = JToken.Parse(jsonPayload);
            if (ignoredPaths is { Length: > 0 })
            {
                foreach (var ignoredPath in ignoredPaths)
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

        private void EnsureSnapshotNotDuplicated(IApprovalNamer namer)
        {
            if (_snapshotTracker.Add(namer.Name) == false)
            {
                throw new SnapshotOverriddenException(namer);
            }
        }
    }
}
