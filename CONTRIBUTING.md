# Contributing to the LaunchDarkly SDK .NET Common Code

LaunchDarkly has published an [SDK contributor's guide](https://docs.launchdarkly.com/docs/sdk-contributors-guide) that provides a detailed explanation of how our SDKs work. See below for additional information on how to contribute to this SDK.

## IMPORTANT, PLEASE READ FIRST

We are currently maintaining two major version branches. The `2.x` branch is used by versions 2.x of the LaunchDarkly .NET server-side SDK version 2.x. The `master` branch, whose version starts at 3.0.0, is used by the Xamarin SDK and will eventually be used by .NET SDK 3.x. If you are making changes that are relevant to both, please target the `2.x` branch.

## Submitting bug reports and feature requests

In general, issues should be filed in the issue trackers for the [.NET server-side SDK](https://github.com/launchdarkly/dotnet-server-sdk/issues) or the [Xamarin client-side SDK](https://github.com/launchdarkly/xamarin-client-sdk/issues) rather than in this repository, unless you have a specific implementation issue regarding the code in this repository.
 
## Submitting pull requests
 
We encourage pull requests and other contributions from the community. Before submitting pull requests, ensure that all temporary or unintended code is removed. Don't worry about adding reviewers to the pull request; the LaunchDarkly SDK team will add themselves. The SDK team will acknowledge all pull requests within two business days.
 
## Build instructions
 
### Prerequisites

To set up your SDK build time environment, you must [download .NET Core and follow the instructions](https://dotnet.microsoft.com/download) (make sure you have 1.0.4 or higher).
 
### Building
 
To install all required packages:

```
dotnet restore
```

Then, to build the SDK without running any tests:

```
dotnet build src/LaunchDarkly.CommonSdk -f netstandard1.4
```
 
### Testing
 
To run all unit tests:

```
dotnet test test/LaunchDarkly.CommonSdk.Tests/LaunchDarkly.CommonSdk.Tests.csproj
```

## Miscellaneous

This project is being developed with Visual Studio in Windows, so the source code uses Windows linefeeds. Please do not check in changes with Unix linefeeds or a mix of the two.

This project imports the `dotnet-base` repository as a subtree. See the `README.md` file in that directory for more information.

Releases are done using the release script in `dotnet-base`. Since the published package includes a .NET Framework 4.5 build, the release must be done from Windows.
