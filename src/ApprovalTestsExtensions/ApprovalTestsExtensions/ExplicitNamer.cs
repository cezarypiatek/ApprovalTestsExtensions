using ApprovalTests.Core;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    internal class ExplicitNamer: IApprovalNamer
    {
        public string SourcePath { get; }
        public string Name { get; }

        public ExplicitNamer(string sourcePath, string name)
        {
            SourcePath = sourcePath;
            Name = name;
        }

        public IApprovalNamer ForScenario(string scenarioName)
        {
            return new ExplicitNamer(SourcePath, $"{Name}.ForScenario.{AdjustScenarioName(scenarioName)}");
        }

        private string AdjustScenarioName(string scenarioName)
        {
            return scenarioName
                .Replace(' ', '_')
                .Replace('\\','_')
                .Replace('/', '_');
        }
    }
}