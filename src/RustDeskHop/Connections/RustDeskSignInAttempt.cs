namespace RustDeskHop.Connections
{
    // Only observes local state; neither token presence nor a change proves server acceptance.
    internal sealed class RustDeskSignInAttempt
    {
        #region Constants and Fields
        private readonly Func<string?> _readFingerprint;
        private readonly string? _initialFingerprint;
        #endregion

        #region Constructors and Destructors
        public RustDeskSignInAttempt(Func<string?> readFingerprint)
        {
            _readFingerprint = readFingerprint;
            _initialFingerprint = readFingerprint();
        }
        #endregion

        #region Methods
        public bool CanContinue(bool userRequestedRetry = false)
        {
            string? current = _readFingerprint();
            return current is not null && (userRequestedRetry || current != _initialFingerprint);
        }
        #endregion
    }
}