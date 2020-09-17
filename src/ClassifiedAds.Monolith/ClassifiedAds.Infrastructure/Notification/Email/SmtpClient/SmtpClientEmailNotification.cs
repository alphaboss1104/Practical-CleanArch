﻿using ClassifiedAds.Domain.Entities;
using ClassifiedAds.Domain.Notification;
using System.Linq;
using System.Net.Mail;

namespace ClassifiedAds.Infrastructure.Notification.Email.SmtpClient
{
    public class SmtpClientEmailNotification : IEmailNotification
    {
        private readonly SmtpClientOptions _options;

        public SmtpClientEmailNotification(SmtpClientOptions options)
        {
            _options = options;
        }

        public void Send(EmailMessage emailMessage)
        {
            var mail = new MailMessage();

            mail.From = new MailAddress(emailMessage.From);

            emailMessage.Tos?.Split(';')
                .ToList()
                .ForEach(x => mail.To.Add(x));

            emailMessage.CCs?.Split(';')
                .ToList()
                .ForEach(x => mail.CC.Add(x));

            emailMessage.BCCs?.Split(';')
                .ToList()
                .ForEach(x => mail.Bcc.Add(x));

            mail.Subject = emailMessage.Subject;

            mail.Body = emailMessage.Body;

            mail.IsBodyHtml = true;

            var smtpClient = new System.Net.Mail.SmtpClient(_options.Host);

            if (_options.Port.HasValue)
            {
                smtpClient.Port = _options.Port.Value;
            }

            if (!string.IsNullOrWhiteSpace(_options.UserName) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                smtpClient.Credentials = new System.Net.NetworkCredential(_options.UserName, _options.Password);

            }

            if (_options.EnableSsl.HasValue)
            {
                smtpClient.EnableSsl = _options.EnableSsl.Value;
            }

            smtpClient.Send(mail);
        }
    }
}
