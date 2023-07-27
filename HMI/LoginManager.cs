using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HMI
{
    internal class LoginManager
    {
        List<string> principalGroups;
        BackgroundWorker backgroundWorkerLogin;
        private bool dataLoaded = false;
        int idGroupLogged;

        public LoginManager()
        {
            backgroundWorkerLogin = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };

            backgroundWorkerLogin.DoWork += BackgroundWorkerLoginOnDoWork;
            backgroundWorkerLogin.ProgressChanged += BackgroundWorkerLoginOnProgressChanged;
            backgroundWorkerLogin.RunWorkerAsync();
        }

        public bool getDataLoaded()
        {
            return dataLoaded;
        }
        public int getidGroupLogged()
        {
            return idGroupLogged;
        }

        private void BackgroundWorkerLoginOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lock (this)
            {
                StreamWriter sw = new StreamWriter("C:\\Temp\\log.txt");

                foreach (string group in principalGroups) sw.WriteLine(group);
                sw.Close();
                dataLoaded = true;
            }
        }
        private async void BackgroundWorkerLoginOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;

            getLoginData();
            worker.ReportProgress(0);
        }

        private void getLoginData()
        {
            PrincipalContext oamdDomain = new PrincipalContext(ContextType.Machine);
            UserPrincipal user = UserPrincipal.FindByIdentity(oamdDomain, Environment.UserName);

            principalGroups = new List<string>();
            if (user != null)
            {
                PrincipalSearchResult<Principal> groups = user.GetAuthorizationGroups();

                foreach (Principal grp in groups)
                {
                    principalGroups.Add(((GroupPrincipal)grp).ToString());
                }
            }
        }
    }
}
