using System;

namespace LaunchDarkly.Common
{
    internal class DisabledChangedArgs : EventArgs {
        internal bool Disabled { get; }

        DisabledChangedArgs(bool disabled) {
            Disabled = disabled;
        }
    }

    internal interface IDiagnosticDisabler {
        event EventHandler<DisabledChangedArgs> DisabledChanged;
        bool Disabled { get; }
    }
}