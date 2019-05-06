using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LaunchDarkly.Client")]
[assembly: InternalsVisibleTo("LaunchDarkly.Tests")]

[assembly: InternalsVisibleTo("LaunchDarkly.Common.Tests")]
[assembly: InternalsVisibleTo("LaunchDarkly.Xamarin")]
[assembly: InternalsVisibleTo("LaunchDarkly.Xamarin.Tests")]

// Allow *unsigned* version of LD.Client to be used with unsigned LD.Common; currently there
// is no such build of the client, but someone forking the library might do this
[assembly: InternalsVisibleTo("LaunchDarkly.Client")]
[assembly: InternalsVisibleTo("LaunchDarkly.Tests")]

// Allow mock/proxy objects in unit tests to access internal classes
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
