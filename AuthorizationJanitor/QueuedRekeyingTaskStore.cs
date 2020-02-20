using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthorizationJanitor
{
    public class QueuedRekeyingTaskStore
    {
        public Task<QueuedRekeyingTask> GetTask(Guid taskId)
        {
            return Task.FromResult(new QueuedRekeyingTask());
        }

        public Task<List<QueuedRekeyingTask>> GetTasks()
        {
            return Task.FromResult(new List<QueuedRekeyingTask>());
        }

        public Task Enqueue(params QueuedRekeyingTask[] tasks)
        {
            return Task.FromResult(0);
        }
    }
}
