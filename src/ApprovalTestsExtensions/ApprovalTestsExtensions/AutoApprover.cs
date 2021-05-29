using System.Diagnostics;
using System.IO;
using ApprovalTests.Core;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     Taken from here https://stackoverflow.com/questions/37604285/how-do-i-automatically-approve-approval-tests-when-i-run-them/37604286#37604286
    /// </summary>
    internal class AutoApprover : IReporterWithApprovalPower
    {
        public static readonly AutoApprover INSTANCE = new AutoApprover();

        private string approved;
        private string received;

        public void Report(string approved, string received)
        {
            this.approved = approved;
            this.received = received;
            Trace.WriteLine($@"Will auto-copy ""{received}"" to ""{approved}""");
        }

        public bool ApprovedWhenReported()
        {
            if (!File.Exists(this.received)) return false;
            File.Delete(this.approved);
            if (File.Exists(this.approved)) return false;
            File.Copy(this.received, this.approved);
            if (!File.Exists(this.approved)) return false;

            return true;
        }
    }
}