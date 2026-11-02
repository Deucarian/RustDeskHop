namespace RustDeskHop.Connections
{
    internal interface IRustDeskClient
    {
        string? FindExecutable();
        void Open(string executablePath);
        void Connect(string executablePath, string target);
    }
}