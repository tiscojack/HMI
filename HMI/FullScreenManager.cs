using Prova;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace HMI
{
    //Settare anche WindowsStyle a None 
    internal class FullScreenManager
    {
        public Task MaximizeWindow(Window window)
        {
            return Task.Factory.StartNew(() =>
            {
                window.Dispatcher.Invoke((Action)(() =>
                {
                    Thread.Sleep(100);
                    window.WindowState = System.Windows.WindowState.Maximized;
                }));
            });
        }

        void Window4_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
        }

        public void PreventClose(MainWindow OamdWin)
        {
            OamdWin.Closing += new System.ComponentModel.CancelEventHandler(Window4_Closing);
        }
    }
}
