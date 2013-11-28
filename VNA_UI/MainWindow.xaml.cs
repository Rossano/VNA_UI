using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using Xceed.Wpf.DataGrid;
using InstrumentsDotNetNative;
using System.Globalization;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ZedGraph;
using System.Drawing;


namespace VNA_GUI
{

    /// <summary>
    /// Management Logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Members

        private List<MeasDataItem> MeasData = new List<MeasDataItem>();
        private bool isConnected = false;
        private HP8753DotNet hp8753;
        private double[] X;
        private double[] ReY;
        private double[] ImY;
        private MasterPane plotGraphPane;
        
        #endregion Members

        #region Constructors

        public MainWindow()
        {
            //hp8753 = new HP8753DotNet();
            isConnected = false;            
            InitializeComponent();
            connect_button.Content = "Local -> Remote";
            // Link to the XAML plot graph
            plotGraphPane = dataGraph.MasterPane;
        }

        #endregion Constructors

        /// <summary>
        /// Error Dialog. This function concentrates all the error display dialogs.
        /// </summary>
        /// <param name="msg">The Message to diplay on the Message Box.</param>
        private void errDlg (string msg)
        {
            Xceed.Wpf.Toolkit.MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        /// <summary>
        /// Reads the data.
        /// </summary>
        private void readData()
        {
            try
            {
                string raw = hp8753.getRawMeasurementData();
                char[] dummy = { ',', ' ' };
                string[] data = raw.Split(dummy, StringSplitOptions.RemoveEmptyEntries);
                // get rid of \n
                for (int k = 0; k < data.Count(); k++ )
                {
                    data[k] = data[k].Replace("\n", "");
                }

                // Read Start & Stop frequencies
                string startFreq = hp8753.Query("STAR?");
                string stopFreq = hp8753.Query("STOP?");
                NumberFormatInfo provider =new NumberFormatInfo();
                provider.NumberDecimalSeparator = ".";
                provider.NumberGroupSeparator = ",";
                //startFreq = startFreq.Replace("+", "").ToLower();
                double Fstart = Convert.ToDouble(startFreq, provider);
                double Fstop = Convert.ToDouble(stopFreq, provider);
                double freq = 0;

                X = new double[hp8753._points];
                ReY = new double[hp8753._points];
                ImY = new double[hp8753._points];
                int n = (int)data.Count();
                uint i = 0;
                uint index = 0;
                while (n > 0)
                {
                    // Carries out the measurements frequency points 
                    if (logX_checkBox.IsChecked == true)
                    {
                        freq = Fstart * Math.Pow(10, index * Math.Log10(Fstop / Fstart) / (hp8753._points - 1));
                    }
                    else
                    {
                        freq = Fstart + (index - 0) * (Fstop - Fstart) / (hp8753._points - 1);
                    }                    
                    MeasData.Add(new MeasDataItem(freq.ToString(), data[i], data[i + 1]));
                    X[index] = freq;
                    ReY[index] = Convert.ToDouble(data[i]);
                    ImY[index] = Convert.ToDouble(data[i + 1]);
                    i += 2;
                    n -= 2;
                    index++;
                }
            }
            catch (FormatException e)
            {
                errDlg("Bad format number exception: " + e.Message);
            }
            catch (Exception ex)
            {
                if (hp8753.isRemote == true)
                {
                    hp8753.isRemote = false;
                    hp8753.GoToLocal();
                    isConnected = false;
                }
                else
                {
                    isConnected = false;
                    if (hp8753.IsInitialized == true) hp8753.GoToLocal();
                }
                connect_button.Content = "Local -> Remote";
                errDlg("Error reading data: " + ex.Message);
            }
        }

        /// <summary>
        /// Handles the Click event of the data2clip_button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void data2clip_button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Empty the clipboard
                System.Windows.Clipboard.Clear();
                // and clear old data
                MeasData.Clear();
                this.readData();
                StringBuilder sb = new StringBuilder();
                foreach(MeasDataItem point in MeasData)
                {
                    string str = string.Format("{0}\t{1}\t{2}", point.freq, point.Real, point.Imaginary);
                    sb.AppendLine(str);
                }
                System.Windows.Clipboard.SetText(sb.ToString());
                // Well now it should be nice to fill the DataGrid to show that you have coded something :-)
                dataBox.Text = sb.ToString();
                UpdatePlot();
            }
            catch (Exception)
            {

            }
            finally
            {               
            }

        }

        /// <summary>
        /// Handles the Click event of the data to Excel button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void data2Xls_button_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Handles the Click event of the connect button control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
        private void connect_button_Click(object sender, RoutedEventArgs e)
        {
            if (isConnected == false)
            {
                try
                {
                    int gpibAddress = (int)Convert.ToDecimal(GpibAddress.Value);
                    hp8753 = new HP8753DotNet(gpibAddress);
                    hp8753.Initialize();
                    string IdString = hp8753.Query("*IDN?");
                    if (hp8753.RecognizedIdString(IdString) == false)
                    {
                        errDlg("Agilent/HP 8753 not recognized\nContinuing anyway...");
                        //throw new NotSupportedException("Agilent/HP 8753 not recognized");
                    }
                    hp8753.setNumPoints(Convert.ToUInt16(points.Value));
                    hp8753.setDataTransferFormat("FORM4");
                    if (hp8753.setRemote() == true)
                    {
                        connect_button.Content = "Remote -> Local";
                    }
                    else
                    {
                        connect_button.Content = "Local -> Remote";
                    }
                }
                catch (Exception ex)
                {
                    hp8753.GoToLocal();
                    hp8753.isRemote = false;
                    connect_button.Content = "Local -> Remote";
                    errDlg("Error Connecting to Remote Device: " + ex.Message);
                }
            }
            else
            {
                hp8753.isRemote = false;
                hp8753.GoToLocal();
                isConnected = false;
                connect_button.Content = "Local";
            }
        }

        private void GridTab_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void PlotTab_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        #region Manage Graphs

        /// <summary>
        /// Data Plot Creation on the Graphical tab of the window.
        /// </summary>
        private void UpdatePlot()
        {
            // Clean up
            plotGraphPane.PaneList.Clear();

            // Add a title
            plotGraphPane.Title.Text = "HP8753 Measurement Data";
            plotGraphPane.Title.IsVisible = true;

            // Add some Margins
            plotGraphPane.Margin.All = 5;

            // Create the plot pane
            GraphPane _pane = new GraphPane(new RectangleF(), "HP8357 Measurement", "Frequency", "HP8753 Data");
            plotGraphPane.Add(_pane);

            // Create Data Point Structures
            PointPairList _pointsRE = new PointPairList();
            PointPairList _pointsIM = new PointPairList();

            // Do the next only if there are some data to fill, else do nothing
            try
            {
                // Fill the data
                for (int i = 0; i < X.Length; i++)
                {
                    _pointsRE.Add(X[i], ReY[i]);
                    _pointsIM.Add(X[i], ImY[i]);

                }

                // Add curves
                LineItem _curveRE = _pane.AddCurve("HP8357 Re Data", _pointsRE, System.Drawing.Color.Red, SymbolType.Default);
                LineItem _curveIM = _pane.AddCurve("HP8357 Im Data", _pointsIM, System.Drawing.Color.Blue, SymbolType.Diamond);

                // Set up Axis
                if (logX_checkBox.IsChecked == true)
                {
                    _pane.XAxis.Type = AxisType.Log;
                    
                }
                else
                {
                    _pane.XAxis.Type = AxisType.Linear;
                }
                if (Y_log_CheckBox.IsChecked == true)
                {
                    _pane.YAxis.Type = AxisType.Log;
                }
                else
                {
                    _pane.YAxis.Type = AxisType.Linear;
                }

                using (System.Drawing.Graphics g = dataGraph.CreateGraphics())
                {
                    
                    plotGraphPane.AxisChange();
                }
                dataGraph.Refresh();
            }
            catch (Exception ex)
            {
                // Do nothing
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePlot();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        #endregion


    }

    /// <summary>
    /// Measurement Data point to build the Data Grid of the GUI
    /// </summary>
    public class MeasDataItem
    {
        public string freq { get; set; }
        public string Real { get; set; }
        public string Imaginary { get; set; }

        public MeasDataItem(string f, string re, string im)
        {
            freq = f;
            Real = re;
            Imaginary = im;
        }

    }

    /// <summary>
    /// DataGrid Interaction class
    /// </summary>
    public class MainWindowViewModel
    {
        public ICollectionView _dataCollection { get; private set; }
        private List<MeasDataItem> _data;

        public MainWindowViewModel()
        {
            _data = new List<MeasDataItem>();
        }

        public void fillData(MeasDataItem[] d)
        {
            try
            {
                _data.Clear();
                foreach (MeasDataItem item in d)
                {
                    _data.Add(item);
                }
                _dataCollection = CollectionViewSource.GetDefaultView(_data);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

}
