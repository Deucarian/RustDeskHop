using System.Text.Json.Serialization;

namespace RustDeskHop.Models
{
    internal sealed class ServerProfile
    {
        #region Properties and Indexers
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "New profile";
        public string ServerAddress { get; set; } = "";
        public string PublicKey { get; set; } = "";
        public bool RequiresPrivateNetwork { get; set; }
        public string ProbeHost { get; set; } = "";
        public int ProbePort { get; set; } = 21116;

        [JsonIgnore]
        public bool IsPublic => string.Equals(ServerAddress.Trim(), "public", StringComparison.OrdinalIgnoreCase);
        #endregion

        #region Methods
        public override string ToString() => Name;
        #endregion
    }
}