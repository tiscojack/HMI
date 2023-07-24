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
        // Simple attribute that represents the TabItem header object
        private readonly CloseableHeader header;
        // Simple attribute that represents the TabItem header content
        public string Title
        {
            set
            {
                header.label_TabTitle.Content = value;
                header.edittab.Text = value;
            }
        }
        public CloseableTab()   
        {
            var closeableTabHeader = new CloseableHeader();
            this.Header = closeableTabHeader;
            header = (CloseableHeader)this.Header;
            // Handles the click of the close button
            closeableTabHeader.button_close.Click += (s, e) =>
            {
                ((TabControl)this.Parent).Items.Remove(this);
            };
            // When the header is double-clicked, allows editing of the textbox
            closeableTabHeader.MouseDoubleClick += (s, e) =>
            {
                header.edittab.Visibility = Visibility.Visible;
                header.label_TabTitle.Visibility = Visibility.Hidden;
                header.edittab.Focus();
                header.edittab.SelectAll();
            };
            // When the user finishes editing the textbox, saves the change into the label 
            header.edittab.LostFocus += (s, e) =>
            {
                header.label_TabTitle.Content = header.edittab.Text;
                header.edittab.Visibility = Visibility.Hidden;
                header.label_TabTitle.Visibility = Visibility.Visible;
            };
            // When the user finishes editing the textbox, saves the change into the label 
            header.edittab.KeyDown += (s, e) =>
            {
                if (e.Key != System.Windows.Input.Key.Enter) { return; }
                header.label_TabTitle.Content = header.edittab.Text;
                header.edittab.Visibility = Visibility.Hidden;
                header.label_TabTitle.Visibility = Visibility.Visible;
            };
        }
    }
}
