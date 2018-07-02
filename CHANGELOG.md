# Change log

For full release notes for the projects that depend on this project, see their respective changelogs. This file describes changes only to the common code. This project adheres to [Semantic Versioning](http://semver.org).

## [1.0.1] - 2018-07-02

### Changed
- The dependency on `Newtonsoft.JSON` now has a minimum version of 6.0.1 rather than 9.0.1. This should not affect any applications that specify a higher version for this assembly.

### Removed
- The `Identify` method is no longer part of `ILdCdCommonClient`, since it does not have the same signature in the Xamarin client as in the server-side .NET SDK.

## [1.0.0] - 2018-06-26

Initial release, corresponding to .net-client version 5.1.0.
