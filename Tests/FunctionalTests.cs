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

    //Unfortunately, path-handling in .NET is runtime-dependent,
    //and no good platform-agnostic path-handling library or VFS exist
    //Therefore we have to copy test data with path modifications and guard them by platform
    
    [Test]
    [TestCase("win", IncludePlatform = "WIN")]
    [TestCase("unix", IncludePlatform = "UNIX")]
    public async Task ReportGeneratorWithPlugin_ShouldGenerateJson(string dir)
    {
        var (g, confBuilder, p) = AssignStep();
        var targetdir = Combine("../../../", nameof(ReportGeneratorWithPlugin_ShouldGenerateJson), dir);
        
        var conf = confBuilder.Create(new Dictionary<string, string>
        {
            ["reporttypes"] = "BitbucketServer",
            ["reports"] = Combine("./", nameof(ReportGeneratorWithPlugin_ShouldGenerateJson), dir, "TestCoverage1.xml"),
            ["targetdir"] = targetdir,
            ["verbosity"] = "Verbose",
            ["plugins"] = p,
        });

        await AssertStepAsync(g, conf, targetdir);
    }

    private static (Generator g, ReportConfigurationBuilder confBuilder, string p) AssignStep()
    {
        var g = new Generator();
        var confBuilder = new ReportConfigurationBuilder();
        var p = Combine(GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "ReportGenerator.BitbucketServerCodeCoverage.dll");
        Environment.SetEnvironmentVariable("SORT_FILE_PATHS", "1");
        return (g, confBuilder, p);
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
    [TestCase("win", IncludePlatform = "WIN")]
    [TestCase("unix", IncludePlatform = "UNIX")]
    public async Task ReportGeneratorWithPlugin_ShouldMergeCoverage(string dir)
    {
        var (g, confBuilder, p) = AssignStep();
        var targetdir = Combine("../../../", nameof(ReportGeneratorWithPlugin_ShouldMergeCoverage), dir);
        
        var conf = confBuilder.Create(new Dictionary<string, string>
        {
            ["reporttypes"] = "BitbucketServer",
            ["reports"] = Combine("./", nameof(ReportGeneratorWithPlugin_ShouldMergeCoverage), dir, "MergeTests.xml"),
            ["targetdir"] = targetdir,
            ["plugins"] = p
        });

        await AssertStepAsync(g, conf, targetdir);
    }
}