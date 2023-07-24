using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HMI
{
    class CloseableTab: TabItem
    {
        private CloseableHeader header; 
        
        public CloseableTab()   
        {
            var closeableTabHeader = new CloseableHeader();
            this.Header = closeableTabHeader;
            header = (CloseableHeader)this.Header;
            closeableTabHeader.button_close.Click +=    
                new RoutedEventHandler(button_close_Click);
            closeableTabHeader.MouseDoubleClick += CloseableTabHeader_MouseDoubleClick1;
            header.edittab.LostFocus += Edittab_LostFocus;
            header.edittab.KeyDown += Edittab_KeyDown;
        }

        private void Edittab_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) { return; }
            header.label_TabTitle.Content = header.edittab.Text;
            header.edittab.Visibility = Visibility.Hidden;
            header.label_TabTitle.Visibility = Visibility.Visible;
        }

        private void Edittab_LostFocus(object sender, RoutedEventArgs e)
        {
            header.label_TabTitle.Content = header.edittab.Text;
            header.edittab.Visibility = Visibility.Hidden;
                header.label_TabTitle.Visibility = Visibility.Visible;
        }

        private void CloseableTabHeader_MouseDoubleClick1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            header.edittab.Visibility = Visibility.Visible;
            header.label_TabTitle.Visibility = Visibility.Hidden;
            header.edittab.Focus();
            header.edittab.SelectAll();

        }


        public string Title
        {
            set
            {
                header.label_TabTitle.Content = value;
                header.edittab.Text = value;
            }
        }

        void button_close_Click(object sender, RoutedEventArgs e)
        {
            ((TabControl)this.Parent).Items.Remove(this);
        }
    }
}
