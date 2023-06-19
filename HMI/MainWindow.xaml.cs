using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;
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
using System.Xml;
using static System.Net.Mime.MediaTypeNames;
using System.Xml.Linq;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;

namespace Prova
{
    public class DropDownButtonBehavior : Behavior<Button>
    {
        private bool isContextMenuOpen;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(Button.ClickEvent, new RoutedEventHandler(AssociatedObject_Click), true);
        }

        void AssociatedObject_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Button source = sender as Button;
            if (source != null && source.ContextMenu != null)
            {
                if (!isContextMenuOpen)
                {
                    // Add handler to detect when the ContextMenu closes
                    source.ContextMenu.AddHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed), true);
                    // If there is a drop-down assigned to this button, then position and display it 
                    source.ContextMenu.PlacementTarget = source;
                    source.ContextMenu.Placement = PlacementMode.Bottom;
                    source.ContextMenu.IsOpen = true;
                    isContextMenuOpen = true;
                }
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(Button.ClickEvent, new RoutedEventHandler(AssociatedObject_Click));
        }

        void ContextMenu_Closed(object sender, RoutedEventArgs e)
        {
            isContextMenuOpen = false;
            var contextMenu = sender as ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool demo = false;
        readonly string[] status =
        {
            "OPERATIVE",
            "NOT_OPERATIVE",
            "MAINTENANCE",
            "SHUTDOWN",
            "FAILURE"};

        public MainWindow()
        {

            InitializeComponent();

            imgLogo.Source = createbitmapImage(@"C:\Users\S_GT011\source\repos\HMIfinal\HMI\resources\Rina2.bmp", 50);

            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += Timer_Tick;
            timer.Start();

            //First, we'll load the Xml document
            XmlDocument xDoc = new();
            xDoc.Load(@"resources/albero_configurazione.xml");

            //Now, clear out the treeview, 
            dirTree.Items.Clear();

            //and add the first (root) node
            TreeViewItem treeviewItemRoot = new()
            {
                Header = "FREMM"
            };
            dirTree.Items.Add(treeviewItemRoot);

            TreeViewItem tNode = new();
            tNode = (TreeViewItem)dirTree.Items[0];

            //We make a call to addTreeNode, 
            //where we'll add all of our nodes
            AddTreeNode(xDoc.DocumentElement, tNode);

        }

        

        void Timer_Tick(object sender, EventArgs e)
        {
            XDocument xDoc = XDocument.Load("C:\\Users\\S_GT011\\Documents\\OAMD/alberoFREMM_GP_ASW_Completo.xml");

            if (demo)
            {
                IEnumerable<XElement> matches = xDoc.Root
                      .Descendants("child");
                foreach (XElement el in matches)
                {
                    Random rand = new Random();
                    int _salt = rand.Next();
                    el.Attribute("status").Value = (_salt % 2).ToString();
                    el.Attribute("status1").Value = (string)status[_salt % 5];
                }
                xDoc.Save("C:\\Users\\S_GT011\\Documents\\OAMD/alberoFREMM_GP_ASW_Completo.xml");
            }

            foreach (Button mybutton in Wrap.Children){
                IEnumerable<XElement> matches = xDoc.Root
                      .Descendants("child")
                      .Where(el => (string)el.Attribute("sys") == (string)mybutton.Content);

                if (matches.Any() == false) return;
                string status = (string)matches.First().Attribute("status").Value;

                if (status == "1")
                {
                    mybutton.Background = Brushes.Green;
                }
                else if (status == "0")
                {
                    mybutton.Background = Brushes.Red;
                }
                else
                {
                    mybutton.Background = Brushes.White;
                }

            }
        }


        private void Demo_Click(object sender, RoutedEventArgs e)
        {
            demo = !demo;

        }
        //This function is called recursively until all nodes are loaded
        private void AddTreeNode(XmlNode xmlNode, TreeViewItem treeNode)
        {
            XmlNode xNode;
            TreeViewItem tNode;
            XmlNodeList xNodeList;
            if (xmlNode.HasChildNodes) //The current node has children
            {
                xNodeList = xmlNode.ChildNodes;
                for (int x = 0; x <= xNodeList.Count - 1; x++)
                //Loop through the child nodes
                {
                    xNode = xmlNode.ChildNodes[x];

                    TreeViewItem nuovoNodo = new TreeViewItem();
                    nuovoNodo.Header = xNode.Attributes["sys"].Value;
                    nuovoNodo.Tag = (string)xNode.Attributes["SBC"].Value;
                    
                    treeNode.Items.Add(nuovoNodo);

                    tNode = treeNode.Items[x] as TreeViewItem;
                    AddTreeNode(xNode, tNode);
                }
            }
        }

        private void Tv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TreeView treeView;
            TreeViewItem item;
            if (sender != null)
            {
                Wrap.Children.Clear();
                treeView = (TreeView)sender;

                item = (TreeViewItem)(treeView.SelectedItem);
                if (item != null)
                {
                    if (item.Items.Count == 0)
                    {
                        AddToDocPanel(item.Header, item.Tag);
                    }
                    else
                    {
                        for (int i = 0; i < item.Items.Count; i++)
                        {
                            TreeViewItem tvItem = (TreeViewItem)item.Items[i];
                            AddToDocPanel(tvItem.Header, tvItem.Tag);
                        }
                    }
                }
            }
        }

        private void AddToDocPanel(object header, object tag)
        {
            Button mybutton = new()
            {
                Content = header,
                Margin = new Thickness(10, 20, 10, 20),
                MinHeight = 30
            };

            XDocument xDoc = XDocument.Load("C:\\Users\\S_GT011\\Documents\\OAMD/alberoFREMM_GP_ASW_Completo.xml");

            IEnumerable<XElement> matches = xDoc.Root
                      .Descendants("child")
                      .Where(el => (string)el.Attribute("SBC") == (string)tag);

            string status = (string)matches.First().Attribute("status").Value;
          

            if (status == "1")
            {
                mybutton.Background = Brushes.Green;
            } else if (status == "0")
            {
                mybutton.Background = Brushes.Red;
            } else
            {
                mybutton.Background = Brushes.White;
            }
            ToolTip tooltip = new()
            {
                Content = (string)tag
            };
            mybutton.ToolTip = tooltip;
            Wrap.Children.Add(mybutton);
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dirTree.Items.Count > 0)
            {
                for (int i = 0; i < dirTree.Items.Count; i++)
                {
                    ExpandTreeItem((TreeViewItem)dirTree.Items[i]);
                }
            }
        }

        private void ExpandTreeItem(TreeViewItem item)
        {
            string text;          
            item.Foreground = Brushes.Black;
            item.SetValue(TreeViewItem.IsExpandedProperty, true);
            text = txtSearch.Text.ToLower();
            string sItem = (string)item.Header;
            if (!text.Equals(string.Empty) && sItem.ToLower().Contains(text))
            {
                item.Foreground = Brushes.Red;
            }
            for (int i = 0; i < item.Items.Count; i++)
            {
                ExpandTreeItem((TreeViewItem)item.Items[i]);
            }
        }

        public static BitmapImage createbitmapImage(string P, int h)
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(P);
            //bitmapImage.DecodePixelWidth = w;
            bitmapImage.DecodePixelHeight = h;
            bitmapImage.EndInit();

            return bitmapImage;
        }
    }
}
