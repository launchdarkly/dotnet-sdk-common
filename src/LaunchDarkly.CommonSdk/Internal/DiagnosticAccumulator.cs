using System;

namespace LaunchDarkly.Common
{
    internal class DiagnosticAccumulator {
        long DataSinceDate;
        volatile DiagnosticId DiagnosticId;

        void Start(DiagnosticId diagnosticId, long dataSinceDate)
        {
            DiagnosticId = diagnosticId;
            DataSinceDate = dataSinceDate;
        }

        DiagnosticEvent.Statistics CreateEventAndReset(long droppedEvents, long deduplicatedUsers, long eventsInQueue)
        {
            long currentTime = Util.GetUnixTimestampMillis(DateTime.Now);
            DiagnosticEvent.Statistics res = new DiagnosticEvent.Statistics(currentTime, DiagnosticId, DataSinceDate,
                                                                            droppedEvents, deduplicatedUsers, eventsInQueue);
            DataSinceDate = currentTime;
            return res;
        }
    }
}
        
