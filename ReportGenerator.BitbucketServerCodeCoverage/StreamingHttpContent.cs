using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace ReportGenerator.BitbucketServerCodeCoverage
{
    /// <summary>
    /// HttpContent implementation which should (hopefully) write directly to network socket
    /// (or, at least remove one unnecessary copy of data)
    /// </summary>
    internal sealed class StreamingHttpContent: HttpContent
    {
        private readonly Func<Stream, Task> _serializeAction;

        public StreamingHttpContent(Func<Stream, Task> serializeAction)
        {
            _serializeAction = serializeAction;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            await _serializeAction(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}