using Microsoft.TeamFoundation.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WitSync;

namespace WitSyncGUI.View
{
    /// <summary>
    /// Interaction logic for TfsProjectControl.xaml
    /// </summary>
    public partial class TfsProjectControl : UserControl
    {
        public TfsProjectControl()
        {
            InitializeComponent();
            (this.Content as FrameworkElement).DataContext = this;
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public string CollectionUrl
        {
            get { return (string)GetValue(CollectionUrlProperty); }
            set
            {
                SetValue(CollectionUrlProperty, value);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("CollectionUrl"));
            }
        }

        public static readonly DependencyProperty CollectionUrlProperty
            = DependencyProperty.Register(
                "CollectionUrl"
                , typeof(string)
                , typeof(TfsProjectControl), null);

        public string ProjectName
        {
            get { return (string)GetValue(ProjectNameProperty); }
            set
            {
                SetValue(ProjectNameProperty, value);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("ProjectName"));
            }
        }

        public static readonly DependencyProperty ProjectNameProperty
            = DependencyProperty.Register(
                "ProjectName"
                , typeof(string)
                , typeof(TfsProjectControl), null);

        public string UserName
        {
            get { return (string)GetValue(UserNameProperty); }
            set
            {
                SetValue(UserNameProperty, value);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("UserName"));
            }
        }

        public static readonly DependencyProperty UserNameProperty
            = DependencyProperty.Register(
                "UserName"
                , typeof(string)
                , typeof(TfsProjectControl), null);

        public string Password
        {
            get { return (string)GetValue(PasswordProperty); }
            set
            {
                SetValue(PasswordProperty, value);
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("Password"));
            }
        }

        public static readonly DependencyProperty PasswordProperty
            = DependencyProperty.Register(
                "Password"
                , typeof(string)
                , typeof(TfsProjectControl), null);


        private void SelectCollectionButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
            var result = picker.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.CollectionUrl = picker.SelectedTeamProjectCollection.Uri.AbsoluteUri;
                this.ProjectName = picker.SelectedProjects[0].Name;
            }
        }
    }
}
