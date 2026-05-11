using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace SwiftFill.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Reading from .env (ASP.NET Core maps SmtpSettings__Host to SmtpSettings:Host)
            var host = _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var user = _config["SmtpSettings:Username"];
            var pass = _config["SmtpSettings:Password"];
            var from = _config["SmtpSettings:FromEmail"] ?? user;

            // Extract the 6-digit code for the Terminal Failsafe
            var codeMatch = System.Text.RegularExpressions.Regex.Match(body, @"\d{6}");
            var backupCode = codeMatch.Success ? codeMatch.Value : "Unknown";

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                Console.WriteLine($"[CRITICAL] SMTP Credentials missing in .env. YOUR CODE IS: {backupCode}");
                throw new Exception("SMTP credentials are not configured.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SwiftFill Support", from!));
            message.To.Add(new MailboxAddress("", to!));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = body };
            message.Body = bodyBuilder.ToMessageBody();

           using (var client = new SmtpClient())
{
    try
    {
        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        client.Timeout = 30000; 

        // FIX: Use Port 587 with StartTls. 
        // Port 465 often fails handshakes in .NET due to 'Implicit SSL' conflicts.
        await client.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

        await client.AuthenticateAsync(user, pass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        Console.WriteLine($"[SMTP SUCCESS] Real email sent to {to}");
    }
                catch (Exception ex)
                {
                    Console.WriteLine("**************************************************");
                    Console.WriteLine($"[SMTP ERROR] {ex.Message}");
                    if (ex.InnerException != null) 
                        Console.WriteLine($"[INNER ERROR] {ex.InnerException.Message}");
                    Console.WriteLine($"[BACKUP] YOUR VERIFICATION CODE IS: {backupCode}");
                    Console.WriteLine("**************************************************");
                    
                    throw; // Keeps the UI error message visible so you know it failed
                }
            }
        }
    }
}