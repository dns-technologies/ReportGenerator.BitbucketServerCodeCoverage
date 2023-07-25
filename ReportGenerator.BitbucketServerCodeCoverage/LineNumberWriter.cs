using System;
using System.Text;
using Microsoft.Extensions.ObjectPool;

namespace ReportGenerator.BitbucketServerCodeCoverage
{
    /// <summary>
    /// Little wrapper around StringBuilder that is responsible for filling line numbers of one particular type (C, P, or U)
    /// </summary>
    internal readonly struct LineNumberWriter: IDisposable
    {
        private readonly char _prefix;
        private readonly ObjectPool<StringBuilder> _pool;
        private readonly StringBuilder _builder;

        public LineNumberWriter(char prefix, ObjectPool<StringBuilder> pool)
        {
            _prefix = prefix;
            _pool = pool;
            _builder = pool.Get();
        }

        public void Write(int lineNumber)
        {
            if (IsEmpty)
                _builder.Append(_prefix).Append(':');
            else
                _builder.Append(',');
            
            _builder.Append(lineNumber);
        }

        public bool IsEmpty => _builder.Length == 0;

        public override string ToString()
        {
            return _builder.ToString();
        }

        public void Dispose()
        {
            _pool.Return(_builder);
        }
    }
}