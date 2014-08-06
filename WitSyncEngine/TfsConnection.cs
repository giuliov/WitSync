using Microsoft.TeamFoundation.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;

namespace WitSync
{
    public class TfsConnection
    {
        public Uri CollectionUrl { get; set; }
        public string ProjectName { get; set; }
        public string Username { get; set; }
        public SecureString Password { get; set; }
        // see http://msdn.microsoft.com/en-us/library/bb130306.aspx

        public void Connect()
        {
            // TODO add support for credentials
            // e.g. http://stackoverflow.com/questions/22594064/tf30063-you-are-not-authorized-to-access-https-test-visualstudio-com-defaultc
            this.Collection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(this.CollectionUrl);
            this.Collection.EnsureAuthenticated();
        }

        public TfsTeamProjectCollection Collection { get; protected set; }
    }
}
