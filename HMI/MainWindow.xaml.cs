using HMI;
using LiveChartsCore.Drawing;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WPF;
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

namespace Prova
{
    public enum Status1
    {
        FAILURE,
        DEGRADED,
        MAINTENANCE,
        UNKNOWN,
        OPERATIVE, 
        NOSTATUS
    }

    public partial class MainWindow : Window
    {
        bool demo = false;
        static string RunningPath = Directory.GetParent(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName).FullName;
        string csvPath = string.Format("{0}resources\\FileDemo.csv", Path.GetFullPath(Path.Combine(RunningPath, @"..\..\")));
        string imagePath = "pack://application:,,,/resources/Rina2.bmp";
        List<TreeViewItem> selectedItemList = new();
        int selectedItemIndex = -1; 
        Dictionary<string, List<DataEntry>> csvData = new();

        public MainWindow()
        {

            InitializeComponent();

            imgLogo.Source = CreatebitmapImage(imagePath, 50);

            StartTimer();

            SetupTreeView();
            Import_CSV(csvPath, csvData);
        }
        // Loads the XML and creates the TreeView root
        private void SetupTreeView()
        {
            // Load the XML document
            XmlDocument xDoc = new();
            xDoc.Load(@"resources\albero_configurazione.xml");

            // Clear out the treeview 
            dirTree.Items.Clear();

            // Add the root node
            TreeViewItem treeviewItemRoot = new()
            {
                Header = "FREMM"
            };
            dirTree.Items.Add(treeviewItemRoot);

            // Call to addTreeNode, 
            // Which recursively populates the TreeView
            AddTreeNode(xDoc.DocumentElement, treeviewItemRoot);
        }
        // Setups and starts the timer that handles the async refreshes of the UI 
        private void StartTimer()
        {
            DispatcherTimer timer = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            // Attach the function to the Tick event
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            LanguageButton.Content = FindResource(LanguageButton.Content == FindResource("ita") ? "uk" : "ita");

        }
        // Functions invoked every tick of the timer that refreshes the UI 
        private void Timer_Tick(object sender, EventArgs e)
        {
            Demo();

            try
            {
                foreach (ToggleButton mybutton in Wrap.Children)
                {
                    //un po' hardcoded, bisogna capire come passarlo in maniera intelligente
                    string sbc = mybutton.ToolTip.ToString().Substring(33);
                    if (!csvData.ContainsKey(sbc)) { continue; }
                    Status1 status = csvData[sbc].Last().get_status1();
                    
                    mybutton.Background = status switch
                    {
                        (Status1)0 => Brushes.Red,
                        (Status1)1 => Brushes.Yellow,
                        (Status1)2 => Brushes.Brown,
                        (Status1)3 => Brushes.White,
                        (Status1)4 => Brushes.Green,
                        _ => Brushes.Gray,
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            };
        }

        private void Demo()
        {
            if (demo)
            {
                Random rand = new();
                foreach (var item in csvData)
                {
                    foreach (var value in item.Value)
                    {
                        int _salt = rand.Next();
                        value.set_status(Convert.ToBoolean(_salt % 2));
                        value.set_status1((Status1)(_salt % 5));
                    }
                }
            }
        }

        private void Demo_Click(object sender, RoutedEventArgs e)
        {
            demo = !demo;
        }
        // Event handler that closes the app when invoked
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int tabcounter = 0;
                TabControl tab = DocPanel;
                List<StackPanel> panel = new();
                List<ScrollViewer> sv = new();
                
                List<CloseableTab> ti = new();
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
                        double maxVal = samples.Last().get_unixtimestamp() - samples.First().get_unixtimestamp();
                        var step = maxVal switch
                        {
                            <= 10800 => 180,
                            <= 21600 and > 10800 => 300,
                            <= 43200 and > 21600 => 600,
                            <= 86400 and > 43200 => 1800,
                            <= 259200 and > 86400 => 3600,
                            <= 604800 and > 259200 => 7200,
                            <= 1296000 and > 604800 => 14400,
                            <= 2592000 and > 1296000 => 28800,
                            <= 3888000 and > 2592000 => 43200,
                            _ => (double)43200,
                        };

                        CartesianChart grafico = new()
                        {
                            Width = 4000,
                            Height = 200,
                            ZoomMode = ZoomAndPanMode.X,
                            HorizontalAlignment = HorizontalAlignment.Left,
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
                            XAxes = new List<Axis> { new Axis { Labeler = (value) => $"{value / 60}m", TextSize = 10, MinStep = step, ForceStepToMin = true, MinLimit = 0, MaxLimit = maxVal + step / 2 }, },
                            YAxes = new List<Axis> { new Axis { TextSize = 10, MinLimit = 0, MaxLimit = 1, Labels = new string[] { "DOWN", "UP" } } }
                        };
                        
                        // Adds a new tab every 10 graphs 
                        if (tabcounter % 10 == 0) 
                        { 
                            panel.Add(new StackPanel() { Orientation = Orientation.Vertical });
                            sv.Add(new ScrollViewer() { VerticalScrollBarVisibility = 
                                                            ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto });
                        }
                        
                        panel[tabcounter / 10].Children.Add(new ToggleButton()     
                                                                    {   Content = mybutton.ToolTip.ToString().Substring(33),     
                                                                        Margin = new Thickness(10, 0, 0, 0), 
                                                                        FontSize = 15,
                                                                        Width = 100,
                                                                        Height = 50,
                                                                        HorizontalAlignment = HorizontalAlignment.Left,
                                                                    });

                        this.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(ChartMouseWheelEvent));
                        
                        panel[tabcounter / 10].Children.Add(grafico);
                        tabcounter++;
                    }
                }
                for (int i = 0; i <= (tabcounter-1) / 10; i++) {
                    ti.Add(new CloseableTab());
                    ti[i].Content = sv[i];
                    sv[i].Content = panel[i];

                    ti[i].Title = String.Format("Preview Tab {0}", i+1);
                    tab.Items.Insert(i+1, ti[i]);
                }
                // The chart gets updated live (when we zoom/pan) so if the demo is set to true, it looks buggy 
                demo = false;
                // Sets the selected tab to the first of the newly inserted ones
                Dispatcher.BeginInvoke((Action)(() => tab.SelectedIndex = 1));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int tabcounter = 0;
                TabControl tab = DocPanel;
                List<StackPanel> panel = new();
                List<ScrollViewer> sv = new();

                List<CloseableTab> ti = new();
                foreach (ToggleButton mybutton in Wrap.Children)
                {
                    if ((bool)mybutton.IsChecked)
                    {
                        List<DataEntry> samples = csvData[mybutton.ToolTip.ToString().Substring(33)];
                        List<DataEntry> green = new();
                        List<DataEntry> red = new();
                        for (int i = 0; i < samples.Count; i++)
                        {
                            if (samples[i].get_status())
                            {
                                green.Add(samples[i]);
                                red.Add(null);
                                if (i < (samples.Count - 1) && samples[i].get_status() != samples[i + 1].get_status()) { green.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), true, samples[i + 1].get_status1())); red.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), true, samples[i + 1].get_status1())); };
                            }
                            else
                            {
                                red.Add(samples[i]);
                                green.Add(null);
                                if (i < (samples.Count - 1) && samples[i].get_status() != samples[i + 1].get_status()) { red.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), false, samples[i + 1].get_status1())); green.Add(new DataEntry(samples[i + 1].get_unixtimestamp(), false, samples[i + 1].get_status1())); };
                            };
                        }
                        double maxVal = samples.Last().get_unixtimestamp() - samples.First().get_unixtimestamp();

                        CartesianChart grafico = new()
                        {
                            Width = 60000,
                            Height = 400,
                            //ZoomMode = ZoomAndPanMode.X,
                            
                            HorizontalAlignment = HorizontalAlignment.Left,
                            Series = new[]
                            {
                                new StepLineSeries<DataEntry>()
                                {
                                    Values = green,
                                    Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 0 },
                                    Fill = new SolidColorPaint(SKColors.Green),
                                    GeometrySize = 0,
                                    Mapping = (sample, chartPoint) =>
                                    {
                                        chartPoint.PrimaryValue = sample.get_status1() switch
                                        {
                                            (Status1)0 => 1,
                                            (Status1)1 => 2,
                                            (Status1)2 => 3,
                                            (Status1)3 => 4,
                                            (Status1)4 => 5,
                                            _ => 6,
                                        };
                                        chartPoint.SecondaryValue = sample.get_unixtimestamp() - samples[0].get_unixtimestamp();
                                    }
                                },
                                new StepLineSeries<DataEntry>()
                                {
                                    Values = red,
                                    Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 0 },
                                    Fill = new SolidColorPaint(SKColors.Red),
                                    GeometrySize = 0,
                                    Mapping = (sample, chartPoint) =>
                                    {
                                        chartPoint.PrimaryValue = sample.get_status1() switch
                                        {
                                            (Status1)0 => 1,
                                            (Status1)1 => 2,
                                            (Status1)2 => 3,
                                            (Status1)3 => 4,
                                            (Status1)4 => 5,
                                            _ => 6,
                                        };
                                        chartPoint.SecondaryValue = sample.get_unixtimestamp() - samples[0].get_unixtimestamp();
                                    }
                                }
                            },
                            XAxes = new List<Axis> { new Axis { Labeler = (value) => $"{value}", TextSize = 10, MinLimit = 0, MaxLimit = maxVal + 50}, },
                            YAxes = new List<Axis> { new Axis { TextSize = 10, MinLimit = 0, MaxLimit = 6, Labels = new string[] {"", "FAILURE", "DEGRADED", "MAINTENANCE", "UNKNOWN", "OPERATIVE" }, }, }
                        };

                        // Adds a new tab every 10 graphs 
                        if (tabcounter % 10 == 0)
                        {
                            panel.Add(new StackPanel() { Orientation = Orientation.Vertical });
                            sv.Add(new ScrollViewer()
                            {
                                VerticalScrollBarVisibility =
                                                            ScrollBarVisibility.Auto,
                                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
                            });
                        }

                        panel[tabcounter / 10].Children.Add(new ToggleButton()
                        {
                            Content = mybutton.ToolTip.ToString().Substring(33),
                            Margin = new Thickness(10, 0, 0, 0),
                            FontSize = 15,
                            Width = 100,
                            Height = 50,
                            HorizontalAlignment = HorizontalAlignment.Left,
                        });
                        panel[tabcounter / 10].Children.Add(grafico);
                        tabcounter++;
                    }
                }
                for (int i = 0; i <= (tabcounter - 1) / 10; i++)
                {
                    ti.Add(new CloseableTab());
                    ti[i].Content = sv[i];
                    sv[i].Content = panel[i];

                    ti[i].Title = String.Format("Preview Tab {0}", i + 1);
                    tab.Items.Insert(i + 1, ti[i]);
                }
                // The chart gets updated live (when we zoom/pan) so if the demo is set to true, it looks buggy 
                demo = false;
                // Sets the selected tab to the first of the newly inserted ones
                Dispatcher.BeginInvoke((Action)(() => tab.SelectedIndex = 1));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }
        // Prevents the event from bubbling up to the scrollviewer, effectively disabling the mousewheel scroll on it
        private void ChartMouseWheelEvent(object sender, MouseWheelEventArgs e)
        {
            var sv = DocPanel.SelectedContent as ScrollViewer;
            var panel = sv.Content as StackPanel;
            bool zoom = true;
            int counter = 0;
            foreach (var child in panel.Children)
            {
                // loop dispari
                if (child.GetType().ToString() == "System.Windows.Controls.Primitives.ToggleButton")
                {
                    var btn = child as ToggleButton;
                    zoom = (bool)btn.IsChecked;
                }  else if (zoom) //loop dispari (condizionale)
                {   
                    var graph = child as CartesianChart;
                    var core = graph.CoreChart as CartesianChart<SkiaSharpDrawingContext>;
                    Point position = e.GetPosition(this);
                    core.Zoom(new LvcPoint((float)position.X, (float)position.Y), (e.Delta <= 0) ? ZoomDirection.ZoomOut : ZoomDirection.ZoomIn);
                }
                counter++;
            }
        }

        // Utils
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
        // Opens the .csv file and reads its data into a dictionary 
        private void Import_CSV(string filePath, Dictionary<string, List<DataEntry>> csvData)
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
                
                StreamReader sR = new(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                string allFile = sR.ReadToEnd();
                sR.Close();
                var lines = allFile.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                string line;
                int i = 0;
                while (i < lines.Length)
                {
                    line = lines[i++];
                    var splittedLine = line.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    Status1 status1 = splittedLine[3] switch
                    {
                        "FAILURE" => (Status1)0,
                        "DEGRADED" => (Status1)1,
                        "MAINTENANCE" => (Status1)2,
                        "UNKNOWN" => (Status1)3,
                        "OPERATIVE" => (Status1)4,
                        _ => (Status1)5,
                    };
                    DataEntry data = new(Double.Parse(splittedLine[1]), Convert.ToBoolean(int.Parse(splittedLine[2])), status1);
                    if (!csvData.ContainsKey(splittedLine[0]))
                    {
                        List<DataEntry> list = new() { data };
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
                throw new Exception("There are some issues with the csv" + ex.Message);
            }
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
                    var sys = xNode.Attributes["sys"].Value.ToString();
                    var sbc = xNode.Attributes["SBC"].Value.ToString();

                    nuovoNodo.Header = sys;
                    nuovoNodo.Tag = sbc;

                    treeNode.Items.Add(nuovoNodo);

                    tNode = treeNode.Items[x] as TreeViewItem;
                    AddTreeNode(xNode, tNode);
                }
            }
        }

        private void Tv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SelectTreeViewItem(sender);
        }

        private void SelectTreeViewItem(object send)
        {
            TreeView treeView;
            TreeViewItem item;
            if (send != null)
            {
                Wrap.Children.Clear();
                treeView = (TreeView)send;

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
                    Dispatcher.BeginInvoke((Action)(() => DocPanel.SelectedItem = mainview));
                }
            }
        }

        private void AddToWrapPanel(object header, object tag)
        {
            Status1 status = Status1.NOSTATUS ;
            ToggleButton mybutton = new()
            {
                Margin = new Thickness(10, 20, 10, 20),
                MinHeight = 30,
                MaxHeight = 100,
                MinWidth = 100,
                MaxWidth = 300 
            };
            
            mybutton.Content = new TextBlock()
            {
                Name = "togglebuttonTextBlock",
                Text = header.ToString()?.Replace("_", " "),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            if (csvData.ContainsKey((string)tag))
            {
                status = csvData[(string)tag].Last().get_status1();
            }

            mybutton.Background = status switch
            {
                (Status1)0 => Brushes.Red,
                (Status1)1 => Brushes.Yellow,
                (Status1)2 => Brushes.Brown,
                (Status1)3 => Brushes.White,
                (Status1)4 => Brushes.Green,
                _ => Brushes.Gray,
            };

            ToolTip tooltip = new()
            {
                Content = (string)tag
            };

            mybutton.ToolTip = tooltip;
            mybutton.AddHandler(ToggleButton.MouseDoubleClickEvent, new RoutedEventHandler(DoubleClick));
            Wrap.Children.Add(mybutton);
        }
        private void DoubleClick(object sender, RoutedEventArgs e)
        {
            ToggleButton button = (ToggleButton)sender;

            for (int i = 0; i < dirTree.Items.Count; i++)
            {
                TextBlock tb = button.Content as TextBlock;
                LookForTvItem((TreeViewItem)dirTree.Items[i], tb.Text.Replace(" ", "_"));
            }
            SelectTreeViewItem(dirTree);
        }

        private void LookForTvItem(TreeViewItem item, string text)
        {
            string sItem = (string)item.Header;
            if (!text.Equals(string.Empty) && sItem.Equals(text))
            {
                item.IsSelected = true;
                TreeViewItem tmpItem = item.Parent as TreeViewItem;
                while (tmpItem != null)
                {
                    tmpItem.Parent.SetValue(TreeViewItem.IsExpandedProperty, true);
                    item.Parent.SetValue(TreeViewItem.IsExpandedProperty, true);
                    tmpItem = tmpItem.Parent as TreeViewItem;
                }
            }
            for (int i = 0; i < item.Items.Count; i++)
            {
                LookForTvItem((TreeViewItem)item.Items[i], text);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dirTree.Items.Count > 0)
            {
                selectedItemList.Clear();
                selectedItemIndex = -1;
                for (int i = 0; i < dirTree.Items.Count; i++)
                {
                    ExpandTreeItem((TreeViewItem)dirTree.Items[i]);
                }
            }
        }

        private void ExpandTreeItem(TreeViewItem item)
        {
            string text;
            string sItem = (string)item.Header;
            item.Foreground = Brushes.Black;
            text = txtSearch.Text.ToLower();
            if (!text.Equals(string.Empty) && sItem.ToLower().Contains(text))
            {
                TreeViewItem tmpItem = item.Parent as TreeViewItem;
                while (tmpItem != null)
                {
                    tmpItem.Parent.SetValue(TreeViewItem.IsExpandedProperty, true);
                    item.Parent.SetValue(TreeViewItem.IsExpandedProperty, true);
                    tmpItem = tmpItem.Parent as TreeViewItem;
                }
                item.Foreground = Brushes.White;
                selectedItemList.Add(item);
            }
            for (int i = 0; i < item.Items.Count; i++)
            {
                ((TreeViewItem)item.Items[i]).SetValue(TreeViewItem.IsExpandedProperty, false);
                ExpandTreeItem((TreeViewItem)item.Items[i]);
            }
        }

        public static BitmapImage CreatebitmapImage(string P, int h)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(P);
            bitmapImage.DecodePixelHeight = h;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        private void SearchresultsbuttonDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedItems = selectedItemList.Count;
            if (selectedItemList.Count > 0)
            {
                selectedItemIndex++;
                if (selectedItemIndex == selectedItems)
                {
                    selectedItemIndex = 0;
                }
                selectedItemList[selectedItemIndex].IsSelected = true;
                SelectTreeViewItem((object)dirTree);
            }
        }

        private void SearchresultsbuttonUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedItems = selectedItemList.Count;
            if (selectedItemList.Count > 0)
            {
                selectedItemIndex--;
                if (selectedItemIndex <= -1)
                {
                    selectedItemIndex = selectedItems - 1;
                }
                selectedItemList[selectedItemIndex].IsSelected = true;
                SelectTreeViewItem((object)dirTree);
            }
        }
    }
}