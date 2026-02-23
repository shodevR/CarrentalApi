using System;
using System.Net.Mail;
using System.Net;
using CarRentalApi.Model;

namespace CarRentalApi.Service
{
    public class MailService
    {
        private readonly string smtpHost;
        private readonly int smtpPort;
        private readonly string smtpUser;
        private readonly string smtpPassword;

        public MailService(string smtpHost, int smtpPort, string smtpUser, string smtpPassword)
        {
            this.smtpHost = smtpHost;
            this.smtpPort = smtpPort;
            this.smtpUser = smtpUser;
            this.smtpPassword = smtpPassword;
        }

        public ResponseModel SendEmail(string from, string name, string to, string subject, string body, string cc = "", string bcc = "")
        {
            var response = new ResponseModel();

            try
            {
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(from, name),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(to);

                if (!string.IsNullOrWhiteSpace(cc))
                {
                    mailMessage.CC.Add(cc);
                }

                if (!string.IsNullOrWhiteSpace(bcc))
                {
                    mailMessage.Bcc.Add(bcc);
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(smtpUser, smtpPassword)
                };

                smtpClient.Send(mailMessage);

                response.Message = "Mail sent successfully.";
                response.Status = StatusEnums.success.ToString();
            }
            catch (Exception ex)
            {
                response.Message = ex.Message;
                response.Status = StatusEnums.error.ToString();
            }

            return response;
        }
    }
}
