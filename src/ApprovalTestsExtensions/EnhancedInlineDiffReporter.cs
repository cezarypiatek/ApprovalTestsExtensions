using System;
using System.IO;
using System.Text;
using ApprovalTests.Core;
using DiffPlex;
using DiffPlex.Chunkers;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    public class ContentDifferentThanExpectedException : Exception
    {
        public string Diff { get; }
        public string ActualContent { get; }
        public string ExpectedContent { get; }
        public string ActualFileName { get; }
        public string ExpectedFileName { get; }

        public ContentDifferentThanExpectedException(string actualContent, string expectedContent, string actualFileName, string expectedFileName, string diff)
            : base($"Content of {actualFileName} and {expectedFileName} is different than expected:{Environment.NewLine}{diff}")
        {
            Diff = diff;
            ActualContent = actualContent;
            ExpectedContent = expectedContent;
            ActualFileName = actualFileName;
            ExpectedFileName = expectedFileName;
        }
    }

    public class EnhancedInlineDiffReporter: IEnvironmentAwareReporter
    {
        public bool ShowWhitespaces { get; set; }

        public void Report(string approved, string received)
        {
            var approvedContent = File.ReadAllText(approved);
            var receivedContent = File.ReadAllText(received);
            var inlineDiff = GenerateInlineDiff(approvedContent, receivedContent);
            throw new ContentDifferentThanExpectedException(receivedContent, approvedContent, received, approved, inlineDiff);
        }

        public bool IsWorkingInThisEnvironment(string forFile) => true;

        private string GenerateInlineDiff(string expected, string actual)
        {
            var differ = new Differ();
            var diffBuilder = new InlineDiffBuilder(differ);
            var diff = diffBuilder.BuildDiffModel(expected, actual, false, false, new LineEndingsPreservingChunker());

            var sb = new StringBuilder();
            var lastChanged = false;
            int lastLine = 1;
            foreach (var line in diff.Lines)
            {
                if (line.Type != ChangeType.Unchanged)
                {
                    if (lastChanged == false)
                    {
                        sb.AppendLine("===========================");
                        var linePosition = line.Position ?? lastLine;
                        sb.AppendLine($"From line {linePosition}:");
                    }

                    lastChanged = true;
                    sb.Append(GetLinePrefix(line));
                    if (ShowWhitespaces)
                    {
                        sb.Append(MakeWhitespacesVisible(line.Text));
                    }
                    else
                    {
                        sb.Append(line.Text);
                    }
                    
                }
                else
                {
                    lastChanged = false;
                }

                if (line.Type != ChangeType.Inserted)
                {
                    lastLine++;
                }
            }

            return sb.ToString();
        }

        private static readonly string CrLfVisualization = $"\u240D\u240A{Environment.NewLine}";
        private static readonly string LfVisualization = $"\u240A{Environment.NewLine}";
        private static readonly string CrVisualization = $"\u240D{Environment.NewLine}";
        private static readonly char SpaceVisualization = '\u00B7';
        private static readonly char TabVisualization = '\u2192';

        private static string MakeWhitespacesVisible(string lineText)
        {
            var middleText = lineText.Replace(' ', SpaceVisualization)
                .Replace('\t', TabVisualization);

            if (middleText.EndsWith("\r\n"))
            {
                return middleText.Replace("\r\n", CrLfVisualization);
            }

            if (middleText.EndsWith("\n"))
            {
                return middleText.Replace("\n", LfVisualization);
            }

            if (middleText.EndsWith("\r"))
            {
                return middleText.Replace("\r", CrVisualization);
            }

            return middleText;
        }

        private static string GetLinePrefix(DiffPiece line)
        {
            return line.Type switch
            {
                ChangeType.Inserted => "+ ",
                ChangeType.Deleted => "- ",
                ChangeType.Modified => "M ",
                ChangeType.Imaginary => "I ",
                _ => "  "
            };
        }
    }
}