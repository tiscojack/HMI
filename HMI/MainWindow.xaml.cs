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
            xDoc.Load(@"resources/alberoFREMM_GP_ASW_Completo.xml");

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
                    string[] roba;
                    roba = new string[2] {"", ""};
                    roba[0] = (string)xNode.Attributes["status"].Value;
                    roba[1] = (string)xNode.Attributes["SBC"].Value;
                    nuovoNodo.Tag = roba;
                    
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
                        addToDocPanel(item.Header, (string[])item.Tag);
                    }
                    else
                    {
                        for (int i = 0; i < item.Items.Count; i++)
                        {
                            TreeViewItem tvItem = (TreeViewItem)item.Items[i];
                            addToDocPanel(tvItem.Header, (string[])tvItem.Tag);
                        }
                    }
                }
            }
        }

        private void addToDocPanel(object header, string[] tag)
        {
            Button mybutton = new Button();
            mybutton.Content = header;
            mybutton.Margin = new Thickness(10, 20, 10, 20);
            mybutton.MinHeight = 30;
            if ((string)tag[0] == "1")
            {
                mybutton.Background = Brushes.Green;
            } else if ((string)tag[0] == "0")
            {
                mybutton.Background = Brushes.Red;
            } else
            {
                mybutton.Background = Brushes.White;
            }
            ToolTip tooltip = new ToolTip();
            tooltip.Content = (string)tag[1];
            mybutton.ToolTip = tooltip;
            Wrap.Children.Add(mybutton);
        }
    }
}
