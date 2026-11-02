using RustDeskHop.Models;

namespace RustDeskHop.Connections
{
    internal sealed class ConnectionCoordinator(
        IRustDeskClient client,
        IRustDeskState state,
        IPrivateNetworkAccess privateNetwork,
        IPublicSignIn publicSignIn) : IConnectionWorkflow
    {
        #region Methods
        public async Task<ConnectionOutcome> ConnectAsync(TargetDefinition target,
                                                          AppSettings settings,
                                                          IConnectionInteraction interaction)
        {
            ServerProfile? profile = settings.Profiles.FirstOrDefault(p => p.Id == target.ProfileId);
            if (profile is null)
            {
                interaction
                    .ShowMessage("This client points to a missing network profile. Use Manage computers & networks to repair it.",
                                 "Configuration needed",
                                 ConnectionMessageKind.WARNING
                                );
                return ConnectionOutcome.BLOCKED;
            }

            try
            {
                string? invalidId = ComputerAddress.Validate(target.RustDeskId);
                if (invalidId is not null)
                {
                    interaction.ShowMessage(invalidId, "Invalid RustDesk ID", ConnectionMessageKind.WARNING);
                    return ConnectionOutcome.BLOCKED;
                }
                ServerProfile? current = state.DetectDefaultProfile(settings);
                if (!profile.IsPublic
                    && current is not null
                    && !string.Equals(current.Id, profile.Id, StringComparison.OrdinalIgnoreCase)
                    && !interaction.ConfirmRoute(target, profile, current))
                {
                    interaction.ReportStatus("Connection cancelled — existing sessions were not changed.");
                    return ConnectionOutcome.CANCELLED;
                }

                if (profile.RequiresPrivateNetwork && !await privateNetwork.EnsureAvailableAsync(profile, interaction))
                    return ConnectionOutcome.BLOCKED;

                string? executable = client.FindExecutable();
                if (executable is null)
                {
                    interaction.ShowMessage("RustDesk was not found in the usual installation locations.",
                                            "RustDesk not found",
                                            ConnectionMessageKind.ERROR
                                           );
                    return ConnectionOutcome.BLOCKED;
                }

                if (profile.IsPublic && !await publicSignIn.EnsureReadyAsync(executable, interaction))
                    return ConnectionOutcome.BLOCKED;

                // Recheck after every awaited probe/setup/dialog. Never launch a bare public
                // ID against a default that changed during user interaction.
                string connectionTarget = ConnectionTargetBuilder.Build(target, profile, state.ReadDefaultRoute());
                client.Connect(executable, connectionTarget);
                interaction.ReportStatus($"Connecting to {target.Name} via {profile.Name}.");
                return ConnectionOutcome.STARTED;
            }
            catch (Exception ex)
            {
                interaction.ShowMessage($"RustDesk could not be started.\n\n{ex.Message}",
                                        "Connection failed",
                                        ConnectionMessageKind.ERROR
                                       );
                return ConnectionOutcome.FAILED;
            }
        }
        #endregion
    }
}