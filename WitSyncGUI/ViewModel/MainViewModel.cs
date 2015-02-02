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
    public class MainViewModel : ViewModelBase
    {
        public string Title
        {
            get
            {
                var title = GetCustomAttribute<System.Reflection.AssemblyTitleAttribute>();
                var infoVersion = GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>();
                if (string.IsNullOrWhiteSpace(Repository.Filename))
                {
                    return string.Format("{0} {1}", title.Title, infoVersion.InformationalVersion);
                }
                else
                {
                    return string.Format("{0} {1} - {2}"
                        , title.Title
                        , infoVersion.InformationalVersion
                        , System.IO.Path.GetFileNameWithoutExtension(Repository.Filename)
                        );
                }//if
            }
        }

        static private T GetCustomAttribute<T>()
            where T : Attribute
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        public string Filename
        {
            get { return Repository.Filename; }
        }

        private Type[] stage = new Type[] {  };

        private Dictionary<Type, Type> stageMapping = new Dictionary<Type, Type>()
        {
            { typeof(GlobalListsStage), typeof(GlobalListsViewModel) },
            { typeof(AreasStage),       typeof(AreasViewModel) },
            { typeof(IterationsStage),  typeof(IterationsViewModel) },
            { typeof(WorkItemsStage),   typeof(WorkItemsViewModel) }
        };

        ObservableCollection<object> _PipelineStages;
        /// <summary>
        /// Returns the collection of available workspaces to display.
        /// A 'workspace' is a ViewModel that can request to be closed.
        /// </summary>
        public ObservableCollection<object> PipelineStages
        {
            get
            {
                if (_PipelineStages == null && Repository.MappingFile != null)
                {
                    // maps each stage configuration section to a ViewModel
                    _PipelineStages = new ObservableCollection<object>();
                    // this is not a stage, but general configuration (e.g. logging) and must always be present
                    _PipelineStages.Add(new GeneralViewModel());

                    WitSync.StageInfo.Build(Repository.MappingFile, info =>
                    {
                        object obj = Activator.CreateInstance(stageMapping[info.Type]);
                        _PipelineStages.Add(obj);
                    });
                }//if
                return _PipelineStages;
            }
        }

        internal void Open(string pathToConfigurationFile)
        {
            Repository.Open(pathToConfigurationFile);
        }
    }
}
