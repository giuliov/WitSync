using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Framework.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;

namespace WitSync
{
    public class TfsConnection
    {
        public Uri CollectionUrl { get; set; }
        public string ProjectName { get; set; }
        public NetworkCredential Credential { protected get; set; }

        // see http://msdn.microsoft.com/en-us/library/bb130306.aspx

        public void Connect()
        {
            if (this.Credential != null && !string.IsNullOrWhiteSpace(this.Credential.UserName))
            {
                this.Collection = new TfsTeamProjectCollection(this.CollectionUrl, this.Credential);
            }
            else
            {
                this.Collection = new TfsTeamProjectCollection(this.CollectionUrl);
            }
            this.Collection.EnsureAuthenticated();
        }

        public TfsTeamProjectCollection Collection { get; protected set; }

        public string GetUsername()
        {
            TeamFoundationIdentity id;
            this.Collection.GetAuthenticatedIdentity(out id);
            return id.UniqueName;
        }
    }
}
