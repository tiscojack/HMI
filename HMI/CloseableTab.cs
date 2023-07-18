using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HMI
{
    class CloseableTab: TabItem
    {
        public CloseableTab()   
        {
            var closeableTabHeader = new CloseableHeader();
            this.Header = closeableTabHeader;
            closeableTabHeader.button_close.Click +=    
                new RoutedEventHandler(button_close_Click);
            closeableTabHeader.MouseDoubleClick += CloseableTabHeader_MouseDoubleClick1;
            ((CloseableHeader)this.Header).edittab.LostFocus += Edittab_LostFocus;
        }

        private void Edittab_LostFocus(object sender, RoutedEventArgs e)
        {
            ((CloseableHeader)this.Header).label_TabTitle.Content = ((CloseableHeader)this.Header).edittab.Text;
            ((CloseableHeader)this.Header).edittab.Visibility = Visibility.Hidden;
            ((CloseableHeader)this.Header).label_TabTitle.Visibility = Visibility.Visible;
        }

        private void CloseableTabHeader_MouseDoubleClick1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ((CloseableHeader)this.Header).edittab.Visibility = Visibility.Visible;
            ((CloseableHeader)this.Header).label_TabTitle.Visibility = Visibility.Hidden;

        }


        public string Title
        {
            set
            {
                ((CloseableHeader)this.Header).label_TabTitle.Content = value;
                ((CloseableHeader)this.Header).edittab.Text = value;
            }
        }

        void button_close_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
        }
    }
}
