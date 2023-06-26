using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using Microsoft.Xaml.Behaviors;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Path = System.IO.Path;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;

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
    public enum status1
    {
        FAILURE,
        SHUTDOWN,
        MAINTENANCE,
        NOT_OPERATIVE,
        OPERATIVE
    }

    public class DataEntry
    {
        private bool status;
        private double unixtimestamp;
        private status1 status1;

        public DataEntry(double unixtimestamp, bool status, status1 status1)
        {
            this.status = status;
            this.unixtimestamp = unixtimestamp;
            this.status1 = status1;
        }

        public bool get_status() { return this.status; }
        public double get_unixtimestamp() { return this.unixtimestamp; }
        public status1 get_status1() { return this.status1; }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool demo = false;

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
            xDoc.Load(@"resources\albero_configurazione.xml");

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

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            LanguageButton.Content = FindResource(LanguageButton.Content == FindResource("ita") ? "uk" : "ita");

        }

        private void Timer_Tick(object sender, EventArgs e)
        {

            Dictionary<string, List<DataEntry>> csvData = new Dictionary<string, List<DataEntry>>();
            Import_CSV("C:\\Users\\S_GT011\\Documents\\OAMD\\prova.csv", out csvData);

            /*
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
            */

            try
            {
                foreach (ToggleButton mybutton in Wrap.Children)
                {
                    bool status = csvData[mybutton.ToolTip.ToString().Substring(33)].Last().get_status();

                    /*
                    IEnumerable<XElement> matches = xDoc.Root
                          .Descendants("child")
                          .Where(el => (string)el.Attribute("sys") == (string)mybutton.Content);

                    if (matches.Any() == false) return;
                    string status = (string)matches.First().Attribute("status").Value;
                    */



                    if (status == true)
                    {
                        mybutton.Background = Brushes.Green;
                    }
                    else if (status == false)
                    {
                        mybutton.Background = Brushes.Red;
                    }
                    else
                    {
                        mybutton.Background = Brushes.White;
                    }

                }
            }
            catch { return; };


        }


        private void Demo_Click(object sender, RoutedEventArgs e)
        {
            demo = !demo;

        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Dictionary<string, List<DataEntry>> csvData = new Dictionary<string, List<DataEntry>>();
                Import_CSV("C:\\Users\\S_GT011\\Documents\\OAMD\\prova.csv", out csvData);
                foreach (ToggleButton mybutton in Wrap.Children)
                {
                    if ((bool)mybutton.IsChecked)
                    {                       
                        List<DataEntry> samples = csvData[mybutton.ToolTip.ToString().Substring(33)];
                        List<DataEntry> up = new();
                        List<DataEntry> down = new();
                        for (int i = 0; i < samples.Count; i++)
                        {
                            if (samples[i].get_status())
                            {
                                up.Add(samples[i]);
                                down.Add(null);
                                if (i < (samples.Count - 1) && samples[i].get_status() != samples[i + 1].get_status()) { up.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), true, samples[i + 1].get_status1())); down.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), true, samples[i + 1].get_status1())); };
                            }
                            else
                            {
                                down.Add(samples[i]);
                                up.Add(null);
                                if (i < (samples.Count - 1) && samples[i].get_status() != samples[i + 1].get_status()) { down.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), false, samples[i + 1].get_status1())); up.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), false, samples[i + 1].get_status1())); };
                            };

                        }

                        CartesianChart grafico = new()
                        {
                            Width = 400,
                            MaxHeight = 200,
                            TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden,
                            Series = new[]
                            {
                                new StepLineSeries<DataEntry>()
                                {
                                    Values = up,
                                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 3 },
                                    GeometrySize = 0,
                                    Mapping = (sample, chartPoint) =>
                                    {
                                        chartPoint.PrimaryValue = sample.get_status() ? 1 : 0;
                                        chartPoint.SecondaryValue = sample.get_unixtimestamp() - samples[0].get_unixtimestamp();
                                    }
                                },
                                new StepLineSeries<DataEntry>()
                                {
                                    Values = down,
                                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
                                    GeometrySize = 0,
                                    Mapping = (sample, chartPoint) =>
                                    {
                                        chartPoint.PrimaryValue = sample.get_status() ? 1 : 0;
                                        chartPoint.SecondaryValue = sample.get_unixtimestamp() - samples[0].get_unixtimestamp();
                                    }

                                }
                            },
                            XAxes = new List<Axis> { new Axis { Labeler = (value) => $"{value}", MinStep=5, ForceStepToMin=true, MinLimit= 0, MaxLimit=samples.Last().get_unixtimestamp() - samples.First().get_unixtimestamp() + 2.5},  },
                            YAxes = new List<Axis> { new Axis { Labels = new string[] { "DOWN", "UP" } } }
                        };
                        
                        DocPanel.Children.Add(grafico);
                    }
                }
                Wrap.Children.Clear();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }


        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        private void Import_CSV(string filePath, out Dictionary<string, List<DataEntry>> csvData)
        {
            try
            {
                if (String.IsNullOrEmpty(filePath))
                    throw new Exception(String.Format("No file selected"));
                if (!File.Exists(filePath) || (Path.GetExtension(filePath) != ".csv"))
                {
                    MessageBox.Show(String.Format("The selected file({0}) doesn't exist", filePath), "R+G Management", MessageBoxButton.OK, MessageBoxImage.Error);
                    csvData = null;
                    return;
                }
                csvData = new Dictionary<string, List<DataEntry>>();
                StreamReader sR = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string allFile = sR.ReadToEnd();
                sR.Close();
                var lines = allFile.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                string line;
                int i = 0;
                while (i < lines.Length)
                {
                    line = lines[i++];
                    var splittedLine = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    status1 status1;
                    switch (splittedLine[3])
                    {
                        case "OPERATIVE":
                            status1 = (status1)4;
                            break;
                        case "NOT_OPERATIVE":
                            status1 = (status1)3;
                            break;
                        case "MAINTENANCE":
                            status1 = (status1)2;
                            break;
                        case "SHUTDOWN":
                            status1 = (status1)1;
                            break;
                        case "FAILURE":
                            status1 = (status1)0;
                            break;
                        default:
                            status1 = (status1)1;
                            break;
                    }
                    DataEntry data = new DataEntry(Double.Parse(splittedLine[1]), Convert.ToBoolean(int.Parse(splittedLine[2])), status1);
                    if (!csvData.ContainsKey(splittedLine[0]))
                    {
                        List<DataEntry> list = new List<DataEntry> { data };
                        csvData.Add(splittedLine[0], list);
                    }
                    else
                    {
                        csvData[splittedLine[0]].Add(data);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("There are some issue with the csv" + ex.Message);
            }
        }


        //This function is called recursively until all nodes are loaded
        private void AddTreeNode(XmlNode xmlNode, TreeViewItem treeNode)
        {
            XmlNode xNode;
            TreeViewItem tNode;
            XmlNodeList xNodeList;
            //var csv = new StringBuilder();
            if (xmlNode.HasChildNodes) //The current node has children
            {
                xNodeList = xmlNode.ChildNodes;

                for (int x = 0; x <= xNodeList.Count - 1; x++)
                //Loop through the child nodes
                {
                    xNode = xmlNode.ChildNodes[x];

                    TreeViewItem nuovoNodo = new TreeViewItem();
                    var sys = xNode.Attributes["sys"].Value.ToString();
                    var sbc = xNode.Attributes["SBC"].Value.ToString();

                    /*
                    var status = xNode.Attributes["status"].Value.ToString();
                    var status1 = xNode.Attributes["status1"].Value.ToString();
                    for (int i = 0; i <= 10; i++)
                    {
                        var timestamp = 1687170643 + i * 5;
                        var newLine = $"{sbc},{timestamp},{status},{status1}";
                        csv.AppendLine(newLine);
                    }
                    */


                    nuovoNodo.Header = sys;
                    nuovoNodo.Tag = sbc;

                    treeNode.Items.Add(nuovoNodo);

                    tNode = treeNode.Items[x] as TreeViewItem;
                    AddTreeNode(xNode, tNode);
                }
            }
            //File.AppendAllText("C:\\Users\\S_GT011\\Documents\\OAMD/prova.csv", csv.ToString());
        }

        private void Tv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            TreeView treeView;
            TreeViewItem item;
            if (DocPanel.Children.Count >= 2)
            {
                DocPanel.Children.RemoveRange(1,DocPanel.Children.Count);
            }
            if (sender != null)
            {
                Wrap.Children.Clear();
                treeView = (TreeView)sender;

                item = (TreeViewItem)(treeView.SelectedItem);
                if (item != null)
                {
                    if (item.Items.Count == 0)
                    {
                        AddToWrapPanel(item.Header, item.Tag);
                    }
                    else
                    {
                        for (int i = 0; i < item.Items.Count; i++)
                        {
                            TreeViewItem tvItem = (TreeViewItem)item.Items[i];
                            AddToWrapPanel(tvItem.Header, tvItem.Tag);
                        }
                    }
                }
            }
        }

        private void AddToWrapPanel(object header, object tag)
        {
            ToggleButton mybutton = new()
            {
                Content = header,
                Margin = new Thickness(10, 20, 10, 20),
                MinHeight = 30,
            };

            Dictionary<string, List<DataEntry>> csvData = new Dictionary<string, List<DataEntry>>();
            Import_CSV("C:\\Users\\S_GT011\\Documents\\OAMD\\prova.csv", out csvData);
            bool status = csvData[(string)tag].Last().get_status();
            /*
            XDocument xDoc = XDocument.Load("C:\\Users\\S_GT011\\Documents\\OAMD/alberoFREMM_GP_ASW_Completo.xml");

            IEnumerable<XElement> matches = xDoc.Root
                      .Descendants("child")
                      .Where(el => (string)el.Attribute("SBC") == (string)tag);

            string status = (string)matches.First().Attribute("status").Value;
            */

            if (status == true)
            {
                mybutton.Background = Brushes.Green;
            }
            else if (status == false)
            {
                mybutton.Background = Brushes.Red;
            }
            else
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
