namespace RustDeskHop.UI.Theme
{
    internal static class UiScale
    {
        #region Methods
        internal static float Factor(Control? control) => (control?.DeviceDpi ?? 96) / 96F
            * ((control?.FindForm() as BrandedForm)?.ScaleState.Factor ?? 1F);

        internal static int Pixels(Control control, int logical) => (int)Math.Round(logical * Factor(control));

        internal static Font FontFor(Control? control, Font original) =>
            (control?.FindForm() as BrandedForm)?.ScaleState.FontFor(original) ?? original;
        #endregion
    }
}