﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Mail;
using ArcGisServerPermissionsProxy.Api.Commands.Email.Infrastructure;

namespace ArcGisServerPermissionsProxy.Api.Commands.Email
{
    public class NewUserAdminNotificationEmailCommand : EmailCommand
    {
        public NewUserAdminNotificationEmailCommand(dynamic templateData)
        {
            TemplateData = templateData;
            MessageTemplate = @"### Dear Admin,

**{{name}}** from **{{agency}}** has requested access to the **{{Description}}**.

Use the links below to **accept** or **reject** this request.

{{#acceptUrls}}
[**Accept** {{name}} as {{role}}]({{acceptUrl}})  
{{/acceptUrls}}    

[**Reject** {{name}}]({{rejectUrl}})
    
Or use the [User admininstration page]({{AdminUrl}}).";

            MailMessage.To.Add(string.Join(",", templateData.ToAddresses));
            MailMessage.From = new MailAddress(Enumerable.First(templateData.FromAddresses));

            if (templateData.FromAddresses.Length > 1)
            {
                foreach (var replyTo in templateData.FromAddresses)
                {
                    MailMessage.ReplyToList.Add(replyTo);
                }
            }

            MailMessage.Subject = string.Format("{0} - Notification of Registration", templateData.Description);

            Init();
        }

        public override sealed string MessageTemplate { get; protected internal set; }
        public override sealed dynamic TemplateData { get; protected internal set; }

        public override string ToString()
        {
            return string.Format("{0}, NewUser: {1}", "NewUserNotificationEmailCommandAsync", TemplateData);
        }

        public class MailTemplate : MailTemplateBase
        {
            public MailTemplate(string[] toAddresses, string[] fromAddresses, string name, string agency, string url,
                                string application, Guid emailToken, IEnumerable<string> roles, string description, string adminUrl)
                : base(toAddresses, fromAddresses, name, application)
            {
                Agency = agency;
                Url = url;
                Description = description;
                AcceptUrls = new Collection<AcceptModel>();
                FormatLinks(emailToken, roles, application);
                AdminUrl = adminUrl;
            }

            public string AdminUrl { get; set; }
            public string Agency { get; set; }
            public string Url { get; set; }
            public string Description { get; set; }
            public Collection<AcceptModel> AcceptUrls { get; set; }
            public string RejectUrl { get; set; }

            private void FormatLinks(Guid emailToken, IEnumerable<string> roles, string application)
            {
                foreach (var role in roles)
                {
                    AcceptUrls.Add(new AcceptModel(role, string.Format("{0}/accept?token={1}&role={2}&application={3}", Url, emailToken, role, application)));
                }

                RejectUrl = string.Format("{0}/reject?token={1}&application={2}", Url, emailToken, application);
            }

            public override string ToString()
            {
                return string.Format("Agency: {0}, Url: {1}", Agency, Url);
            }

            public class AcceptModel
            {
                public AcceptModel(string role, string url)
                {
                    Role = role;
                    AcceptUrl = url;
                }

                public string Role { get; set; }
                public string AcceptUrl { get; set; }
            }
        }
    }
}