using EventTicketSystem.Web.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace EventTicketSystem.Web.Services;

public class EmailService(IConfiguration config, IWebHostEnvironment env, ILogger<EmailService> logger)
{
    public async Task SendOrderConfirmationAsync(Order order)
    {
        var smtpHost = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "smtp.gmail.com" &&
            (config["Smtp:Username"] == "your-email@gmail.com" || string.IsNullOrWhiteSpace(config["Smtp:Username"])))
        {
            logger.LogInformation("SMTP not configured — skipping confirmation email for order {Id}.", order.Id);
            return;
        }

        try
        {
            var html = await BuildHtmlAsync(order);
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                config["Smtp:FromName"] ?? "TicketHub",
                config["Smtp:FromEmail"] ?? config["Smtp:Username"]!));
            message.To.Add(new MailboxAddress(order.CustomerName, order.CustomerEmail));
            message.Subject = $"[TicketHub] Xác nhận đặt vé – {order.ConfirmationCode}";
            message.Body = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            var port    = int.Parse(config["Smtp:Port"] ?? "587");
            var useSsl  = bool.Parse(config["Smtp:EnableSsl"] ?? "true");
            var options = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(smtpHost, port, options);
            await client.AuthenticateAsync(config["Smtp:Username"], config["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Confirmation email sent to {Email} for order {Id}.", order.CustomerEmail, order.Id);
        }
        catch (Exception ex)
        {
            // Non-fatal: log and continue — order is already confirmed in DB
            logger.LogError(ex, "Failed to send confirmation email for order {Id}.", order.Id);
        }
    }

    private async Task<string> BuildHtmlAsync(Order order)
    {
        var templatePath = Path.Combine(env.ContentRootPath, "Templates", "Emails", "OrderConfirmation.html");
        var template = await File.ReadAllTextAsync(templatePath);

        var ticketItems = BuildTicketItemsHtml(order);
        var discountRow = order.DiscountAmount > 0
            ? $"""<div class="summary-row discount"><span>Giảm giá</span><span>-{order.DiscountAmount:N0} đ</span></div>"""
            : "";

        return template
            .Replace("{{CustomerName}}",    order.CustomerName)
            .Replace("{{ConfirmationCode}}", order.ConfirmationCode)
            .Replace("{{TicketItems}}",      ticketItems)
            .Replace("{{OriginalAmount}}",   order.OriginalAmount.ToString("N0"))
            .Replace("{{DiscountRow}}",      discountRow)
            .Replace("{{TotalAmount}}",      order.TotalAmount.ToString("N0"));
    }

    private static string BuildTicketItemsHtml(Order order)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var item in order.OrderItems)
        {
            var evt      = item.TicketType?.Event;
            var evtName  = evt?.Title ?? "Sự kiện";
            var startDate = evt?.StartDate.ToLocalTime().ToString("HH:mm – dd/MM/yyyy") ?? "";
            var venue    = evt?.Venue ?? "";

            sb.Append($"""
                <div class="ticket-card">
                  <div class="ticket-header">
                    <div class="event-name">{System.Net.WebUtility.HtmlEncode(evtName)}</div>
                    <div class="event-meta">📅 {startDate} &nbsp;|&nbsp; 📍 {System.Net.WebUtility.HtmlEncode(venue)}</div>
                  </div>
                  <div class="ticket-body">
                    <div class="ticket-row">
                      <span class="label">Loại vé</span>
                      <span class="value">{System.Net.WebUtility.HtmlEncode(item.TicketType?.Name ?? "")}</span>
                    </div>
                    <div class="ticket-row">
                      <span class="label">Số lượng</span>
                      <span class="value">{item.Quantity} vé</span>
                    </div>
                    <div class="ticket-row">
                      <span class="label">Đơn giá</span>
                      <span class="value">{item.UnitPrice:N0} đ</span>
                    </div>
                    <div class="ticket-row">
                      <span class="label">Thành tiền</span>
                      <span class="value" style="color:#00897b">{item.Subtotal:N0} đ</span>
                    </div>
                  </div>
                </div>
                """);
        }
        return sb.ToString();
    }
}
