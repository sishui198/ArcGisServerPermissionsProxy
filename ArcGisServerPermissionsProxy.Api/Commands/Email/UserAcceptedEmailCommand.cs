﻿using System.Linq;
using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class UserAcceptedEmailCommand : EmailCommand
    {
        public UserAcceptedEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear {{Name}},

You have been granted permission to [login]({{BaseUrl}}) to the {{Application}}.

You can access the {{Application}} at `{{BaseUrl}}`.

Your user name is: **{{UserName}}**  
Your assigned role is: **{{Role}}**  
Your password is what you provided when you registered.

If you have any questions, you may reply to this email.

Thank you";

            MailMessage.To.Add(string.Join(",", templateData.ToAddresses));
            MailMessage.From = new MailAddress(Enumerable.First(templateData.FromAddresses));
            
            foreach (var replyTo in templateData.FromAddresses)
            {
                MailMessage.ReplyToList.Add(replyTo);
            }

            MailMessage.Subject = string.Format("{0} - Access Granted", templateData.Application);

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, Template: {1}, MessageTemplate: {2}", "UserAcceptedEmailCommand", TemplateData,
                                 MessageTemplate);
        }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string role,
                                string userName, string application, string baseUrl)
                : base(toAddresses, fromAddresses, name, application)
            {
                UserName = userName;
                BaseUrl = baseUrl;
                Role = role;
            }

            public string Role { get; set; }
            public string UserName { get; set; }
            public string BaseUrl { get; set; }

            public override string ToString()
            {
                return string.Format("{0}, Role: {1}, UserName: {2}", base.ToString(), Role,
                                     UserName);
            }
        }
    }
}