﻿using System.Collections.Generic;
using System.Linq;

namespace ArcGisServerPermissionProxy.Domain.Database
{
    public class Config
    {
        public Config(string[] administrativeEmails, IEnumerable<string> roles, string description)
        {
            AdministrativeEmails = administrativeEmails;
            Description = description;
            Roles = roles.Select(x=>x.ToLowerInvariant()).ToArray();
        }

        /// <summary>
        /// Gets or sets the administrative emails. These email adresses will become admin users
        /// and will have their passwords sent to them.
        /// </summary>
        /// <value>
        /// The administrative emails.
        /// </value>
        public string[] AdministrativeEmails { get; set; }

        /// <summary>
        /// Gets or sets the description that is used in emails ect.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the roles for the aplication.
        /// </summary>
        /// <value>
        /// The roles.
        /// </value>
        public string[] Roles { get; set; }
    }
}