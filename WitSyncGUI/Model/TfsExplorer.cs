using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using WitSync;

namespace WitSyncGUI.Model
{
    class TfsExplorer
    {
        TfsConnection connection;

        internal void Connect(WitSync.PipelineConfiguration.ConnectionInfo info)
        {
            connection = new TfsConnection()
            {
                CollectionUrl = new Uri(info.CollectionUrl),
                ProjectName = info.ProjectName,
                Credential = new NetworkCredential(info.User, info.Password)
            };
            // connect
            connection.Connect();
        }

        internal List<string> GetAllGlobalLists()
        {
            var result = new List<string>();

            var sourceWIStore = connection.Collection.GetService<WorkItemStore>();
            var sourceGL = sourceWIStore.ExportGlobalLists();
            // read the XML and get only the GLOBALLIST element. 
            foreach (XmlElement glElement in sourceGL.GetElementsByTagName("GLOBALLIST"))
            {
                string glName = glElement.Attributes["name"].Value.ToString();
                result.Add(glName);
            }//for list
            return result;
        }
    }
}
