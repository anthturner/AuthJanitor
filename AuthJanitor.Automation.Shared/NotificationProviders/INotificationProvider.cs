using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.NotificationProviders
{
    public interface INotificationProvider
    {
        Task DispatchNotification_AdminApprovalRequiredTaskCreated(
            string[] toAddresses,
            RekeyingTask task);

        Task DispatchNotification_SanityTestFailed(
            string[] toAddresses,
            Resource resource);

        Task DispatchNotification_ManagedSecretExpired(
            string[] toAddresses,
            ManagedSecret secret);
    }
}
