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

namespace Prova
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();


            //First, we'll load the Xml document
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(@"resources/albero_configurazione.xml");

            //Now, clear out the treeview, 
            dirTree.Items.Clear();

            //and add the first (root) node
            TreeViewItem treeviewItemRoot = new TreeViewItem();
            treeviewItemRoot.Header = "FREMM";
            dirTree.Items.Add(treeviewItemRoot);

            TreeViewItem tNode = new TreeViewItem();
            tNode = (TreeViewItem)dirTree.Items[0];

            //We make a call to addTreeNode, 
            //where we'll add all of our nodes
            addTreeNode(xDoc.DocumentElement, tNode);




        }

        //This function is called recursively until all nodes are loaded
        private void addTreeNode(XmlNode xmlNode, TreeViewItem treeNode)
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
                    addTreeNode(xNode, tNode);
                }

            }
            //else //No children, so add the outer xml (trimming off whitespace)
            //   treeNode.Header = xmlNode.OuterXml.Trim();

        }

        private void tv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TreeView treeView;
            TreeViewItem item;
            if (sender != null)
            {
                Wrap.Children.Clear();
                treeView = (TreeView)(sender);

                item = (TreeViewItem)(treeView.SelectedItem);
                if (item != null)
                {
                    if (item.Items.Count == 0)
                    {
                        addToDocPanel(item.Header, item.Tag);
                    }
                    else
                    {
                        for (int i = 0; i < item.Items.Count; i++)
                        {
                            TreeViewItem tvItem = (TreeViewItem)item.Items[i];
                            addToDocPanel(tvItem.Header, tvItem.Tag);
                        }
                    }
                }
            }
        }

        private void addToDocPanel(object header, object tag)
        {
            Button mybutton = new Button();
            mybutton.Content = header;
            mybutton.Margin = new Thickness(10, 20, 10, 20);
            mybutton.MinHeight = 30;

            XmlDocument xDoc = new XmlDocument();
           // xDoc.Load(@"resources/alberoFREMM_GP_ASW_Completo.xml");

            XElement root = XElement.Load(@"resources/alberoFREMM_GP_ASW_Completo.xml");
            IEnumerable<XElement> address =
                from el in root.Elements("child")
                where (string)el.Attribute("SBC") == (string)tag
                select el;
            foreach (XElement el in address)
                Console.WriteLine(el);

            /*if (placeholder == "1")
            {
                mybutton.Background = Brushes.Green;
            } else if (placeholder == "0")
            {
                mybutton.Background = Brushes.Red;
            } else
            {
                mybutton.Background = Brushes.White;
            }*/
            ToolTip tooltip = new ToolTip();
            tooltip.Content = (string)tag;
            mybutton.ToolTip = tooltip;
            Wrap.Children.Add(mybutton);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dirTree.Items.Count > 0)
            {
                for (int i = 0; i < dirTree.Items.Count; i++)
                {
                    expandTreeItem((TreeViewItem)dirTree.Items[i]);
                }
            }
        }

        private void expandTreeItem(TreeViewItem item)
        {
            string text;

            
            item.Foreground = Brushes.Black;
            item.SetValue(TreeViewItem.IsExpandedProperty, true);
            text = txtSearch.Text.ToLower();
            string sItem = (string)(item.Header);
            if (!text.Equals(string.Empty) && sItem.ToLower().Contains(text))
            {
                item.Foreground = Brushes.Red;
            }
            for (int i = 0; i < item.Items.Count; i++)
            {
                expandTreeItem((TreeViewItem)item.Items[i]);
            }
        }
    }
}
