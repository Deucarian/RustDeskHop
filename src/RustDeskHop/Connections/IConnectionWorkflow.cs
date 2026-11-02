using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal enum ConnectionOutcome
    {
        STARTED,
        CANCELLED,
        BLOCKED,
        FAILED
    }

    internal interface IConnectionWorkflow
    {
        Task<ConnectionOutcome> ConnectAsync(TargetDefinition target,
                                             AppSettings settings,
                                             IConnectionInteraction interaction);
    }
}