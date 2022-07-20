using System;
using ApprovalTests.Core;
using ApprovalTests.Reporters;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     This reporter serves as a decorator for underlying reporter allowing to run it only in Build Server environment
    /// </summary>
    public class BuildServerReporter: IEnvironmentAwareReporter
    {
        private readonly IApprovalFailureReporter _failureReporter;
        private bool? _isBuildServerEnvironment;

        public BuildServerReporter(IApprovalFailureReporter failureReporter)
        {
            _failureReporter = failureReporter;
        }

        public void Report(string approved, string received) => _failureReporter.Report(approved, received);

        public bool IsWorkingInThisEnvironment(string forFile) => _isBuildServerEnvironment ??= IsTeamCity() || DefaultFrontLoaderReporter.INSTANCE.IsWorkingInThisEnvironment(forFile);

        // INFO: https://github.com/JetBrains/TeamCity.VSTest.TestAdapter/issues/43
        private static bool IsTeamCity() => Environment.GetEnvironmentVariable("TEAMCITY_VERSION") != null;
    }
}