using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LaunchDarkly.Common.Tests")]
[assembly: InternalsVisibleTo("LaunchDarkly.Xamarin")]
[assembly: InternalsVisibleTo("LaunchDarkly.Xamarin.Tests")]

// Allow mock/proxy objects in unit tests to access internal classes
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
