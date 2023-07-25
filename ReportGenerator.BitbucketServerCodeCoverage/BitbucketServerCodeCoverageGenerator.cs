using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using Palmmedia.ReportGenerator.Core.Reporting;

[assembly: InternalsVisibleTo("Tests")]

namespace ReportGenerator.BitbucketServerCodeCoverage
{
    /// <summary>
    /// ReportGenerator plugin, which converts Code Coverage to a json format used by BitbucketServer semi-official Code Coverage plugin
    /// Can either generate a json file or POST json directly to Bitbucket Server API if you set url and credentials through env variables
    /// </summary>
    public class BitbucketServerCodeCoverageGenerator: IReportBuilder
    {
        private readonly ConcurrentDictionary<string, FileCoverageForBitbucket> _filesCoverage =
            new ConcurrentDictionary<string, FileCoverageForBitbucket>();

        /// <summary>
        /// Process coverage data for class, merge if same file was already processed for another class
        /// </summary>
        /// <param name="class"></param>
        /// <param name="fileAnalyses"></param>
        public void CreateClassReport(Class @class, IEnumerable<FileAnalysis> fileAnalyses)
        {
            foreach (var file in @class.Files)
            {
                _filesCoverage.AddOrUpdate(file.Path, new FileCoverageForBitbucket(file.Path, file.LineVisitStatus), 
                    (key, oldVal) => oldVal.Merge(file.LineVisitStatus));
            }
        }

        /// <summary>
        /// Hijack SummaryReporting step to produce final json file or post json to server
        /// </summary>
        /// <param name="summaryResult"></param>
        public void CreateSummaryReport(SummaryResult summaryResult)
        {
            var uri = Environment.GetEnvironmentVariable("BITBUCKET_SERVER_URI");

            if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                SendReportToBitbucketServer(new Uri(uri));
                return;
            }
            
            using var stream = File.OpenWrite(Path.Combine(ReportContext?.ReportConfiguration.TargetDirectory ?? "./",
                "bitbucket-server-coverage.json"));
            RunConverter(stream);
        }

        private void SendReportToBitbucketServer(Uri uri)
        {
            var username = Environment.GetEnvironmentVariable("BITBUCKET_USERNAME");
            var password = Environment.GetEnvironmentVariable("BITBUCKET_PASSWORD");

            var client = new HttpClient();
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

            using var content = new StreamingHttpContent(stream =>
            {
                RunConverter(stream);
                return Task.CompletedTask;
            });
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

            var result = client.PostAsync(uri, content)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            result.EnsureSuccessStatusCode();
        }

        private void RunConverter(Stream stream)
        {
            using var converter = new FileCoverageJsonConverter(stream);
            foreach (var filesCoverageValue in _filesCoverage.Values)
            {
                converter.Write(filesCoverageValue);
            }
        }

        public string ReportType => "BitbucketServer";
        public IReportContext? ReportContext { get; set; }
    }
}