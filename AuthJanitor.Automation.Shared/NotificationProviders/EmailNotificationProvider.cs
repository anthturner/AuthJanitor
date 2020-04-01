using AuthJanitor.Automation.Shared.Models;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AuthJanitor.Automation.Shared.NotificationProviders
{
    public class EmailNotificationProvider : INotificationProvider
    {
        public string FromEmail { get; }
        public string FromName { get; }
        public string AuthJanitorUiBase { get; }

        private readonly string _sendGridApiKey;
        public EmailNotificationProvider(
            string sendGridApiKey,
            string authJanitorUiBase,
            string fromEmail,
            string fromName = "AuthJanitor Managed Secret Agent")
        {
            _sendGridApiKey = sendGridApiKey;
            FromEmail = fromEmail;
            FromName = fromName;
            AuthJanitorUiBase = authJanitorUiBase.EndsWith("/") ? authJanitorUiBase : $"{authJanitorUiBase}/";
        }

        public async Task DispatchNotification_AdminApprovalRequiredTaskCreated(string[] toAddresses, RekeyingTask task)
        {
            if (toAddresses == null || !toAddresses.Any()) return;
            var msg = Template("AuthJanitor Administrator Approval Required");
            msg.AddBccs(toAddresses.Select(a => new EmailAddress(a)).ToList());

            msg.AddContent(MimeType.Text,
                "Administrator approval is required for a new task!" + Environment.NewLine +
                $"{new Uri(AuthJanitorUiBase + "aj/tasks/" + task.ObjectId)}");
            msg.AddContent(MimeType.Html,
                "<p>Administrator approval is required for a new task!</p><br />" +
                $"<a href=\"{new Uri(AuthJanitorUiBase + "aj/tasks/" + task.ObjectId)}\">Click here to review!</a>");

            await new SendGridClient(_sendGridApiKey).SendEmailAsync(msg);
        }

        public async Task DispatchNotification_AutoRekeyingTaskCreated(string[] toAddresses, RekeyingTask task)
        {
            if (toAddresses == null || !toAddresses.Any()) return;
            var msg = Template("AuthJanitor Auto-Rekeying Task Created");
            msg.AddBccs(toAddresses.Select(a => new EmailAddress(a)).ToList());

            msg.AddContent(MimeType.Text,
                "New Automatic Rekeying Task was created!" + Environment.NewLine +
                $"{new Uri(AuthJanitorUiBase + "aj/tasks/" + task.ObjectId)}");
            msg.AddContent(MimeType.Html,
                "<p>New Automatic Rekeying Task was created!</p><br />" +
                $"<a href=\"{new Uri(AuthJanitorUiBase + "aj/tasks/" + task.ObjectId)}\">Click here to review!</a>");

            await new SendGridClient(_sendGridApiKey).SendEmailAsync(msg);
        }

        public async Task DispatchNotification_ManagedSecretExpired(string[] toAddresses, ManagedSecret secret)
        {
            if (toAddresses == null || !toAddresses.Any()) return;
            var msg = Template("AuthJanitor Managed Secret Has Expired!");
            msg.AddBccs(toAddresses.Select(a => new EmailAddress(a)).ToList());

            msg.AddContent(MimeType.Text,
                $"An AuthJanitor Managed Secret ({secret.Name}) has expired!" + Environment.NewLine +
                $"{new Uri(AuthJanitorUiBase + "aj/secrets/" + secret.ObjectId)}");
            msg.AddContent(MimeType.Html,
                $"<p>An AuthJanitor Managed Secret (<strong>{secret.Name}</strong>) has expired!</p><br />" +
                $"<a href=\"{new Uri(AuthJanitorUiBase + "aj/secrets/" + secret.ObjectId)}\">Click here to review!</a>");

            await new SendGridClient(_sendGridApiKey).SendEmailAsync(msg);
        }

        public async Task DispatchNotification_SanityTestFailed(string[] toAddresses, Resource resource)
        {
            if (toAddresses == null || !toAddresses.Any()) return;
            var msg = Template("AuthJanitor Resource Sanity Test Failed!");
            msg.AddBccs(toAddresses.Select(a => new EmailAddress(a)).ToList());

            msg.AddContent(MimeType.Text,
                $"An AuthJanitor Resource ({resource.Name}) is not accessible with the current credential configuration." + Environment.NewLine +
                $"{new Uri(AuthJanitorUiBase + "aj/resources/" + resource.ObjectId)}");
            msg.AddContent(MimeType.Html,
                $"<p>An AuthJanitor Resource (<strong>{resource.Name}</strong>) is not accessible with the current credential configuration.</p><br />" +
                $"<a href=\"{new Uri(AuthJanitorUiBase + "aj/resources/" + resource.ObjectId)}\">Click here to review!</a>");

            await new SendGridClient(_sendGridApiKey).SendEmailAsync(msg);
        }

        private SendGridMessage Template(string subject) => new SendGridMessage() { From = new EmailAddress(FromEmail, FromName), Subject = subject };
    }
}
