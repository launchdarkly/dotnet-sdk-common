using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace LaunchDarkly.Common
{
    abstract class DiagnosticEvent
    {
        public readonly string kind;
        public readonly long creationDate;
        public readonly DiagnosticId id;

        internal DiagnosticEvent(string kind, long creationDate, DiagnosticId diagnosticId)
        {
            this.kind = kind;
            this.creationDate = creationDate;
            this.id = diagnosticId;
        }
    }

    internal class InitDiagnosticEvent : DiagnosticEvent
    {
        public readonly DiagnosticSdk sdk;
        public readonly Dictionary<String, Object> configuration;
        public readonly DiagnosticPlatform platform = new DiagnosticPlatform();

        internal InitDiagnosticEvent(long creationDate, DiagnosticId diagnosticId, Dictionary<String, Object> Configuration)
            : base("diagnostic-init", creationDate, diagnosticId)
        {
            this.sdk = new DiagnosticSdk();
            this.configuration = Configuration;
        }
    }

    class DiagnosticSdk
    {
        public readonly string name = "dotnet-server-sdk";

        internal DiagnosticSdk()
        {

        }
    }

    class DiagnosticPlatform
    {
        public readonly string name = "DotNet";

        internal DiagnosticPlatform()
        {

        }
    }
}
