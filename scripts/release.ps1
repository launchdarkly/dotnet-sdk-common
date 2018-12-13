dotnet clean
dotnet build -c Debug
dotnet test -c Debug test\LaunchDarkly.Common.Tests\LaunchDarkly.Common.Tests.csproj
dotnet build -c Release
del src\LaunchDarkly.Common\bin\Release\*.nupkg
del src\LaunchDarkly.Common.StrongName\bin\Release\*.nupkg
dotnet pack -c Release
dotnet nuget push src\LaunchDarkly.Common\bin\Release\*.nupkg -s https://www.nuget.org
dotnet nuget push src\LaunchDarkly.Common.StrongName\bin\Release\*.nupkg -s https://www.nuget.org
