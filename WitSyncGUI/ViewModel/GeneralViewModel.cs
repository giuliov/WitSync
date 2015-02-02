using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSyncGUI.Helpers;
using System.Collections.ObjectModel;
using WitSyncGUI.Model;
using WitSync;

namespace WitSyncGUI.ViewModel
{
    public class GeneralViewModel : StageViewModelBase
    {
        public GeneralViewModel() { StageName = "General"; }

        public bool IsTestRun
        {
            get { return Repository.MappingFile.TestOnly; }
            set
            {
                if (Repository.MappingFile.TestOnly != value)
                {
                    Repository.MappingFile.TestOnly = value;
                    RaisePropertyChanged("IsTestRun");
                }
            }
        }

        public PipelineConfiguration.ConnectionInfo SourceConnection
        {
            get
            {
                return Repository.MappingFile.SourceConnection;
            }
            set
            {
                Repository.MappingFile.SourceConnection = value;
                RaisePropertyChanged("SourceConnection");
            }
        }

        public PipelineConfiguration.ConnectionInfo DestinationConnection
        {
            get
            {
                return Repository.MappingFile.DestinationConnection;
            }
            set
            {
                Repository.MappingFile.DestinationConnection = value;
                RaisePropertyChanged("DestinationConnection");
            }
        }
    }
}
