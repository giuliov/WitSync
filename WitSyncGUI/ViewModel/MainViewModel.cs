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

        bool _IsSaveEnabled = false;
        public bool IsSaveEnabled
        {
            get { return _IsSaveEnabled; }
            set { _IsSaveEnabled = value; RaisePropertyChanged("IsSaveEnabled"); }
        }

        bool _IsSaveAsEnabled = false;
        public bool IsSaveAsEnabled
        {
            get { return _IsSaveAsEnabled; }
            set { _IsSaveAsEnabled = value; RaisePropertyChanged("IsSaveAsEnabled"); }
        }

        bool _IsCloseEnabled = false;
        public bool IsCloseEnabled
        {
            get { return _IsCloseEnabled; }
            set { _IsCloseEnabled = value; RaisePropertyChanged("IsCloseEnabled"); }
        }

        public bool HasPipelineStages
        {
            get { return _PipelineStages != null; }
        }

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
                    _PipelineStages.Add(new GlobalListsViewModel());
                    _PipelineStages.Add(new AreasViewModel());
                    _PipelineStages.Add(new IterationsViewModel());
                    _PipelineStages.Add(new WorkItemsViewModel());

                    WitSync.StageInfo.Build(Repository.MappingFile, info =>
                    {
                        // convention based!
                        string viewModelTypeName = "WitSyncGUI.ViewModel." + info.Type.Name.Replace("Stage", "ViewModel");
                        Type viewModelType = Type.GetType(viewModelTypeName);
                        ((StageViewModelBase)_PipelineStages.Where(s => s.GetType() == viewModelType).Single()).Enabled = true;
                    });
                }//if
                return _PipelineStages;
            }
        }

        internal void New()
        {
            Repository.New("default.yml");
            IsSaveEnabled = IsSaveAsEnabled = IsCloseEnabled = true;
        }

        internal void Open(string pathToConfigurationFile)
        {
            Repository.Open(pathToConfigurationFile);
            IsSaveEnabled = IsSaveAsEnabled = IsCloseEnabled = true;
        }

        internal void SaveAs(string p)
        {
            throw new NotImplementedException();
        }

        internal void Save()
        {
            SaveAs(Filename);
        }

        internal void Close(string pathToConfigurationFile)
        {
            //Repository.Open(pathToConfigurationFile);
            IsSaveEnabled = IsSaveAsEnabled = IsCloseEnabled = false;
        }
    }
}
