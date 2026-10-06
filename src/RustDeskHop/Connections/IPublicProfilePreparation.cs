namespace RustDeskHop.Connections
{
    // Disruptive operations are deliberately absent from the ordinary launch interface.
    internal interface IPublicProfilePreparation
    {
        Task<PublicSetupResult> PrepareAsync();
    }
}