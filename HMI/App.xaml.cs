using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HMI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        DispatcherTimer tmrProgressBarFill;
        LoginManager loginManager = new LoginManager();
        int progressCounter = 0;
        SplashScreenWindow splashScreen;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            splashScreen = new SplashScreenWindow();
            this.MainWindow = splashScreen;
            splashScreen.Show();

            tmrProgressBarFill = new DispatcherTimer();
            tmrProgressBarFill.Tick += new EventHandler(tmrProgressBarFill_tick);
            tmrProgressBarFill.Interval = new TimeSpan(0, 0, 1);
            Task.Factory.StartNew(() => { tmrProgressBarFill.Start(); });
        }

        void tmrProgressBarFill_tick(object sender, EventArgs e)
        {
            lock(loginManager)
            {
                if (loginManager.getDataLoaded())
                {
                    tmrProgressBarFill.Stop();
                    progressCounter = 100;
                    this.Dispatcher.Invoke(() =>
                    {
                        var mainWindow = new MainWindow();
                        this.MainWindow = mainWindow;
                        mainWindow.Show();
                        splashScreen.Close();
                    });
                }
                else
                {
                    progressCounter++;
                    splashScreen.Dispatcher.Invoke(() => splashScreen.setProgresAttributes("Get authentication info: ", progressCounter));
                }
            }
        }
    }
}
