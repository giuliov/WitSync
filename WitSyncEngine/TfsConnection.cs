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
                if (IsVSO(this.CollectionUrl))
                {
                    // source http://blogs.msdn.com/b/buckh/archive/2013/01/07/how-to-connect-to-tf-service-without-a-prompt-for-liveid-credentials.aspx
                    BasicAuthCredential basicCred = new BasicAuthCredential(this.Credential);
                    TfsClientCredentials tfsCred = new TfsClientCredentials(basicCred);
                    tfsCred.AllowInteractive = false;
                    this.Collection = new TfsTeamProjectCollection(this.CollectionUrl, tfsCred);
                }
                else
                {
                    this.Collection = new TfsTeamProjectCollection(this.CollectionUrl, this.Credential);
                }//if
            }
            else
            {
                if (IsVSO(this.CollectionUrl))
                {
                    throw new SecurityException("VSO requires user and password");
                }
                else
                {
                    this.Collection = new TfsTeamProjectCollection(this.CollectionUrl);
                }
            }
            this.Collection.EnsureAuthenticated();
        }

        private bool IsVSO(Uri uri)
        {
            // HACK hope they do not change this again
            return uri.Host.EndsWith(".visualstudio.com", StringComparison.InvariantCultureIgnoreCase);
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
