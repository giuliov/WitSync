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
        public GeneralViewModel() { DisplayName = "General"; IsOptional = false; }

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
            get { return Repository.MappingFile.SourceConnection; }
            set
            {
                Repository.MappingFile.SourceConnection = value;
                RaisePropertyChanged("SourceConnection");
            }
        }

        public PipelineConfiguration.ConnectionInfo DestinationConnection
        {
            get { return Repository.MappingFile.DestinationConnection; }
            set
            {
                Repository.MappingFile.DestinationConnection = value;
                RaisePropertyChanged("DestinationConnection");
            }
        }

        public string ChangeLogFile
        {
            get { return Repository.MappingFile.ChangeLogFile; }
            set
            {
                if (Repository.MappingFile.ChangeLogFile != value)
                {
                    Repository.MappingFile.ChangeLogFile = value;
                    RaisePropertyChanged("ChangeLogFile");
                }
            }
        }
        public string LogFile
        {
            get { return Repository.MappingFile.LogFile; }
            set
            {
                if (Repository.MappingFile.LogFile != value)
                {
                    Repository.MappingFile.LogFile = value;
                    RaisePropertyChanged("LogFile");
                }
            }
        }

        public LoggingLevel Logging
        {
            get { return Repository.MappingFile.Logging; }
            set
            {
                if (Repository.MappingFile.Logging != value)
                {
                    Repository.MappingFile.Logging = value;
                    RaisePropertyChanged("Logging");
                }
            }
        }

        public bool StopPipelineOnFirstError
        {
            get { return Repository.MappingFile.StopPipelineOnFirstError; }
            set
            {
                if (Repository.MappingFile.StopPipelineOnFirstError != value)
                {
                    Repository.MappingFile.StopPipelineOnFirstError = value;
                    RaisePropertyChanged("StopPipelineOnFirstError");
                }
            }
        }

    }
}
