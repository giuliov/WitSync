using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WitSyncGUI.Helpers;

namespace WitSyncGUI.Helpers
{
    public class StageViewModelBase : ViewModelBase
    {
        private bool _Enabled = false;
        public bool Enabled
        {
            get { return _Enabled; }
            set
            {
                if (_Enabled != value)
                {
                    _Enabled = value;
                    RaisePropertyChanged("Enabled");
                }
            }
        }

        private bool _IsOptional = true;
        public bool IsOptional
        {
            get { return _IsOptional; }
            set
            {
                if (_IsOptional != value)
                {
                    _IsOptional = value;
                    RaisePropertyChanged("IsOptional");
                }
            }
        }

        private string _DisplayName = "** UNKNOWN **";
        public string DisplayName
        {
            get { return _DisplayName; }
            set
            {
                if (_DisplayName != value)
                {
                    _DisplayName = value;
                    RaisePropertyChanged("DisplayName");
                }
            }
        }
    }

}
