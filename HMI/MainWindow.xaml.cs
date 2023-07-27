using LiveChartsCore.SkiaSharpView;
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
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using LiveChartsCore.Measure;
using File = System.IO.File;
using System.DirectoryServices.AccountManagement;
using System.Resources;

namespace HMI
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
            if (sender is Button source && source.ContextMenu != null)
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
            if (sender is ContextMenu contextMenu)
            {
                contextMenu.RemoveHandler(ContextMenu.ClosedEvent, new RoutedEventHandler(ContextMenu_Closed));
            }
        }
    }

    public class DataEntry
    {
        private bool status;
        private double unixtimestamp;
        private Status1 status1;

        public DataEntry(double unixtimestamp, bool status, Status1 status1)
        {
            this.status = status;
            this.unixtimestamp = unixtimestamp;
            this.status1 = status1;
        }

        public bool get_status() { return this.status; }
        public double get_unixtimestamp() { return this.unixtimestamp; }
        public Status1 get_status1() { return this.status1; }
        public void set_status(int status) { this.status = (status == 0); }
        public void set_status1(Status1 status) { this.status1 = status; }
    }

    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool demo = false, isBntUp = false, isBntDown = false, fileImported = false;
        double vOffset = 0, vBottomValue = 0;
        string[] csvPath;
        //string imagePath = "pack://application:,,,/resources/Rina2.bmp";
        string imagePath = "C:\\Progetti\\OSN\\OAMD\\OAMDHMI\\HMI\\resources\\Rina2.bmp";
        Dictionary<string, List<DataEntry>> csvData;
        List<List<string>> menuItemData;
        FullScreenManager fullMan = new FullScreenManager();
        List<TreeViewItem> selectedItemList = new List<TreeViewItem>();
        int selectedItemIndex = -1;
        DispatcherTimer timerDemo;
        SystemInfoManager infoManager;
        ResourceDictionary dictionary = new ResourceDictionary();
        TreeViewItem treeviewItemRoot;
        DatabaseProperties dbProperties = new DatabaseProperties();
        GetDataFromSql getDataFromSql;
        BackgroundWorker backgroundWorker;
        DispatcherTimer tmrEnsureWorkerGetsCalled, tmrCallBgWorker;
        object lockObject = new object();

        public MainWindow()
        {
            InitializeComponent();
            /*Aggiunta da GPO per impedire minimizzazione*/
            //fullMan.PreventClose(MainWindowOAMD);
            getDataFromSql = new GetDataFromSql(dbProperties.sqlConnection);
            infoManager = new SystemInfoManager(dbProperties.sqlConnection);
            imgLogo.Source = createbitmapImage(imagePath, 50);
            backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            backgroundWorker.DoWork += BackgroundWorkerOnDoWork;
            backgroundWorker.ProgressChanged += BackgroundWorkerOnProgressChanged;
            backgroundWorker.RunWorkerAsync();

            tmrCallBgWorker = new DispatcherTimer();
            tmrCallBgWorker.Tick += new EventHandler(tmrCallBgWorker_tick);
            tmrCallBgWorker.Interval = new TimeSpan(0, 0, 1);
            tmrCallBgWorker.Start();

            //First, we'll load the Xml document
            XmlDocument xDoc = new();
            xDoc.Load(@"resources\albero_configurazione.xml");
            //Now, clear out the treeview, 
            dirTree.Items.Clear();
            //and add the first (root) node
            treeviewItemRoot = new TreeViewItem();
            dirTree.Items.Add(treeviewItemRoot);

            TreeViewItem tNode = new();
            tNode = (TreeViewItem)dirTree.Items[0];
            //We make a call to addTreeNode, 
            //where we'll add all of our nodes
            AddTreeNode(xDoc.DocumentElement, tNode);
            //dictionary.Source = new Uri("..\\StringResources.it.xaml", UriKind.Relative);
            menuItemData = getDataFromSql.getsqlData(Languages.IT, ItemStatus.Active);
            AddContextMenuItem(menuItemData, MenuButton.ContextMenu);
            switchLanguage(false, menuItemData);
        }

        void tmrCallBgWorker_tick(object sender, EventArgs e)
        {
            if (Monitor.TryEnter(lockObject))
            {
                try
                {
                    //  if bgw is not busy call the worker
                    if (!backgroundWorker.IsBusy)
                        backgroundWorker.RunWorkerAsync();

                }
                finally
                {
                    Monitor.Exit(lockObject);
                }
            }
            else
            {

                tmrEnsureWorkerGetsCalled = new DispatcherTimer();
                tmrEnsureWorkerGetsCalled.Tick += new EventHandler(tmrEnsureWorkerGetsCalled_Callback);
                tmrEnsureWorkerGetsCalled.Interval = new TimeSpan(0, 0, 1);

            }
        }

        void tmrEnsureWorkerGetsCalled_Callback(object obj, EventArgs e)
        {
            try
            {
                if (!backgroundWorker.IsBusy)
                    backgroundWorker.RunWorkerAsync();

            }
            finally
            {
                Monitor.Exit(lockObject);
            }
            tmrEnsureWorkerGetsCalled = null;
        }


        private void BackgroundWorkerOnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            updateSystemInfoManagerPanel();
        }

        private async void BackgroundWorkerOnDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            // while (!worker.CancellationPending)
            {
                infoManager.Update();
                //
                worker.ReportProgress(0);
            }
        }

        private void SystemInfoDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            bool newVal;

            newVal = SystemInfoDetailsButton.Content == FindResource("left");
            if (newVal)
            {
                SystemInfoDetailsButton.Content = FindResource("right");
                SystemInfoDetailsButton.ToolTip = "Show System Info Details";
            }
            else
            {
                SystemInfoDetailsButton.Content = FindResource("left");
                SystemInfoDetailsButton.ToolTip = "Hide System Info Details";
            }
        }

        private void itaItem_Click(object sender, RoutedEventArgs e)
        {
            menuItemData = getDataFromSql.getsqlData(Languages.IT, (ItemStatus)(Convert.ToInt16(!demo)));
            switchLanguage(false, menuItemData);
        }

        private void ukItem_Click(object sender, RoutedEventArgs e)
        {
            menuItemData = getDataFromSql.getsqlData(Languages.ES_UK, (ItemStatus)(Convert.ToInt16(!demo)));
            switchLanguage(true, menuItemData);
        }

        private void switchLanguage(bool doSwitch, List<List<string>> menuItems)
        {
            MenuItem mItem;
            int menuItemsIndex = 0;

            if (menuItems.Count > 0)
            {
                langBtnMenu.Content = FindResource(doSwitch ? "uk" : "ita");
                dictionary.Source = new Uri(@"resources\StringResources.it.xaml", UriKind.Relative);
                langBtnMenu.ToolTip = "Italiano It";
                if (doSwitch)
                {
                    dictionary.Source = new Uri(@"resources\StringResources.en.xaml", UriKind.Relative);
                    langBtnMenu.ToolTip = "English Uk";
                }
                this.Resources.MergedDictionaries.Add(dictionary);
                treeviewItemRoot.Header = dictionary["treeviewRoot"];
                treeviewItemRoot.Foreground = Brushes.Black;

                for (int i = 0; i < MenuButton.ContextMenu.Items.Count; i++)
                {
                    if (MenuButton.ContextMenu.Items[i].ToString() != "System.Windows.Controls.Separator")
                    {
                        mItem = MenuButton.ContextMenu.Items[i] as MenuItem;
                        mItem.Header = menuItems[menuItemsIndex][0];
                        menuItemsIndex++;
                    }
                }
            }
        }

        private void GenerateChartData()
        {
            List<string> keysList;
            List<DataEntry> system;
            int i, j, stop, _salt, updown, numDataItems;
            Random rand;
            Status1 stat1;

            lock (csvData)
            {
                keysList = csvData.Keys.ToList();
                foreach (string key in keysList)
                {
                    i = 0;
                    j = 0;
                    system = csvData[key];
                    numDataItems = system.Count;
                    while (i < numDataItems)
                    {
                        rand = new Random();
                        _salt = rand.Next();
                        updown = _salt % 2;
                        stat1 = (Status1)(_salt % 5);
                        stop = j + (_salt % 200);
                        while ((j < stop) && (j < numDataItems))
                        {
                            system[j].set_status(updown);
                            system[j].set_status1(stat1);
                            j++;
                        }
                        i = j;
                    }
                }
            }
        }

        private void TimerDemo_Tick(object sender, EventArgs e)
        {
            if (fileImported)
            {
                lock (csvData)
                {
                    if (demo)
                    {
                        try
                        {
                            List<string> keysList = csvData.Keys.ToList();
                            foreach (string key in keysList)
                            {
                                Random rand = new Random();
                                int _salt = rand.Next();
                                int updown = (_salt % 2);
                                Status1 stat1 = (Status1)(_salt % 5);
                                csvData[key].Last().set_status(updown);
                                csvData[key].Last().set_status1(stat1);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(dictionary["csvIssue"] + ". " + ex.Message);
                        }
                    }

                    try
                    {
                        foreach (ToggleButton mybutton in Wrap.Children)
                        {
                            Status1 status = csvData[mybutton.ToolTip.ToString().Substring(33)].Last().get_status1();

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
                    catch { return; };
                }
            }
        }

        private void updateSystemInfoManagerPanel()
        {
            ramTotal.Text = infoManager.getInfoRam()["Total"].ToString();
            ramUsed.Text = infoManager.getInfoRam()["UsedMB"].ToString();
            ramFree.Text = infoManager.getInfoRam()["Free"].ToString();
            memoryTxtBlock.Text = infoManager.getInfoRam()["UsedGB"].ToString();
            memoryProgressBar.Value = Convert.ToDouble(infoManager.getInfoRam()["PercentageRam"]);
            memoryProgressBar.Foreground = (Brush)infoManager.getInfoRam()["bgColor"];

            hddTotal.Text = infoManager.getInfoHdd()["Total"].ToString();
            hddUsed.Text = infoManager.getInfoHdd()["UsedMB"].ToString();
            hddFree.Text = infoManager.getInfoHdd()["Free"].ToString();
            diskUsageTxtBlock.Text = infoManager.getInfoHdd()["UsedGB"].ToString();
            diskProgressBar.Value = Convert.ToDouble(infoManager.getInfoHdd()["PercentageHDD"]);
            diskProgressBar.Foreground = (Brush)infoManager.getInfoHdd()["bgColor"];

            cpuUsage.Text = (string)infoManager.getInfoCpuUsage()["CPU"];
            cpuTxtBlock.Text = (string)infoManager.getInfoCpuUsage()["CPU Freq"];
            cpuProgressBar.Value = Convert.ToDouble(infoManager.getInfoCpuUsage()["CPU RAW"]);
            cpuProgressBar.Foreground = (Brush)infoManager.getInfoCpuUsage()["bgColor"];

            netStatus.Text = infoManager.getInfoNet()["Status"].ToString();
            netBytesSent.Text = infoManager.getInfoNet()["Sent"].ToString();
            netBytesReceived.Text = infoManager.getInfoNet()["Received"].ToString();
            netStatusBorder.Background = (Brush)infoManager.getInfoNet()["bgColor"];
            netBytesSentBorder.Background = (Brush)infoManager.getInfoNet()["bgColor"];
            netBytesReceivedBorder.Background = (Brush)infoManager.getInfoNet()["bgColor"];
            sqlserverConnection.Text = infoManager.getInfoDb()["Status"].ToString();
            sqlserverConnection.ToolTip = infoManager.getInfoDb()["Tip"].ToString();
            sqlserverConnectionBorder.Background = (Brush)infoManager.getInfoDb()["bgColor"];
        }

        private void manageDemo()
        {
            demo = !demo;

            if (demo)
            {
                Parallel.Invoke(() => demoStart());
            }
            else
            {
                demoStop();
            }
        }

        private void demoStart() 
        {
            timerDemo = new()
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timerDemo.Tick += TimerDemo_Tick;

            timerDemo.Start();
            ((MenuItem)ContMenu.Items[1]).Header = dictionary["demoStop"];
            MessageBox.Show(dictionary["msgDemoStart"].ToString(), dictionary["msgDemoTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void demoStop()
        {
            timerDemo.Stop();
            ((MenuItem)ContMenu.Items[1]).Header = dictionary["demoStart"];
            MessageBox.Show(dictionary["msgDemoStop"].ToString(), dictionary["msgDemoTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ImportData()
        {
            bool res = false;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            Nullable<bool> result;

            fileImported = false;
            //dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            dlg.InitialDirectory = "C:\\Progetti\\OSN\\OAMD\\OAMDHMI\\HMI\\resources";
            dlg.DefaultExt = ".csv";
            dlg.Filter = "CSV Files|*.csv";
            dlg.Multiselect = true;
            result = dlg.ShowDialog();
            res = result.Value;
            if (res == true)
            {
                csvPath = dlg.FileNames;
                if (csvData != null) csvData.Clear();
                Import_CSV(csvPath, out csvData);
            }
            if ((csvData != null) && (csvData.Count > 0))
            {
                fileImported = true;
            }
            ((MenuItem)ContMenu.Items[0]).IsEnabled = fileImported;
            ((MenuItem)ContMenu.Items[1]).IsEnabled = fileImported;
        }


        private void CloseApp()
        {
            App.Current.Shutdown();
        }

        private void ExportPreview()
        {
            try
            {
                int tabcounter = 0;

                if (Wrap.Children.Count > 0)
                {
                    TabControl tab = DocPanel;
                    List<StackPanel> panel = new();
                    List<ScrollViewer> sv = new();
                    List<CloseableTab> ti = new();
                    bool isOneChecked = false;

                    GenerateChartData();
                    foreach (ToggleButton mybutton in Wrap.Children)
                    {
                        List<DataEntry> samples;
                        isOneChecked = (bool)mybutton.IsChecked;
                        if (isOneChecked)
                        {
                            lock (csvData)
                            {
                                samples = csvData[mybutton.ToolTip.ToString().Substring(33)];
                            }
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
                                Height = 100,
                                ZoomMode = ZoomAndPanMode.X,
                                TooltipPosition = TooltipPosition.Hidden,
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
                                XAxes = new List<Axis> { new Axis { Labeler = (value) => $"{value / 60}m", TextSize = 10, MinStep = step, ForceStepToMin = true, MinLimit = 0, MaxLimit = samples.Last().get_unixtimestamp() - samples.First().get_unixtimestamp() + step / 2 }, },
                                YAxes = new List<Axis> { new Axis { TextSize = 10, MinLimit = 0, MaxLimit = 1, Labels = new string[] { "DOWN", "UP" } } }
                            };
                            if (tabcounter % 10 == 0)
                            {
                                panel.Add(new StackPanel() { Orientation = Orientation.Vertical });
                                sv.Add(new ScrollViewer() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto });
                            }

                            panel[tabcounter / 10].Children.Add(new ToggleButton() { Content = mybutton.ToolTip.ToString().Substring(33), Margin = new Thickness(10, 0, 0, 0), FontSize = 15, Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Left });
                            panel[tabcounter / 10].Children.Add(grafico);
                            tabcounter++;
                        }
                    }
                    for (int i = 0; i <= (tabcounter - 1) / 10; i++)
                    {
                        ti.Add(new CloseableTab());
                        ti[i].Content = sv[i];
                        sv[i].Content = panel[i];
                        ti[i].Title = String.Format("Tab {0}", i + 1);
                        tab.Items.Insert(i+1, ti[i]);
                    }
                    this.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(ChartMouseWheelEvent));
                    Dispatcher.BeginInvoke((Action)(() => tab.SelectedIndex = 1));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        private void MissionReport()
        {
            try
            {
                int tabcounter = 0;
                TabControl tab = DocPanel;
                List<StackPanel> panel = new();
                List<ScrollViewer> sv = new();

                List<CloseableTab> ti = new();

                GenerateChartData();
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
                            Width = 6000,
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
                            XAxes = new List<Axis> { new Axis { Labeler = (value) => $"{value}", TextSize = 10, MinLimit = 0, MaxLimit = maxVal + 50 }, },
                            YAxes = new List<Axis> { new Axis { TextSize = 10, MinLimit = 0, MaxLimit = 6, Labels = new string[] { "", "FAILURE", "DEGRADED", "MAINTENANCE", "UNKNOWN", "OPERATIVE" }, }, }
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

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        private void Import_CSV(string[] filePathList, out Dictionary<string, List<DataEntry>> dataCsv)
        {
            int i = 0, j = 0;
            string filePath = string.Empty, line, allFile, importMsg = dictionary["msgFileImport"].ToString();

            try
            {
                dataCsv = null;
                for (i = 0; i < filePathList.Count(); i++)
                {
                    j = 0;
                    filePath = filePathList[i];
                    if (String.IsNullOrEmpty(filePath))
                        throw new Exception(String.Format(dictionary["msgNoFileSelected"].ToString()));
                    if (!File.Exists(filePath) || (Path.GetExtension(filePath) != ".csv"))
                    {
                        MessageBox.Show(String.Format(dictionary["msgFileNoExists"].ToString(), filePath), dictionary["msgFileNoExistsTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    dataCsv = new Dictionary<string, List<DataEntry>>();
                    StreamReader sR = new(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
                    allFile = sR.ReadToEnd();
                    sR.Close();
                    var lines = allFile.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    while (j < lines.Length)
                    {
                        line = lines[j++];
                        var splittedLine = line.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                        var status1 = splittedLine[3] switch
                        {
                            "OPERATIVE" => (Status1)4,
                            "UNKNOWN" => (Status1)3,
                            "MAINTENANCE" => (Status1)2,
                            "DEGRADED" => (Status1)1,
                            "FAILURE" => (Status1)0,
                            _ => (Status1)3,
                        };
                        DataEntry data = new DataEntry(Double.Parse(splittedLine[1]), Convert.ToBoolean(int.Parse(splittedLine[2])), status1);
                        if (!dataCsv.ContainsKey(splittedLine[0]))
                        {
                            List<DataEntry> list = new() { data };
                            dataCsv.Add(splittedLine[0], list);
                        }
                        else
                        {
                            dataCsv[splittedLine[0]].Add(data);
                        }
                    }
                    importMsg += " " + filePath;
                }
                MessageBox.Show(importMsg, dictionary["msgImportResultTitle"].ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                throw new Exception(dictionary["csvIssue"] + ": " + filePath + ". " + ex.Message);
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

        private void AddContextMenuItem(List<List<string>> menuItems, ContextMenu contextMenu)
        {
            MenuItem CmItem;
            Separator SepItem;
            int i = 0, numItems, itemIndex, stop;

            numItems = stop = menuItems.Count;
            itemIndex = 0;  
            while(i < stop)
            {
                CmItem = new  MenuItem();
                if (i == numItems - 1)
                {
                    SepItem = new Separator();
                    contextMenu.Items.Add(SepItem);
                    stop++;
                }
                else
                {
                    CmItem.Tag = menuItems[itemIndex][1];
                    CmItem.Click += (sender, e) => { ContextMenuItemClick(sender); };
                    contextMenu.Items.Add(CmItem);
                    itemIndex++;
                }
                i++;
            }
            if (numItems > 1)
            {
                ((MenuItem)ContMenu.Items[0]).IsEnabled = fileImported;
                ((MenuItem)ContMenu.Items[1]).IsEnabled = fileImported;
            }
            else
            {
                CmItem = new  MenuItem();
                CmItem.Tag = 1;
                CmItem.Header = "Anteprima";
                CmItem.Click += (sender, e) => { ContextMenuItemClick(sender); };
                contextMenu.Items.Add(CmItem);
                CmItem = new MenuItem();
                CmItem.Tag = 3;
                CmItem.Header = "Importazione Dati";
                CmItem.Click += (sender, e) => { ContextMenuItemClick(sender); };
                contextMenu.Items.Add(CmItem);
                CmItem = new MenuItem();
                CmItem.Tag = 5;
                CmItem.Header = "Chiudi";
                CmItem.Click += (sender, e) => { ContextMenuItemClick(sender); };
                contextMenu.Items.Add(CmItem);
            }
        }

        private void Tv_MouseUp(object sender, MouseButtonEventArgs e)
        {
            selectTreeViewItem(sender);
        }

        private void selectTreeViewItem(object send)
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
                }
            }
        }

        private void AddToWrapPanel(object header, object tag)
        {
            if (fileImported)
            {
                Status1 status = Status1.NOSTATUS;
                ToggleButton mybutton = new()
                {
                    Margin = new Thickness(10, 20, 10, 20),
                    MinHeight = 30,
                    MaxHeight = 100,
                    MaxWidth = 300,
                    MinWidth = 100
                };

                mybutton.Content = new TextBlock()
                {
                    Name = "togglebuttonTextBlock",
                    Text = header.ToString().Replace("_", " "),
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap

                };

                lock (csvData)
                {
                    if (csvData.ContainsKey((string)tag))
                    {
                        status = csvData[(string)tag].Last().get_status1();
                    }
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
                mybutton.Style = (Style)Resources["button"];
                Wrap.Children.Add(mybutton);
            }
        }
        private void DoubleClick(object sender, RoutedEventArgs e)
        {
            ToggleButton button = (ToggleButton)sender;

            for (int i = 0; i < dirTree.Items.Count; i++)
            {
                TextBlock tb = button.Content as TextBlock;
                LookForTvItem((TreeViewItem)dirTree.Items[i], tb.Text.Replace(" ", "_"));
            }
            selectTreeViewItem(dirTree);
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
                tvScrollview.ScrollToBottom();
                vBottomValue = tvScrollview.VerticalOffset;
                tvScrollview.ScrollToTop();
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

        public static BitmapImage createbitmapImage(string P, int h)
        {
            BitmapImage bitmapImage = new();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(P);
            bitmapImage.DecodePixelHeight = h;
            bitmapImage.EndInit();

            return bitmapImage;
        }

        private void ContextMenuItemClick(object sender)
        {
            MenuItem selectedMenuItem = sender as MenuItem;

            if (selectedMenuItem != null)
            {
                switch (selectedMenuItem.Tag.ToString())
                {
                    case "1":
                        ExportPreview();
                        break;
                    case "2":
                        manageDemo();
                        break;
                    case "3":
                        ImportData();
                        break;
                    case "4":
                        break;
                    case "5":
                        CloseApp();
                        break;
                    case "6":
                        MissionReport();
                        break;
                }
            }
        }

        /*Aggiunta da GPO per impedire minimizzazione*/
        private async void MainWindowOAMD_StateChanged(object sender, EventArgs e)
        {
            //await fullMan.MaximizeWindow(this);
        }

        private void searchresultsbuttonDown_Click(object sender, RoutedEventArgs e)
        {
            int selectedItems = selectedItemList.Count;
            if (selectedItemList.Count > 0)
            {
                isBntDown = true;
                isBntUp = false;
                selectedItemIndex++;
                if (selectedItemIndex == selectedItems)
                {
                    selectedItemIndex = 0;
                    vOffset = 0;
                }
                selectedItemList[selectedItemIndex].IsSelected = true;
                selectTreeViewItem((object)dirTree);
                selectedItemList[selectedItemIndex].BringIntoView();
                tvScrollview.ScrollToVerticalOffset(vOffset);
            }
        }

        private void searchresultsbuttonUp_Click(object sender, RoutedEventArgs e)
        {
            int selectedItems = selectedItemList.Count;
            if (selectedItemList.Count > 0)
            {
                isBntDown = false;
                isBntUp = true;
                selectedItemIndex--;
                if (selectedItemIndex <= -1)
                {
                    selectedItemIndex = selectedItems - 1;
                    vOffset = vBottomValue;
                }
                selectedItemList[selectedItemIndex].IsSelected = true;
                selectTreeViewItem((object)dirTree);
                selectedItemList[selectedItemIndex].BringIntoView();
                tvScrollview.ScrollToVerticalOffset(vOffset);
            }
        }

        private void tvScrollview_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scView = (ScrollViewer)(sender);
            bool isVoffsetChanged = (scView.VerticalOffset != vOffset);

            if (isVoffsetChanged)
            {
                vOffset = scView.VerticalOffset;
                if (isBntDown)
                {
                    vOffset += 500;
                }
                else if (isBntUp)
                {
                    vOffset -= 500;
                }
            }
            isBntDown = false;
            isBntUp = false;
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
                if (counter > 6) MessageBox.Show("Non è rotto");
                // loop dispari
                if (child.GetType().ToString() == "System.Windows.Controls.Primitives.ToggleButton")
                {
                    var btn = child as ToggleButton;
                    zoom = (bool)btn.IsChecked;
                }
                else if (zoom) //loop dispari (condizionale)
                {
                    var graph = child as CartesianChart;

                    //graph.RemoveHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(ChartMouseWheelEvent));
                    ((CartesianChart)child).RaiseEvent(e);
                    //graph.AddHandler(UIElement.MouseWheelEvent, new MouseWheelEventHandler(ChartMouseWheelEvent));
                }
                counter++;
            }
        }
    }
}