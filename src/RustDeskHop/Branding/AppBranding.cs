namespace RustDeskHop.Branding
{
    internal static class AppBranding
    {
        #region Constants and Fields
        internal const string ICON_RESOURCE_NAME = "RustDeskHop.Assets.RustDeskHop.ico";
        internal const string LOGO_RESOURCE_NAME = "RustDeskHop.Assets.RustDeskHop.png";
        private static readonly Lazy<Icon> _applicationIcon = new Lazy<Icon>(() =>
                                                                             {
                                                                                 using Stream stream =
                                                                                     OpenResource(ICON_RESOURCE_NAME);
                                                                                 using Icon icon = new Icon(stream);
                                                                                 return (Icon)icon.Clone();
                                                                             }
                                                                            );
        private static readonly Lazy<Bitmap> _applicationLogo = new Lazy<Bitmap>(() =>
             {
                 using Stream stream = OpenResource(LOGO_RESOURCE_NAME);
                 using Image image = Image.FromStream(stream);
                 return new Bitmap(image);
             }
            );
        #endregion

        #region Properties and Indexers
        // Shared for the application's lifetime; individual windows must not dispose these.
        internal static Icon Icon => _applicationIcon.Value;
        internal static Bitmap Logo => _applicationLogo.Value;
        #endregion

        #region Methods
        private static Stream OpenResource(string name) => typeof(AppBranding).Assembly.GetManifestResourceStream(name)
                                                           ?? throw new
                                                               InvalidOperationException($"Missing application branding resource: {name}"
                                                                   );
        #endregion
    }
}