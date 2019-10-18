using System;

namespace LaunchDarkly.Common
{
    internal class DisabledChangedArgs : EventArgs
    {
        internal bool Disabled { get; }

        DisabledChangedArgs(bool disabled)
        {
            Disabled = disabled;
        }
    }

    /// <summary>
    /// An interface provided to DefaultEventProcessor to indicate when the sending of diagnostic
    /// events should be disabled. This is used for mobile platforms to disable diagnostic events
    /// when the application is in the background.
    /// </summary>
    internal interface IDiagnosticDisabler
    {
        /// <summary>
        /// An Event listener to be called when the diagnostic event processor so switch from
        /// disabled to enabled or vice versa.
        /// </summary>
        event EventHandler<DisabledChangedArgs> DisabledChanged;
        /// <summary>
        /// Whether the DefaultEventProcessor should currently send any diagnostic events.
        /// </summary>
        bool Disabled { get; }
    }
}
