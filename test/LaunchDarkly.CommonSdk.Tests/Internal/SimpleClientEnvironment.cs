
namespace LaunchDarkly.Sdk.Internal
{
    internal class SimpleClientEnvironment : ClientEnvironment
    {
        internal static readonly SimpleClientEnvironment Instance =
            new SimpleClientEnvironment();

        public override string UserAgentType { get { return "CommonClient"; } }
    }
}
