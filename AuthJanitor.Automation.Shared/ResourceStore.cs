using System;
using System.Collections.Generic;

namespace AuthJanitor.Automation.Shared
{
    public class ResourceStore
    {
        public Resource GetResource(Guid id)
        {
            return new Resource();
        }

        public List<Resource> GetResources()
        {
            return new List<Resource>();
        }
    }
}
