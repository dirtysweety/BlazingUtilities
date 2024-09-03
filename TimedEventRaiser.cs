using System.Timers;
using Timer = System.Timers.Timer;

namespace BlazingUtilities
{
    public sealed class TimedEventRaiser : IDisposable
    {
        private readonly object _lock = new();
        private Timer? _timer;
        private readonly bool _isAsync;
        private readonly Func<Task>? _asyncCallback;
        private readonly Action? _syncCallback;
        private bool _running;
        private bool _ignoreElapsed;

        public bool Running
        {
            get
            {
                lock (_lock)
                {
                    return _running;
                }
            }
        }


        public TimedEventRaiser(int delayMilliseconds, Func<Task> asyncCallback)
        {
            _asyncCallback = asyncCallback;
            _isAsync = true;
            _timer = new Timer(TimeSpan.FromMilliseconds(delayMilliseconds));
            _timer.AutoReset = false;
            _timer.Elapsed += ElapsedAsync;
        }

        public TimedEventRaiser(int delayMilliseconds, Action syncCallback)
        {
            _syncCallback = syncCallback;
            _isAsync = false;
            _timer = new Timer(TimeSpan.FromMilliseconds(delayMilliseconds));
            _timer.AutoReset = false;
            _timer.Elapsed += Elapsed;
        }

        private void InternalElapsed()
        {
            lock (_lock)
            {
                if (_timer == null) throw new Exception("Raiser incorrectly initialized.");
                if (!_running)
                {
                    _ignoreElapsed = true; // Caused by Stop
                }
                else
                {
                    _running = false;
                }
            }
        }

        private void Elapsed(object? sender, ElapsedEventArgs e)
        {
            InternalElapsed();
            if (_ignoreElapsed)
            {
                _ignoreElapsed = false;
                return;
            }
            _syncCallback!.Invoke();
        }

        private async void ElapsedAsync(object? sender, ElapsedEventArgs e)
        {
            InternalElapsed();
            if (_ignoreElapsed)
            {
                _ignoreElapsed = false;
                return;
            }
            await _asyncCallback!.Invoke();
        }

        public void Set()
        {
            lock (_lock)
            {
                if (_timer == null) throw new Exception("Raiser incorrectly initialized.");
                if (_running)
                {
                    _running = false;
                    _timer.Stop();
                }
                _running = true;
                _timer.Start();
            }
        }

        /// <summary>
        /// Returns true if the <see cref="TimedEventRaiser"/> was actually active and got canceled.
        /// False if it was not active when the method was called.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool CancelIfRunning()
        {
            lock (_lock)
            {
                if (_timer == null) throw new Exception("Raiser incorrectly initialized.");
                if (!_running) return false;
                _running = false;
                _timer.Stop();
                return true;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_timer == null) return; //Assuming already disposed
                if (_running)
                {
                    _running = false;
                    _timer.Stop();
                }
                if (_isAsync) _timer.Elapsed -= ElapsedAsync;
                else _timer.Elapsed -= Elapsed;
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
