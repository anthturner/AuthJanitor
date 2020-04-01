using System;

namespace AuthJanitor.Automation.Shared.Models
{
    public interface IAuthJanitorModel
    {
        /// <summary>
        /// Unique Object Identifier
        /// </summary>
        Guid ObjectId { get; set; }
    }
}
