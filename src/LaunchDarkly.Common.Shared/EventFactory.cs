using System;
using Newtonsoft.Json.Linq;
using LaunchDarkly.Client;

namespace LaunchDarkly.Common
{
    internal abstract class EventFactory
    {
        internal static EventFactory Default { get; } = new DefaultEventFactory();
        internal static EventFactory DefaultWithReasons { get; } = new DefaultEventFactoryWithReasons();

        internal abstract long GetTimestamp();
        internal abstract bool IncludeReasons { get; }

        internal FeatureRequestEvent NewFeatureRequestEvent(IFlagEventProperties flag, User user,
            EvaluationDetail<JToken> result, JToken defaultVal)
        {
            return new FeatureRequestEvent(GetTimestamp(), flag.Key, user, result.VariationIndex, result.Value, defaultVal,
                flag.Version, null, flag.TrackEvents, flag.DebugEventsUntilDate, false,
                IncludeReasons ? result.Reason : null);
        }

        internal FeatureRequestEvent NewDefaultFeatureRequestEvent(IFlagEventProperties flag, User user,
            JToken defaultVal, EvaluationErrorKind errorKind)
        {
            return new FeatureRequestEvent(GetTimestamp(), flag.Key, user, null, defaultVal, defaultVal,
                flag.Version, null, flag.TrackEvents, flag.DebugEventsUntilDate, false,
                IncludeReasons ? new EvaluationReason.Error(errorKind) : null);
        }

        internal FeatureRequestEvent NewUnknownFeatureRequestEvent(string key, User user,
            JToken defaultVal, EvaluationErrorKind errorKind)
        {
            return new FeatureRequestEvent(GetTimestamp(), key, user, null, defaultVal, defaultVal,
                null, null, false, null, false,
                IncludeReasons ? new EvaluationReason.Error(errorKind) : null);
        }

        internal FeatureRequestEvent NewPrerequisiteFeatureRequestEvent(IFlagEventProperties prereqFlag, User user,
            EvaluationDetail<JToken> result, IFlagEventProperties prereqOf)
        {
            return new FeatureRequestEvent(GetTimestamp(), prereqFlag.Key, user, result.VariationIndex, result.Value, null,
                prereqFlag.Version, prereqOf.Key, prereqFlag.TrackEvents, prereqFlag.DebugEventsUntilDate, false,
                IncludeReasons ? result.Reason : null);
        }

        internal FeatureRequestEvent NewDebugEvent(FeatureRequestEvent from)
        {
            return new FeatureRequestEvent(from.CreationDate, from.Key, from.User, from.Variation, from.Value, from.Default,
                from.Version, from.PrereqOf, from.TrackEvents, from.DebugEventsUntilDate, true, from.Reason);
        }

        internal CustomEvent NewCustomEvent(string key, User user, JToken data)
        {
            return new CustomEvent(GetTimestamp(), key, user, data);
        }

        internal IdentifyEvent NewIdentifyEvent(User user)
        {
            return new IdentifyEvent(GetTimestamp(), user);
        }
    }

    internal class DefaultEventFactory : EventFactory
    {
        override internal long GetTimestamp()
        {
            return Util.GetUnixTimestampMillis(DateTime.UtcNow);
        }

        override internal bool IncludeReasons => false;
    }

    internal class DefaultEventFactoryWithReasons : DefaultEventFactory
    {
        override internal bool IncludeReasons => true;
    }
}
