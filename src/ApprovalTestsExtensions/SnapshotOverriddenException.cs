using System;
using ApprovalTests.Core;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    public class SnapshotOverriddenException: Exception
    {
        public SnapshotOverriddenException(IApprovalNamer namer) :
            base($"Snapshot '{namer.Name}' already checked. You are trying to override snapshot that has been already check within this test method. To avoid overrides use method overload that takes scenario name as one of the parameters. Remember to provide unique scenario names within a single test method")
        {
        }
    }
}