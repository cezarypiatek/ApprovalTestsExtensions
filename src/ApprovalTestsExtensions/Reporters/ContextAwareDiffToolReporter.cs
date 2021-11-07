using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ApprovalTests.Core;
using ApprovalTests.Reporters;
using DiffEngine;

namespace SmartAnalyzers.ApprovalTestsExtensions
{
    /// <summary>
    ///     This DiffTool-based reporter automatically select preferred DiffTool based on the execution context
    /// </summary>
    public class ContextAwareDiffToolReporter: IEnvironmentAwareReporter
    {
        private readonly Lazy<DiffToolReporter?> _preferredReporter;
        
        public ContextAwareDiffToolReporter()
        {
            _preferredReporter = new Lazy<DiffToolReporter?>(() =>
            {
                if (FindPreferredTool() is { } preferredTool)
                {
                    return new DiffToolReporter(preferredTool);
                }

                return null;
            });
        }

        public void Report(string approved, string received)
        {
            if (_preferredReporter.Value == null)
            {
                throw new InvalidOperationException("Unable to find a preferred reported for current used IDE");
            }
            _preferredReporter.Value.Report(approved, received);
        }

        public bool IsWorkingInThisEnvironment(string forFile) => IsAutoDetectionEnabled() &&  _preferredReporter.Value is not null;

        private static bool IsAutoDetectionEnabled() => Environment.GetEnvironmentVariable("DiffEngine_UseContextAwareReporter")?.ToLower() is "true";

        private DiffTool? FindPreferredTool()
        {
            foreach (var process in EnumerateExecutingProcesses())
            {
                if (GetPreferredDiffTool(process) is { } preferredDiffTool)
                {
                    return preferredDiffTool;
                }
            }

            return null;
        }

        private IEnumerable<Process> EnumerateExecutingProcesses()
        {
            var process = Process.GetCurrentProcess();
            yield return process;

            // INFO: Right now there's no cross-platform way to get a chain of parent processes https://github.com/dotnet/runtime/issues/24423
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                while (ParentProcessUtilities.GetParentProcess(process.Id) is { HasExited: false } parent)
                {
                    yield return parent;
                    process = parent;
                }
            }
        }

        private static DiffTool? GetPreferredDiffTool(Process process)
        {
            return process.ProcessName?.ToLower() switch
            {
                "testhost" when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false => DiffTool.VisualStudioCode,
                "resharpertestrunner" when RuntimeInformation.IsOSPlatform(OSPlatform.Windows) == false  => DiffTool.Rider,
                "rider64" => DiffTool.Rider,
                "devenv" => DiffTool.VisualStudio,
                "omnisharp" => DiffTool.VisualStudioCode,
                _ => null
            };
        }
    }


    /// <summary>
    /// A utility class to determine a process parent.
    /// </summary>
    /// <remarks>
    /// TAKEN FROM: https://stackoverflow.com/a/3346055/876060
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ParentProcessUtilities
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            Process process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
}