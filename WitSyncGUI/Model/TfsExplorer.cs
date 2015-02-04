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
        bool connected = false;
        TfsConnection connection;

        internal void Connect(WitSync.PipelineConfiguration.ConnectionInfo info)
        {
            connection = new TfsConnection()
            {
                CollectionUrl = new Uri(info.CollectionUrl),
                ProjectName = info.ProjectName,
                Credential = new NetworkCredential(info.User, info.Password)
            };
            try
            {
                // connect
                connection.Connect();
                connected = true;
            }
            catch (Exception)
            {
                // log somehow
            }
        }

        internal List<string> GetAllGlobalLists()
        {
            var result = new List<string>();

            if (connected)
            {
                var sourceWIStore = connection.Collection.GetService<WorkItemStore>();
                var sourceGL = sourceWIStore.ExportGlobalLists();
                // read the XML and get only the GLOBALLIST element. 
                foreach (XmlElement glElement in sourceGL.GetElementsByTagName("GLOBALLIST"))
                {
                    string glName = glElement.Attributes["name"].Value.ToString();
                    result.Add(glName);
                }//for list
            }
            return result;
        }
    }
}
