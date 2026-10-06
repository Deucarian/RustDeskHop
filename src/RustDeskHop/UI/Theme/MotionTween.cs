using System.Diagnostics;

namespace RustDeskHop.UI.Theme
{
    // UI-thread animation; no worker threads, idle timer or delayed application actions.
    internal sealed class MotionTween : IDisposable
    {
        #region Constants and Fields
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer { Interval = 15 };
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Action<float> _update;
        private readonly Func<bool> _enabled;
        private float _from;
        private float _target;
        private int _duration;
        private bool _disposed;
        #endregion

        #region Constructors and Destructors
        internal MotionTween(Action<float> update, Func<bool>? enabled = null)
        {
            _update = update;
            _enabled = enabled ?? (() => UiMotion.Enabled);
            _timer.Tick += (_, _) => Tick();
        }
        #endregion

        #region Properties and Indexers
        internal float Value { get; private set; }
        internal bool IsRunning => _timer.Enabled;
        #endregion

        #region Methods
        internal void Tick() => Advance(_enabled() ? (float)_clock.Elapsed.TotalMilliseconds / _duration : 1);

        internal void To(float target, int duration = UiMotion.HOVER_DURATION)
        {
            if (_disposed || (_target == target && (IsRunning || Value == target)))
                return;

            _from = Value;
            _target = target;
            _duration = Math.Max(1, duration);
            if (!_enabled())
            {
                Finish();
                return;
            }
            _clock.Restart();
            _timer.Start();
        }

        internal void Reset(float value)
        {
            if (_disposed)
                return;

            _timer.Stop();
            _clock.Reset();
            Value = _target = value;
            _update(Value);
        }

        internal void Advance(float progress)
        {
            if (_disposed)
                return;

            if (progress >= 1)
            {
                _timer.Stop();
                _clock.Stop();
            }
            Value = _from + (_target - _from) * UiMotion.Ease(progress);
            _update(Value);
        }

        internal void Finish() => Advance(1);

        public void Dispose()
        {
            _disposed = true;
            _timer.Dispose();
            _clock.Stop();
        }
        #endregion
    }
}