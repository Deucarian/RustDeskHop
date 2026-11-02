namespace RustDeskHop.Connections
{
    internal static class ComputerAddress
    {
        #region Methods
        internal static string Normalize(string value)
        {
            string trimmed = value.Trim();

            // RustDesk displays numeric IDs in groups; accept pasted display formatting.
            return trimmed.All(c => char.IsAsciiDigit(c) || char.IsWhiteSpace(c))
                ? string.Concat(trimmed.Where(c => !char.IsWhiteSpace(c)))
                : trimmed;
        }

        internal static string? Validate(string value)
        {
            string id = Normalize(value);
            if (id.Length == 0)
                return "Enter the computer's RustDesk ID.";
            if (id.Any(char.IsWhiteSpace)
                || id.Any(char.IsControl)
                || id.IndexOfAny(['@', '?', '&', '/', '\\']) >= 0
                || id.StartsWith('-'))
                return "Enter only the RustDesk ID, without a server address or connection link.";

            return null;
        }
        #endregion
    }
}