using System.Reflection;
using Palmmedia.ReportGenerator.Core;
using static System.IO.Path;
namespace Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task ReportGeneratorWithPlugin_ShouldGenerateJson()
    {
        var g = new Generator();
        var confBuilder = new ReportConfigurationBuilder();
        var p = Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "ReportGenerator.BitbucketServerCodeCoverage.dll");
        Environment.SetEnvironmentVariable("SORT_FILE_PATHS", "1");
        var targetdir = Combine("../../../", nameof(ReportGeneratorWithPlugin_ShouldGenerateJson));
        
        var conf = confBuilder.Create(new Dictionary<string, string>
        {
            ["reporttypes"] = "BitbucketServer",
            ["reports"] = Combine("./", nameof(ReportGeneratorWithPlugin_ShouldGenerateJson), "TestCoverage1.xml"),
            ["targetdir"] = targetdir,
            ["verbosity"] = "Verbose",
            ["plugins"] = p,
        });

        await AssertStepAsync(g, conf, targetdir);
    }

    private static async Task AssertStepAsync(Generator g, ReportConfiguration conf, string targetdir)
    {
        Assert.That(g.GenerateReport(conf), Is.True);
        var fp = new FileInfo(Combine(targetdir, "bitbucket-server-coverage.json"));
        var s = new VerifySettings();
        s.UseDirectory(GetFullPath(targetdir));
        await VerifyFile(fp, s);
    }

    [Test]
    public async Task ReportGeneratorWithPlugin_ShouldMergeCoverage()
    {
        var g = new Generator();
        var confBuilder = new ReportConfigurationBuilder();
        var p = Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "ReportGenerator.BitbucketServerCodeCoverage.dll");
        Environment.SetEnvironmentVariable("SORT_FILE_PATHS", "1");
        var targetdir = Combine("../../../", nameof(ReportGeneratorWithPlugin_ShouldMergeCoverage));
        
        var conf = confBuilder.Create(new Dictionary<string, string>
        {
            ["reporttypes"] = "BitbucketServer",
            ["reports"] = Combine("./", nameof(ReportGeneratorWithPlugin_ShouldMergeCoverage), "MergeTests.xml"),
            ["targetdir"] = targetdir,
            ["plugins"] = p,
        });

        await AssertStepAsync(g, conf, targetdir);
    }
}