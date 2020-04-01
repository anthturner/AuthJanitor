using AuthJanitor.Automation.Shared.Models;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.NotificationProviders
{
    public interface INotificationProvider
    {
        /// <summary>
        /// Dispatch a notification that an Administrator's approval is required for a new Rekeying Task
        /// </summary>
        /// <param name="toAddresses">Target users</param>
        /// <param name="task">Task which created this notification</param>
        Task DispatchNotification_AdminApprovalRequiredTaskCreated(
            string[] toAddresses,
            RekeyingTask task);

        /// <summary>
        /// Dispatch a notification that an automatic Rekeying Task was created (using Agent Identity)
        /// </summary>
        /// <param name="toAddresses">Target users</param>
        /// <param name="task">Task which created this notification</param>
        Task DispatchNotification_AutoRekeyingTaskCreated(
            string[] toAddresses,
            RekeyingTask task);

        /// <summary>
        /// Dispatch a notification that a sanity test on a Resource failed
        /// </summary>
        /// <param name="toAddresses">Target users</param>
        /// <param name="resource">Resource which created this notification</param>
        Task DispatchNotification_SanityTestFailed(
            string[] toAddresses,
            Resource resource);

        /// <summary>
        /// Dispatch a notification that a Managed Secret has expired
        /// </summary>
        /// <param name="toAddresses">Target users</param>
        /// <param name="secret">Secret which created this notification</param>
        Task DispatchNotification_ManagedSecretExpired(
            string[] toAddresses,
            ManagedSecret secret);
    }
}
