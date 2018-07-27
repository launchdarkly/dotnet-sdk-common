# Change log

For full release notes for the projects that depend on this project, see their respective changelogs. This file describes changes only to the common code. This project adheres to [Semantic Versioning](http://semver.org).

## [1.0.3] - 2018-07-27

### Changed
- The package `LaunchDarkly.Common` is no longer strong-named. Instead, we are now building two packages: `LaunchDarkly.Common` and `LaunchDarkly.Common.StrongName`. This is because the Xamarin project requires an unsigned version of the package, whereas the main .NET SDK uses the signed one.
- The project now uses a framework reference (`Reference`) instead of a package reference (`PackageReference`) to refer to `System.Net.Http`. An unnecessary reference to `System.Runtime` was removed.
- The stream processor now propagates an exception out of its initialization `Task` if it encounters an unrecoverable error.

## [1.0.2] - 2018-07-24

''This release is broken and should not be used.''

## [1.0.1] - 2018-07-02

### Changed
- When targeting .NET 4.5, the dependency on `Newtonsoft.Json` now has a minimum version of 6.0.1 rather than 9.0.1. This should not affect any applications that specify a higher version for this assembly.

### Removed
- The `Identify` method is no longer part of `ILdCommonClient`, since it does not have the same signature in the Xamarin client as in the server-side .NET SDK.

## [1.0.0] - 2018-06-26

Initial release, corresponding to .net-client version 5.1.0.
