using ReportGenerator.BitbucketServerCodeCoverage;

namespace Tests;

[TestFixture]
public class FileCoverageForBitbucketTests
{
    [Test]
    [TestCase(@"C:\foo", @"C:\foo\bar\baz.txt", ExpectedResult = "bar/baz.txt")]
    [TestCase(@"C:\foo", @"C:/foo/bar/baz.txt", ExpectedResult = "bar/baz.txt")]
    [TestCase(@"C:\foo", @"C:\foo/bar/baz.txt", ExpectedResult = "bar/baz.txt")]
    [TestCase(@"C:\nonfoo", @"C:\foo/bar/baz.txt", ExpectedResult = @"C:/foo/bar/baz.txt")]
    [TestCase(@"/home/user/foo", @"/home/user/foo/baz.txt", ExpectedResult = @"baz.txt")]
    public string TestPathTransform(string cwd, string filePath)
    {
        return FileCoverageForBitbucket.TransformToUnixPath(cwd, filePath);
    }
    
    [Test]
    [Platform("Unix")]
    [TestCase(@"/home/user/nonfoo", @"/home/user/foo/baz.txt",  ExpectedResult = @"/home/user/foo/baz.txt")]
    public string TestPathTransform_Unix(string cwd, string filePath)
    {
        return FileCoverageForBitbucket.TransformToUnixPath(cwd, filePath);
    }
}