using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Threading;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;

namespace ReportGenerator.BitbucketServerCodeCoverage
{
    /// <summary>
    /// This struct captures info about single file lines coverage
    /// </summary>
    internal struct FileCoverageForBitbucket
    {
        private static readonly Lazy<string> _currentWorkDir =
            new Lazy<string>(() => Path.GetFullPath(Directory.GetCurrentDirectory()), LazyThreadSafetyMode.PublicationOnly);

        public IReadOnlyList<LineVisitStatus> Lines { get; private set; }

        public LineVisitStatus AllLinesStatus { get; private set; }

        public string FilePath { get; }

        internal FileCoverageForBitbucket(string filePath, IReadOnlyList<LineVisitStatus> lines)
        {
            FilePath = TransformToUnixPath(_currentWorkDir.Value, filePath);
            Lines = lines;
            AllLinesStatus = LineVisitStatus.PartiallyCovered;
            AggregateAllLinesStatus();
        }

        /// <summary>
        /// Bitbucket server expects paths to be in unix-format and also relative to the repo root
        /// </summary>
        /// <param name="cwd">Current workdir of a ReportGenerator</param>
        /// <param name="filePath">Path to a file</param>
        /// <returns>Transformed path</returns>
        internal static string TransformToUnixPath(string cwd, string filePath)
        {
            if (Path.IsPathRooted(filePath))
            {
                //sort of "normalizing" paths
                filePath = Path.GetFullPath(filePath);
                cwd = Path.GetFullPath(cwd);
                if (filePath.StartsWith(cwd, StringComparison.OrdinalIgnoreCase))
                {
                    var idx = cwd.Length - 1;
                    do
                    {
                        idx++;
                    } while (filePath[idx] != Path.DirectorySeparatorChar && idx < filePath.Length);

                    idx++;

                    if (idx < filePath.Length)
                        filePath = filePath.Substring(idx);
                    
                }
            }

            if (Path.DirectorySeparatorChar != '/')
                filePath = filePath.Replace('\\', '/');
            
            return filePath;
        }

        /// <summary>
        /// Iterate once over all line numbers to check if all lines having same status, which we can use to simplify writing to json later
        /// The goal is to optimize memory traffic when file is fully covered/uncovered/contains non-coverable lines
        /// </summary>
        /// <returns>Single <see cref="LineVisitStatus"/> value. If there is no common status for all lines, return <see cref="LineVisitStatus.PartiallyCovered"/></returns>
        private void AggregateAllLinesStatus()
        {
            bool fullyCovered = true;
            bool fullyUncovered = true;
            bool fullyNonCoverable = true;

            foreach (var line in Lines)
            {
                if (line == LineVisitStatus.NotCoverable)
                    continue;
                else
                    fullyNonCoverable = false;

                if (line == LineVisitStatus.Covered && fullyUncovered)
                {
                    fullyUncovered = false;
                    if (!fullyCovered)
                        break;
                    
                }
                else if (line == LineVisitStatus.NotCovered && fullyCovered)
                {
                    fullyCovered = false;
                    if (!fullyUncovered)
                        break;
                }
            }

            if (fullyCovered)
                AllLinesStatus = LineVisitStatus.Covered;
            if (fullyUncovered)
                AllLinesStatus = LineVisitStatus.NotCovered;
            if (fullyNonCoverable)
                AllLinesStatus = LineVisitStatus.NotCoverable;
        }

        /// <summary>
        /// Merge multiple coverage reports for the same file
        /// (in case file contains multiple classes or probably when multiple test suites covers the same file)
        /// </summary>
        /// <param name="lines">Coverage info to merge with</param>
        /// <returns>New struct with merged info</returns>
        [Pure]
        public FileCoverageForBitbucket Merge(IReadOnlyList<LineVisitStatus> lines)
        {
            var statuses = new LineVisitStatus[Math.Max(Lines.Count, lines.Count)];
            var (shorter, longer) = lines.Count > Lines.Count ? (Lines, lines) : (lines, Lines);
            var i = 0;
            for (; i < shorter.Count; i++)
            {
                statuses[i] = MergeStatus(shorter[i], longer[i]);
            }

            for (; i < longer.Count; i++)
            {
                statuses[i] = longer[i];
            }

            Lines = statuses;
            AggregateAllLinesStatus();

            return this;
        }

        private static LineVisitStatus MergeStatus(LineVisitStatus lineVisitStatus, LineVisitStatus lineVisitStatus1)
        {
            if (lineVisitStatus == lineVisitStatus1)
                return lineVisitStatus;

            var maxStatus = (LineVisitStatus)Math.Max((int)lineVisitStatus, (int)lineVisitStatus1);
            return maxStatus;
        }
    }
}