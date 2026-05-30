using EventTicketSystem.Web.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System.Net;

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

        var siteUrl = config["App:SiteUrl"]?.TrimEnd('/') ?? "http://localhost:8080";

        return template
            .Replace("{{CustomerName}}",    order.CustomerName)
            .Replace("{{ConfirmationCode}}", order.ConfirmationCode)
            .Replace("{{TicketItems}}",      ticketItems)
            .Replace("{{OriginalAmount}}",   order.OriginalAmount.ToString("N0"))
            .Replace("{{DiscountRow}}",      discountRow)
            .Replace("{{TotalAmount}}",      order.TotalAmount.ToString("N0"))
            .Replace("{{SiteUrl}}",          siteUrl);
    }

    public async Task SendTempPasswordAsync(string toEmail, string userName, string tempPassword)
    {
        var smtpHost = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "smtp.gmail.com" &&
            (config["Smtp:Username"] == "your-email@gmail.com" || string.IsNullOrWhiteSpace(config["Smtp:Username"])))
        {
            logger.LogInformation("SMTP not configured — skipping temp-password email to {Email}.", toEmail);
            return;
        }

        try
        {
            var siteUrl  = config["App:SiteUrl"]?.TrimEnd('/') ?? "http://localhost:8080";
            var nameEnc  = WebUtility.HtmlEncode(userName);
            var pwdEnc   = WebUtility.HtmlEncode(tempPassword);
            var loginUrl = $"{siteUrl}/Account/Login";

            var html =
                "<!DOCTYPE html><html lang='vi'><head><meta charset='utf-8'/>" +
                "<style>" +
                "body{margin:0;padding:0;background:#f4f4f8;font-family:'Segoe UI',Arial,sans-serif;}" +
                ".w{max-width:520px;margin:32px auto;background:#fff;border-radius:12px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,.1);}" +
                ".h{background:linear-gradient(135deg,#7c4dff,#448aff);padding:28px 32px;text-align:center;color:#fff;}" +
                ".b{padding:28px 32px;}" +
                ".pwd{font-size:26px;font-weight:800;letter-spacing:4px;color:#7c4dff;background:#f3f0ff;border:2px dashed #7c4dff;border-radius:8px;text-align:center;padding:14px 20px;margin:20px 0;}" +
                ".warn{background:#fff8e1;border:1px solid #ffe082;border-radius:8px;padding:12px 16px;font-size:13px;color:#795548;}" +
                ".f{background:#fafafa;padding:18px 32px;text-align:center;border-top:1px solid #eee;font-size:12px;color:#aaa;}" +
                "</style></head><body><div class='w'>" +
                "<div class='h'><div style='font-size:40px;margin-bottom:8px;'>🔑</div>" +
                "<h2 style='margin:0;font-size:22px;'>Đặt Lại Mật Khẩu</h2>" +
                "<p style='margin:6px 0 0;opacity:.85;font-size:14px;'>Quản trị viên đã đặt lại mật khẩu của bạn</p></div>" +
                $"<div class='b'><p style='color:#333;font-size:15px;'>Xin chào <strong>{nameEnc}</strong>,</p>" +
                "<p style='color:#555;font-size:14px;'>Mật khẩu tài khoản của bạn đã được đặt lại bởi quản trị viên. Đây là mật khẩu tạm thời:</p>" +
                $"<div class='pwd'>{pwdEnc}</div>" +
                $"<div class='warn'>⚠️ <strong>Quan trọng:</strong> Vui lòng đăng nhập và <strong>đổi mật khẩu ngay lập tức</strong> để bảo vệ tài khoản. " +
                $"Đường dẫn: <a href='{loginUrl}' style='color:#7c4dff;'>{loginUrl}</a></div></div>" +
                "<div class='f'><p>© 2026 TicketHub — Email này được gửi tự động, vui lòng không trả lời.</p></div>" +
                "</div></body></html>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config["Smtp:FromName"] ?? "TicketHub", config["Smtp:FromEmail"] ?? config["Smtp:Username"]!));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "[TicketHub] Mật khẩu tài khoản của bạn đã được đặt lại";
            message.Body    = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            var port    = int.Parse(config["Smtp:Port"] ?? "587");
            var useSsl  = bool.Parse(config["Smtp:EnableSsl"] ?? "true");
            var options = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(smtpHost, port, options);
            await client.AuthenticateAsync(config["Smtp:Username"], config["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
            logger.LogInformation("Temp-password email sent to {Email}.", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send temp-password email to {Email}.", toEmail);
        }
    }

    public async Task SendPasswordResetAsync(string toEmail, string resetLink, string userName)
    {
        var smtpHost = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "smtp.gmail.com" &&
            (config["Smtp:Username"] == "your-email@gmail.com" || string.IsNullOrWhiteSpace(config["Smtp:Username"])))
        {
            logger.LogInformation("SMTP not configured — skipping password reset email to {Email}.", toEmail);
            return;
        }

        try
        {
            var templatePath = Path.Combine(env.ContentRootPath, "Templates", "Emails", "PasswordReset.html");
            var template     = await File.ReadAllTextAsync(templatePath);
            var siteUrl      = config["App:SiteUrl"]?.TrimEnd('/') ?? "http://localhost:8080";

            var html = template
                .Replace("{{UserName}}",  WebUtility.HtmlEncode(userName))
                .Replace("{{ResetLink}}", resetLink)
                .Replace("{{SiteUrl}}",   siteUrl);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                config["Smtp:FromName"] ?? "TicketHub",
                config["Smtp:FromEmail"] ?? config["Smtp:Username"]!));
            message.To.Add(new MailboxAddress(userName, toEmail));
            message.Subject = "[TicketHub] Đặt lại mật khẩu của bạn";
            message.Body    = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            var port    = int.Parse(config["Smtp:Port"] ?? "587");
            var useSsl  = bool.Parse(config["Smtp:EnableSsl"] ?? "true");
            var options = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(smtpHost, port, options);
            await client.AuthenticateAsync(config["Smtp:Username"], config["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Password reset email sent to {Email}.", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}.", toEmail);
        }
    }

    public async Task SendRefundNotificationAsync(RefundRequest refund, Order order)
    {
        var smtpHost = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(smtpHost) || smtpHost == "smtp.gmail.com" &&
            (config["Smtp:Username"] == "your-email@gmail.com" || string.IsNullOrWhiteSpace(config["Smtp:Username"])))
        {
            logger.LogInformation("SMTP not configured — skipping refund email for RefundRequest {Id}.", refund.Id);
            return;
        }

        string templateName = refund.Status switch
        {
            RefundStatus.AutoApproved => "RefundAutoApproved.html",
            RefundStatus.Approved     => "RefundProcessed.html",
            RefundStatus.Rejected     => "RefundProcessed.html",
            _                         => "RefundReceived.html"
        };

        string subject = refund.Status switch
        {
            RefundStatus.AutoApproved => $"[TicketHub] Yêu cầu hoàn vé đã được duyệt tự động – {order.ConfirmationCode}",
            RefundStatus.Approved     => $"[TicketHub] Yêu cầu hoàn vé đã được duyệt – {order.ConfirmationCode}",
            RefundStatus.Rejected     => $"[TicketHub] Yêu cầu hoàn vé đã bị từ chối – {order.ConfirmationCode}",
            _                         => $"[TicketHub] Đã tiếp nhận yêu cầu hoàn vé – {order.ConfirmationCode}"
        };

        try
        {
            var templatePath = Path.Combine(env.ContentRootPath, "Templates", "Emails", templateName);
            var template = await File.ReadAllTextAsync(templatePath);
            var html = BuildRefundHtml(template, refund, order);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                config["Smtp:FromName"] ?? "TicketHub",
                config["Smtp:FromEmail"] ?? config["Smtp:Username"]!));
            message.To.Add(new MailboxAddress(order.CustomerName, order.CustomerEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = html };

            using var client = new SmtpClient();
            var port    = int.Parse(config["Smtp:Port"] ?? "587");
            var useSsl  = bool.Parse(config["Smtp:EnableSsl"] ?? "true");
            var options = useSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(smtpHost, port, options);
            await client.AuthenticateAsync(config["Smtp:Username"], config["Smtp:Password"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            logger.LogInformation("Refund email ({Status}) sent to {Email}.", refund.Status, order.CustomerEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send refund email for RefundRequest {Id}.", refund.Id);
        }
    }

    private static string BuildRefundHtml(string template, RefundRequest refund, Order order)
    {
        var statusLabel = refund.Status switch
        {
            RefundStatus.AutoApproved => "Đã duyệt tự động (AI)",
            RefundStatus.Approved     => "Đã duyệt",
            RefundStatus.Rejected     => "Đã từ chối",
            _                         => "Đang chờ xử lý"
        };

        var reasonLabel = refund.Reason switch
        {
            RefundReason.WrongTicket      => "Mua nhầm vé",
            RefundReason.ScheduleConflict => "Trùng lịch, không thể tham dự",
            RefundReason.EventChanged     => "Sự kiện thay đổi thời gian/địa điểm",
            RefundReason.HealthReason     => "Lý do sức khỏe",
            RefundReason.PersonalReason   => "Lý do cá nhân khác",
            _                             => "Khác"
        };

        bool approved = refund.Status == RefundStatus.Approved;
        var headerClass = approved ? "approved" : "rejected";
        var headerIcon  = approved ? "✅" : "❌";
        var headerTitle = approved ? "Yêu Cầu Hoàn Vé Đã Được Duyệt!" : "Yêu Cầu Hoàn Vé Đã Bị Từ Chối";
        var headerSub   = approved ? "Tiền sẽ hoàn trong 3–5 ngày làm việc" : "Vui lòng liên hệ hỗ trợ nếu cần thêm thông tin";
        var bodyIntro   = approved
            ? "Yêu cầu hoàn vé của bạn đã được admin duyệt. Tiền sẽ được hoàn về tài khoản của bạn trong 3–5 ngày làm việc."
            : "Rất tiếc, yêu cầu hoàn vé của bạn đã bị từ chối. Nếu bạn cho rằng đây là quyết định không chính xác, vui lòng liên hệ đội ngũ hỗ trợ.";
        var adminNoteBlock = !string.IsNullOrWhiteSpace(refund.AdminNote)
            ? $"""<div class="note-box note-{headerClass}">📝 <strong>Ghi chú từ admin:</strong> {WebUtility.HtmlEncode(refund.AdminNote)}</div>"""
            : "";

        return template
            .Replace("{{CustomerName}}",    WebUtility.HtmlEncode(order.CustomerName))
            .Replace("{{ConfirmationCode}}", order.ConfirmationCode)
            .Replace("{{RefundStatus}}",    statusLabel)
            .Replace("{{RefundReason}}",    reasonLabel)
            .Replace("{{Description}}",     WebUtility.HtmlEncode(refund.Description ?? ""))
            .Replace("{{TotalAmount}}",     order.TotalAmount.ToString("N0"))
            .Replace("{{AIReason}}",        WebUtility.HtmlEncode(refund.AIDecisionReason ?? ""))
            .Replace("{{AdminNote}}",       WebUtility.HtmlEncode(refund.AdminNote ?? ""))
            .Replace("{{AdminNoteBlock}}",  adminNoteBlock)
            .Replace("{{HeaderClass}}",     headerClass)
            .Replace("{{HeaderIcon}}",      headerIcon)
            .Replace("{{HeaderTitle}}",     headerTitle)
            .Replace("{{HeaderSub}}",       headerSub)
            .Replace("{{BodyIntro}}",       bodyIntro)
            .Replace("{{CreatedAt}}",       refund.CreatedAt.ToLocalTime().ToString("HH:mm – dd/MM/yyyy"))
            .Replace("{{ProcessedAt}}",     refund.ProcessedAt?.ToLocalTime().ToString("HH:mm – dd/MM/yyyy") ?? "");
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
