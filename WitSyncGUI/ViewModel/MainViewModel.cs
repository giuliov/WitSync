using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSyncGUI.Helpers;
using System.Collections.ObjectModel;
using WitSyncGUI.Model;

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

        private string[] stageNames = new string[] { "globallists", "areas", "iterations", "workitems" };
        private Type[] stage = new Type[] { typeof(GlobalListsViewModel), typeof(AreasViewModel), typeof(IterationsViewModel), typeof(WorkItemsViewModel) };
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
                    _PipelineStages = new ObservableCollection<object>();
                    // always present
                    _PipelineStages.Add(new GeneralViewModel());
                    //fill from model
                    for (int i=0; i < stageNames.Length; i++)
                    {
                        if (Repository.MappingFile.PipelineStages.Contains(stageNames[i]))
                        {
                            object obj = Activator.CreateInstance(stage[i]);
                            _PipelineStages.Add(obj);
                        }//if
                    }//for
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
