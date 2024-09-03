namespace BlazingUtilities
{
    public class TaskQueue
    {
        private readonly object _lock = new();
        private readonly Queue<TaskCompletionSource?> _completionSources = new();
        private bool _running;


        public Task WaitAsync()
        {
            lock (_lock)
            {
                if (!_running)
                {
                    _running = true;
                    return Task.CompletedTask;
                }
                else
                {
                    var completion = new TaskCompletionSource(/*TaskCreationOptions.RunContinuationsAsynchronously*/);
                    _completionSources.Enqueue(completion);
                    return completion.Task;
                }
            }
        }

        public void Signal()
        {
            lock (_lock)
            {
                if (!_running) return;
                if (!_completionSources.TryDequeue(out var completion))
                {
                    _running = false;
                }
                else
                {
                    if (completion == null) throw new Exception("Dequeue failed");
                    completion.SetResult();
                }
            }
        }
    }
}