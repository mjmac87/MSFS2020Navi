using MahApps.Metro.Controls;
using Microsoft.FlightSimulator.SimConnect;
using Microsoft.Maps.MapControl.WPF;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace MSFS2020Navi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        readonly IntPtr handle;
        readonly HwndSource handleSource;

        // User-defined win32 event
        const int WM_USER_SIMCONNECT = 0x0402;

        // SimConnect object
        SimConnect simConnect = null;

        private Location mapCenter = new Location(52.329989, -0.182659);

        public Location MapCenter
        {
            get { return mapCenter; }
            set
            {
                mapCenter = value;
                RaisePropertyChanged(nameof(MapCenter));
            }
        }

        private Uri planeIcon = new Uri("plane-icon.png", UriKind.Relative);

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        enum DEFINITIONS
        {
            Struct1,
        }

        enum DATA_REQUESTS
        {
            REQUEST_1,
        };

        // this is how you declare a data structure so that
        // simconnect knows how to fill it/read it.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1
        {
            // this is how you declare a fixed size string
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public String title;
            public double latitude;
            public double longitude;
            public double altitude;
            public double heading;
        };

        readonly MapLayer ImageLayer = new MapLayer();
        readonly Image PlaneIconImage = new Image();

        public MainWindow()
        {
            this.DataContext = this;
            Topmost = true;

            InitializeComponent();
            handle = new WindowInteropHelper(this).EnsureHandle(); // Get handle of main WPF Window
            handleSource = HwndSource.FromHwnd(handle); // Get source of handle in order to add event handlers to it
            handleSource.AddHook(HandleSimConnectEvents);

            SetButtons(true, false, false);

            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1)
            };
            timer.Tick += UpdatePosition;
            timer.Start();

            PlaneIconImage.Height = 48;
            PlaneIconImage.Width = 48;
            PlaneIconImage.Source = new BitmapImage(planeIcon);
            PlaneIconImage.Stretch = System.Windows.Media.Stretch.None;

            PlaneIconImage.HorizontalAlignment = HorizontalAlignment.Center;
            PlaneIconImage.VerticalAlignment = VerticalAlignment.Center;

            Location planeIconLocation = mapCenter;
            PositionOrigin planeIconPosition = PositionOrigin.Center;
            ImageLayer.AddChild(PlaneIconImage, planeIconLocation, planeIconPosition);
            myMap.Children.Add(ImageLayer);
        }

        ~MainWindow()
        {
            if (handleSource != null)
            {
                handleSource.RemoveHook(HandleSimConnectEvents);
            }
        }

        private IntPtr HandleSimConnectEvents(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam, ref bool isHandled)
        {
            isHandled = false;

            switch (message)
            {
                case WM_USER_SIMCONNECT:
                    {
                        if (simConnect != null)
                        {
                            simConnect.ReceiveMessage();
                            isHandled = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            return IntPtr.Zero;
        }

        private void SetButtons(bool bConnect, bool bGet, bool bDisconnect)
        {
            ConnectButton.IsEnabled = bConnect;
            RequestDataButton.IsEnabled = bGet;
            DisconnectButton.IsEnabled = bDisconnect;
        }

        private void CloseConnection()
        {
            if (simConnect != null)
            {
                simConnect.Dispose();
                simConnect = null;
                DisplayText("Connection closed");
            }
        }

        // Response number
        int response = 1;
        string output = "\n\n\n\n\n\n\n\n\n\n";

        void DisplayText(string s)
        {
            output = output.Substring(output.IndexOf("\n") + 1);
            output += "\n" + response++ + ": " + s;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (simConnect == null)
            {
                try
                {
                    // the constructor is similar to SimConnect_Open in the native API
                    simConnect = new SimConnect("Managed Data Request", this.handle, WM_USER_SIMCONNECT, null, 0);

                    SetButtons(false, true, true);

                    InitDataRequest();

                }
                catch (COMException ex)
                {
                    DisplayText("Unable to connect to MSFS2020:\n\n" + ex.Message);
                }
            }
            else
            {
                DisplayText("Error - try again");
                CloseConnection();

                SetButtons(true, false, false);
            }
        }

        // Set up all the SimConnect related data definitions and event handlers
        private void InitDataRequest()
        {
            try
            {
                simConnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(SimConnect_OnRecvOpen);
                simConnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(SimConnect_OnRecvQuit);
                simConnect.OnRecvException += new SimConnect.RecvExceptionEventHandler(SimConnect_OnRecvException);

                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "title", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Latitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Longitude", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Altitude", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simConnect.AddToDataDefinition(DEFINITIONS.Struct1, "Plane Heading Degrees Magnetic", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simConnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);

                simConnect.OnRecvSimobjectDataBytype += new SimConnect.RecvSimobjectDataBytypeEventHandler(SimConnect_OnRecvSimobjectDataBytype);

            }
            catch (COMException ex)
            {
                DisplayText(ex.Message);
            }
        }

        void SimConnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            DisplayText("Connected to MSFS2020");
        }

        // The case where the user closes Prepar3D
        void SimConnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            DisplayText("MSFS2020 has exited");
            CloseConnection();
        }

        void SimConnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            DisplayText("Exception received: " + data.dwException);
        }

        void SimConnect_OnRecvSimobjectDataBytype(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA_BYTYPE data)
        {

            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_1:
                    Struct1 s1 = (Struct1)data.dwData[0];

                    //displayText("title: " + s1.title);
                    //displayText("Lat:   " + s1.latitude);
                    //displayText("Lon:   " + s1.longitude);
                    //displayText("Alt:   " + s1.altitude);
                    
                    DisplayText("Heading:   " + s1.heading);
                    mapCenter = new Location(s1.latitude, s1.longitude);
                    myMap.SetView(mapCenter, 16, 0d);

                    MapLayer.SetPosition(ImageLayer.FindChild<Image>(), mapCenter);
                    RotateTransform rotateTransform = new RotateTransform(s1.heading);
                    ImageLayer.FindChild<Image>().RenderTransform = rotateTransform;

                    break;

                default:
                    DisplayText("Unknown request ID: " + data.dwRequestID);
                    break;
            }
        }

        void UpdatePosition(object sender, EventArgs e)
        {
            if (simConnect == null)
            {
                return;
            }

            simConnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            CloseConnection();
            SetButtons(true, false, false);
        }

        private void RequestDataButton_Click(object sender, RoutedEventArgs e)
        {
            // The following call returns identical information to:
            // simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE);

            simConnect.RequestDataOnSimObjectType(DATA_REQUESTS.REQUEST_1, DEFINITIONS.Struct1, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
            DisplayText("Request sent...");
        }
    }
}
