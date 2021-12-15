using System.Diagnostics;
using ApprovalTests.Core;

namespace SmartAnalyzers.ApprovalTestsExtensions.Reporters
{
    /// <summary>
    ///     Breaks after detecting every failed snapshot and allows to continue after approving
    /// </summary>
    public class DebuggerStepApprovalFailureReporter : IReporterWithApprovalPower
    {
        private readonly IApprovalFailureReporter _failureReporter;

        public DebuggerStepApprovalFailureReporter(IApprovalFailureReporter failureReporter)
        {
            _failureReporter = failureReporter;
        }

        public bool ApprovedWhenReported() => Debugger.IsAttached;

        public void Report(string approved, string received)
        {
            _failureReporter.Report(approved, received);
            Debugger.Break();
        }
    }
}