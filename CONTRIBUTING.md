Contributing
------------

We encourage pull-requests and other contributions from the community. We've also published an [SDK contributor's guide](http://docs.launchdarkly.com/docs/sdk-contributors-guide) that provides a detailed explanation of how our SDKs work.


Getting Started
-----------------

Mac OS:

1. [Download .net core and follow instructions](https://www.microsoft.com/net/core#macos) (make sure you have 1.0.4 or higher)
1. Building for the net45 target doesn't currently work with the current set of tooling... you'll have to build the nuget artifact in windows. ~~Install Mono 5.0 from [here](http://www.mono-project.com/download/)~~
1. Run ```dotnet restore``` to pull in required packages
1. Make sure you can build and run tests from command line:

```
dotnet build src/LaunchDarkly.Common -f netstandard1.4
dotnet test test/LaunchDarkly.Common.Tests/LaunchDarkly.Common.Tests.csproj
```

To package for local use:
1. Adjust Version element in `/src/LaunchDarkly.Common/LaunchDarkly.Common.csproj` and in dependency declaration in your local app
1. `dotnet pack src/LaunchDarkly.Common`
1. Restore your app using the output directory of the previous command:
```
dotnet restore -s [.net-client repo root]/src/LaunchDarkly.Common/bin/Debug/
```