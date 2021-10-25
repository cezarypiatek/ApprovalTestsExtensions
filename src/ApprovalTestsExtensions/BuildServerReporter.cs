using ApprovalTests.Core;
using ApprovalTests.Reporters;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    public class BuildServerReporter: IEnvironmentAwareReporter
    {
        private readonly IApprovalFailureReporter _failureReporter;
        private bool? _isBuildServerEnvironment;

        public BuildServerReporter(IApprovalFailureReporter failureReporter)
        {
            _failureReporter = failureReporter;
        }

        public void Report(string approved, string received) => _failureReporter.Report(approved, received);

        public bool IsWorkingInThisEnvironment(string forFile) => _isBuildServerEnvironment ??= DefaultFrontLoaderReporter.INSTANCE.IsWorkingInThisEnvironment(forFile);
    }
}