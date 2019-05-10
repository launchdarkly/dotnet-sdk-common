using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LaunchDarkly.CommonSdk.Tests")]
[assembly: InternalsVisibleTo("LaunchDarkly.XamarinSdk")]
[assembly: InternalsVisibleTo("LaunchDarkly.XamarinSdk.Tests")]

// Allow *unsigned* version of LD.ServerSdk to be used with unsigned LD.CommonSdk; currently there
// is no such build of the client, but someone forking the library might do this
[assembly: InternalsVisibleTo("LaunchDarkly.ServerSdk")]
[assembly: InternalsVisibleTo("LaunchDarkly.ServerSdk.Tests")]

// Allow mock/proxy objects in unit tests to access internal classes
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
