﻿using System;

namespace AuthJanitor.Automation.Shared
{
    public class Resource : IDataStoreCompatibleStructure
    {
        public Guid ObjectId { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }

        public bool IsRekeyableObjectProvider { get; set; }
        public string ProviderType { get; set; }
        public string ProviderConfiguration { get; set; }
    }
}
