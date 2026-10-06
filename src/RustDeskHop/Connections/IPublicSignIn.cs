namespace RustDeskHop.Connections
{
    internal interface IPublicSignIn
    {
        Task<bool> EnsureReadyAsync(string rustDeskPath, IConnectionInteraction interaction);
    }
}