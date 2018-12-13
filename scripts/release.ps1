#
# Simple PowerShell script for building and uploading NuGet packages from dotnet-client-common.
#
# Both LaunchDarkly.Common and LaunchDarkly.Common.StrongName will be published, in the Release
# configuration (after first building and testing in Debug configuration to make sure it works).
#
# Before you run this script, make sure:
# 1. you have set the correct project version in both LaunchDarkly.Common.csproj and
#    LaunchDarkly.Common.StrongName.csproj
# 2. you have downloaded the key file, LaunchDarkly.Common.snk, in the project root directory
#

dotnet clean
dotnet build -c Debug
dotnet test -c Debug test\LaunchDarkly.Common.Tests\LaunchDarkly.Common.Tests.csproj
dotnet build -c Release
del src\LaunchDarkly.Common\bin\Release\*.nupkg
del src\LaunchDarkly.Common.StrongName\bin\Release\*.nupkg
dotnet pack -c Release
dotnet nuget push src\LaunchDarkly.Common\bin\Release\*.nupkg -s https://www.nuget.org
dotnet nuget push src\LaunchDarkly.Common.StrongName\bin\Release\*.nupkg -s https://www.nuget.org
