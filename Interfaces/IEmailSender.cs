namespace SupportPanel.Interfaces
{
    public interface IEmailSender
    {
        Task SendAsync(string recipientEmail, string recipientName, string subject, string message);
    }
}
