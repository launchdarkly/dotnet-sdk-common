param([string]$password = $(throw "-password is required"))

$assemblyNames = "LaunchDarkly.Common", "LaunchDarkly.Common.StrongName"
$testProject = "LaunchDarkly.Common.Tests"

#
# Simple PowerShell script for building and uploading NuGet packages from dotnet-client-common.
#
# Both LaunchDarkly.Common and LaunchDarkly.Common.StrongName will be published, in the Release
# configuration (after first building and testing in Debug configuration to make sure it works).
#
# Before you run this script, make sure:
# 1. you have the Visual Studio tools in your path (easiest way to ensure this is to run
#    Developer Command Prompt and then run PowerShell from within that)
# 2. you have set the correct project version in both LaunchDarkly.Common.csproj and
#    LaunchDarkly.Common.StrongName.csproj
# 3. you have downloaded the key file, LaunchDarkly.Common.snk, in the project root directory
# 4. you have downloaded the certificate, catamorphic_code_signing_certificate.p12, in the
#    project root directory
# 5. you have the password for the certificate (paste it after -password on the command line)_
#

# This helper function comes from https://github.com/psake/psake - it allows us to terminate the
# script if any executable returns an error code, which PowerShell won't otherwise do
function Exec
{
    [CmdletBinding()]
    param(
        [Parameter(Position=0,Mandatory=1)][scriptblock]$cmd,
        [Parameter(Position=1,Mandatory=0)][string]$errorMessage = ("Error executing command {0}" -f $cmd)
    )
    & $cmd
    if ($lastexitcode -ne 0) {
        throw ($errorMessage)
    }
}

$certFile = "catamorphic_code_signing_certificate.p12"

if (-not (Test-Path $certFile)) {
    throw ("Certificate file $certfile must be in the current directory. Download it from s3://launchdarkly-pastebin/ci/dotnet/")
}

Exec { dotnet clean }

Exec { dotnet build -c Debug }
Exec { dotnet test -c Debug test\$testProject }

foreach ($assemblyName in $assemblyNames) {
    Exec { dotnet build -c Release "src\$assemblyName" }
    $dlls = dir -Path "src\$assemblyName\bin\Release" -Filter "$assemblyName.dll" -Recurse | %{$_.FullName}
    Exec { signtool sign /f $certFile /p $password $dlls }

    del "src\$assemblyName\bin\Release\*.nupkg"
    Exec { dotnet pack -c Release "src\$assemblyName" }
}

foreach ($assemblyName in $assemblyNames) {
    dotnet nuget push "src\$assemblyName\bin\Release\*.nupkg" -s https://www.nuget.org
}
