using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.ObjectPool;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;

namespace ReportGenerator.BitbucketServerCodeCoverage
{
    /// <summary>
    /// Hand-made json converter which writes coverage data in a json in a format required by Bitbucket Server.
    /// It writes directly to a provided stream as an (premature but fun) optimization
    /// TODO: Probably could be rewritten with UTF8JsonWriter, if it supports writing raw chars in a middle of a string
    /// </summary>
    internal sealed class FileCoverageJsonConverter: IDisposable
    {
        private readonly StreamWriter _writer;
        private bool _firstFile;
        private readonly ObjectPool<StringBuilder> _pool;

        /// <summary>
        /// .ctor
        /// </summary>
        /// <param name="stream">This class DOES NOT take ownership of a stream and don't dispose it!</param>
        public FileCoverageJsonConverter(Stream stream)
        {
            _writer = new StreamWriter(stream, Encoding.UTF8);
            _pool = ObjectPool.Create(new StringBuilderPooledObjectPolicy());
            _firstFile = true;
            OpenCurly();
            WritePropertyLHS("files");
            _writer.WriteLine('[');
        }

        private void WritePropertyLHS(string files)
        {
            _writer.WriteLine($"\"{files}\": ");
        }

        public void Write(FileCoverageForBitbucket fileCoverage)
        {
            if (fileCoverage.AllLinesStatus == LineVisitStatus.NotCoverable)
                return;
            
            if (!_firstFile)
                _writer.Write(',');

            _firstFile = false;
            OpenCurly();
            WritePropertyLHS("path");

            //small trick to ensure that string is json-encoded
            var path = JsonValue.Create(fileCoverage.FilePath);
            
            _writer.WriteLine($"{path!.ToJsonString()},");
            WritePropertyLHS("coverage");
            
            switch (fileCoverage.AllLinesStatus)
            {
                case LineVisitStatus.Covered:
                    _writer.Write("\"C:");
                    WriteAllLineNumbers(fileCoverage.Lines);
                    _writer.Write('"');
                    break;
                case LineVisitStatus.NotCovered:
                    _writer.Write("\"U:");
                    WriteAllLineNumbers(fileCoverage.Lines);
                    _writer.Write('"');
                    break;
                default:
                    WriteMixedCoverage(fileCoverage);
                    break;
            }
            CloseCurly();
            
        }

        private void WriteMixedCoverage(FileCoverageForBitbucket fileCoverage)
        {
            using var w1 = new LineNumberWriter('C', _pool);
            using var w2 = new LineNumberWriter('P', _pool);
            using var w3 = new LineNumberWriter('U', _pool);

            for (var i = 0; i < fileCoverage.Lines.Count; i++)
            {
                switch (fileCoverage.Lines[i])
                {
                    case LineVisitStatus.Covered:
                        w1.Write(i);
                        break;
                    case LineVisitStatus.PartiallyCovered:
                        w2.Write(i);
                        break;
                    case LineVisitStatus.NotCovered:
                        w3.Write(i);
                        break;
                }
            }

            _writer.Write("\"");
            if (!w1.IsEmpty)
                _writer.Write(w1.ToString());

            if (!w2.IsEmpty)
            {
                if (!w1.IsEmpty)
                    _writer.Write(';');
                _writer.Write(w2.ToString());
            }

            if (!w3.IsEmpty)
            {
                if (!w1.IsEmpty || !w2.IsEmpty)
                    _writer.Write(';');

                _writer.Write(w3.ToString());
            }
            
            _writer.Write("\"");
        }

        private void WriteAllLineNumbers(IReadOnlyList<LineVisitStatus> lines)
        {
            var firstLineNum = true;
            for (var i = 0; i < lines.Count; i++)
            {
                if (lines[i] != LineVisitStatus.NotCoverable)
                {
                    if (!firstLineNum)
                        _writer.Write(',');
                    _writer.Write(i); //surprisingly, it seems that line numbering is 0-based
                    firstLineNum = false;
                }
            }
        }

        private void OpenCurly() => _writer.WriteLine('{');
        private void CloseCurly() => _writer.WriteLine('}');

        public void Dispose()
        {
            _writer.WriteLine(']');
            CloseCurly();
            _writer.Flush();
        }
    }
}