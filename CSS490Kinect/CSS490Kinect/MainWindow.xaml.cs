using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace CSS490Kinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Mode mode = Mode.Color;
        private int framesCaptured = 0;
        KinectSensor sensor = null;
        FrameReducer frameReducer = null;
        List<People> currentPeople = null;


        private MultiSourceFrameReader msfr = null;

        private CoordinateMapper coordinateMappter = null;


        ushort[] irData;
        byte[] irDataConverted;
        WriteableBitmap irBitmap;

        //Main
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            //Initialize the FrameReducer
            frameReducer = new FrameReducer();
            
        }

        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            sensor = KinectSensor.GetDefault();
            if (sensor != null)
            {
                sensor.Open();
            }
            msfr = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color
                                                     | FrameSourceTypes.Depth
                                                     | FrameSourceTypes.Infrared);
            coordinateMappter = sensor.CoordinateMapper;

            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;
        }

        void msfr_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var multiFrame = e.FrameReference.AcquireFrame();

            using (var colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null && mode == Mode.Color)
                {
                    CameraImage.Source = colorFrame.ToBitmap();
                }
            }

            using (var depthFrame = multiFrame.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame != null && mode == Mode.Depth)
                {
                    CameraImage.Source = depthFrame.ToBitmap();
                }
            }

            using (var irFrame = multiFrame.InfraredFrameReference.AcquireFrame())
            {
                if (irFrame != null && mode == Mode.Infrared)
                {
                    CameraImage.Source = irFrame.ToBitmap();
                }
            }
        }

        


        //Turn the list of people into string information
        //Each person will be line deliminted on the UI
        private void updatePeopleInfo()
        {
            //Count the total number of frames
            framesCaptured++;
            List<People> currentPeople = frameReducer.GetPeople();

            //Update the Text Information in the UI
            BodiesTracked.Text = "Bodies Tracked: " + frameReducer.CurrentBodyCount;
            FacesTracked.Text = "Faces Tracked: " + frameReducer.CurrentFaceCount;
            string currentPeopleInfo = "";
            
            foreach (People p in currentPeople)
            {
                currentPeopleInfo += "Tracking ID: " + p.TrackingID + " Engaged: " + p.Engauged + " EyesOpen: " + p.EyesOpen + "\n" + "Vector: x:" + p.FaceOrientaion.X + " y:" + p.FaceOrientaion.Y + " z:" + p.FaceOrientaion.Z + " w:" + p.FaceOrientaion.W + "\n";
            }
            FrameCount.Text = "" + framesCaptured;
            BodyFramesProcessed.Text = "BodyFramesProcessed: " + frameReducer.BodyFramesProcessed;
            FaceFramesProcessed.Text = "FaceFramesProcessed: " + frameReducer.FaceFramesProcessed;
            PeopleInfo.Text = currentPeopleInfo;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            updatePeopleInfo();
        }


        private void IR_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.Infrared;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.Depth;
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            mode = Mode.Color;
        }

        public enum Mode
        {
            Color,
            Depth,
            Infrared
        }
    }
}
