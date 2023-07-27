# ReportGenerator.BitbucketServerCodeCoverage
This is a [custom report plugin](https://github.com/danielpalme/ReportGenerator/wiki/Custom-reports) for [ReportGenerator tool](https://github.com/danielpalme/ReportGenerator) which produces coverage data in a json format supported by [Bitbucket Server Code Coverage plugin](https://bitbucket.org/atlassian/bitbucket-code-coverage/src/master/code-coverage-plugin/).

## How to use it
1. Install nuget package with a plugin
2. You'll have to obtain path to the plugin's main assembly. The easiest way to do it, if you start Report Generator programmatically from C#, is to get assembly information from types in runtime: `typeof(BitbucketServerCodeCoverageGenerator).Assembly.Location`. Otherwise you have to look for your inside your local nuget cache folder.
3. Basic CLI invocation of ReportGenerator with the plugin should look like this:
```shell
dotnet reportgenerator -reports:TestCover1.xml -targetdir:output -reporttypes:BitbucketServer -plugins:/path/to/ReportGenerator.BitbucketServerCodeCoverage.dll
```
4. By default, this plugin produces json file bitbucket-server-coverage.json in the output folder, that can be POSTed manually to Bitbucket Server API endpoint `https://your-server/rest/code-coverage/1.0/commits/{commit-sha}`.
5. This plugin can also push coverage report to the server automatically, for that you have to supply following environment variables:
    * `BITBUCKET_SERVER_URI` - full uri to API endpoint, including sha of commit this coverage report belongs to
    * `BITBUCKET_USERNAME` - username of an account used to access API
    * `BITBUCKET_PASSWORD` - password of an account used to access API

## Contributions
Feel free to ask questions, report bugs, or suggest improvements through Github issues.