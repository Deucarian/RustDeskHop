namespace RustDeskHop.UI.Theme
{
    internal sealed class ContentTransition : IDisposable
    {
        #region Constants and Fields
        private readonly Control _target;
        private readonly Func<bool> _enabled;
        private TransitionFrame? _before;
        private TransitionOverlay? _overlay;
        private int _generation;
        #endregion

        #region Constructors and Destructors
        internal ContentTransition(Control target, Func<bool>? enabled = null)
        {
            _target = target;
            _enabled = enabled ?? (() => UiMotion.Enabled);
            target.Disposed += (_, _) => Dispose();
            target.VisibleChanged += (_, _) =>
            {
                if (!target.Visible)
                    Cancel();
            };
            target.SizeChanged += (_, _) => Cancel();
        }
        #endregion

        #region Methods
        internal void Begin()
        {
            Cancel();
            if (CanAnimate())
                _before = TransitionFrame.Capture(_target);
        }

        internal void End(bool reflow = false)
        {
            if (_before is null || !_target.IsHandleCreated)
                return;

            int generation = _generation;
            _target.BeginInvoke((Action)(() => Complete(generation, reflow)));
        }

        internal void Cancel()
        {
            _generation++;
            _before?.Dispose();
            _before = null;
            _overlay?.Finish();
            _overlay = null;
        }

        public void Dispose() => Cancel();

        private void Complete(int generation, bool reflow)
        {
            if (generation != _generation || _before is null)
                return;

            if (!CanAnimate())
            {
                Cancel();
                return;
            }
            TransitionFrame before = _before;
            _before = null;
            TransitionFrame after;
            try
            {
                after = TransitionFrame.Capture(_target);
            }
            catch
            {
                before.Dispose();
                throw;
            }
            _overlay = new TransitionOverlay(before, after, reflow, _enabled) { Bounds = _target.Bounds };
            _target.Parent!.Controls.Add(_overlay);
            _overlay.Start();
        }

        private bool CanAnimate() => _enabled() && _target.Visible && !_target.IsDisposed
            && _target.IsHandleCreated && _target.Parent is not null && _target.Width > 0 && _target.Height > 0;
        #endregion
    }
}