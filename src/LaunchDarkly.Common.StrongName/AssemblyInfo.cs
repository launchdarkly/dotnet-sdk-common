using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LaunchDarkly.Client")]
[assembly: InternalsVisibleTo("LaunchDarkly.Tests")]

// Allow mock/proxy objects in unit tests to access internal classes
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
