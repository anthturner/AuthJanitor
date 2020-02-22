using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared
{
    public class RekeyingTaskStore
    {
        public Task<RekeyingTask> GetTask(Guid taskId)
        {
            return Task.FromResult(new RekeyingTask());
        }

        public Task<List<RekeyingTask>> GetTasks()
        {
            return Task.FromResult(new List<RekeyingTask>());
        }

        public Task Enqueue(params RekeyingTask[] tasks)
        {
            return Task.FromResult(0);
        }
    }
}
