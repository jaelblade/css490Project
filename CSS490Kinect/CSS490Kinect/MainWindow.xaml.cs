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
using System.Windows.Controls;

namespace CSS490Kinect
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        Mode mode = Mode.Color;
        Boolean ImageEnabled;
        private int framesCaptured = 0;
        KinectSensor sensor = null;
        FrameReducer frameReducer = null;
        List<People> currentPeople = null;
        Calculator calc = null;

        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));
        private readonly Brush inferredJointBrush = Brushes.Yellow;


        private MultiSourceFrameReader msfr = null;

        private CoordinateMapper coordinateMappter = null;

        private DrawingGroup drawingGroup = null;

        //Main
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            //Initialize the FrameReducer
            frameReducer = new FrameReducer();
            calc = new Calculator();
            ImageEnabled = false;

            drawingGroup = new DrawingGroup();
            
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
            if (!ImageEnabled)
            {
                CameraImage.Visibility = System.Windows.Visibility.Hidden;
                return;
            }
            else
            {
                CameraImage.Visibility = System.Windows.Visibility.Visible;
            }
            var multiFrame = e.FrameReference.AcquireFrame();

            using (var colorFrame = multiFrame.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null && mode == Mode.Color)
                {
                    CameraImage.Source = drawPeopleVectors(colorFrame.ToBitmap());
                }
            }

            using (var depthFrame = multiFrame.DepthFrameReference.AcquireFrame())
            {
                if (depthFrame != null && mode == Mode.Depth)
                {
                    CameraImage.Source = drawPeopleVectors(depthFrame.ToBitmap());
                }
            }

            using (var irFrame = multiFrame.InfraredFrameReference.AcquireFrame())
            {
                if (irFrame != null && mode == Mode.Infrared)
                {
                    CameraImage.Source = drawPeopleVectors(irFrame.ToBitmap());
                }
            }
        }

        


        //Turn the list of people into string information
        //Each person will be line deliminted on the UI
        private void updatePeopleInfo()
        {

            currentPeople = frameReducer.GetPeople();
            double score = calc.calculateAttentionScore(currentPeople);
            //Update the Text Information in the UI
            BodiesTracked.Text = "Bodies Tracked: " + frameReducer.CurrentBodyCount;
            FacesTracked.Text = "Faces Tracked: " + frameReducer.CurrentFaceCount;
            string currentPeopleInfo = "";
            
            foreach (People p in currentPeople)
            {
                currentPeopleInfo += "Tracking ID: " + p.TrackingID + " Engaged: " + p.Engauged + " EyesOpen: " + p.EyesOpen + "\n" + 
                                        "Vector: x:" + p.FaceOrientaion.X + " y:" + p.FaceOrientaion.Y + " z:" + p.FaceOrientaion.Z + " w:" + p.FaceOrientaion.W + "\n" +
                                        "Head Joint X: " + p.HeadJoint.X + " Y: " + p.HeadJoint.Y + " Z: " + p.HeadJoint.Z + "\n";
            }
            BodyFramesProcessed.Text = "BodyFramesProcessed: " + frameReducer.BodyFramesProcessed;
            FaceFramesProcessed.Text = "FaceFramesProcessed: " + frameReducer.FaceFramesProcessed;
            PeopleInfo.Text = currentPeopleInfo;
            CalcNum.Text = "Current Score: " + score;
        }

        private ImageSource drawPeopleVectors(ImageSource imageSource)
        {
            //Check if there are any people to draw vectors with
            if(currentPeople != null && currentPeople.Count > 0)
            {
                using (DrawingContext dc = drawingGroup.Open())
                {
                    
                    Brush brush = Brushes.Orange;
                    Pen pen = new Pen(brush, 2.0);
                    //Need to map to color space points
                    if (mode == Mode.Color)
                    {
                        //Draw the image before drawing points and lines
                        dc.DrawImage(imageSource, new Rect(0.0, 0.0, sensor.ColorFrameSource.FrameDescription.Width, sensor.ColorFrameSource.FrameDescription.Height));
                        foreach (People p in currentPeople)
                        {
                            //Get the point in color space
                            ColorSpacePoint colorPoint = coordinateMappter.MapCameraPointToColorSpace(p.HeadJoint);
                            Point headPoint = new Point(colorPoint.X, colorPoint.Y);
                            dc.DrawEllipse(brush, pen, headPoint, 10.0, 10.0);

                            //Calculate Vector End Points
                            CameraSpacePoint vectorEnd = p.HeadJoint;
                            vectorEnd.X -= p.FaceOrientaion.X / p.FaceOrientaion.W;
                            vectorEnd.Y += p.FaceOrientaion.Y / p.FaceOrientaion.W;
                            vectorEnd.Z += p.FaceOrientaion.Z / p.FaceOrientaion.W;

                            ColorSpacePoint colorPointEnd = coordinateMappter.MapCameraPointToColorSpace(vectorEnd);
                            Point vector = new Point(colorPointEnd.X, colorPointEnd.Y);

                            dc.DrawLine(pen, headPoint, vector);
                        }

                    }

                    //The depth and infrared use the depth space point
                    if (mode == Mode.Depth || mode == Mode.Infrared)
                    {
                        foreach (People p in currentPeople)
                        {
                            DepthSpacePoint depthPoint = coordinateMappter.MapCameraPointToDepthSpace(p.HeadJoint);
                        }
                    }
                }

                DrawingImage drawingImage = new DrawingImage(drawingGroup);
                return drawingImage;
            }

            return imageSource;
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

        private void Toggle_Click(object sender, RoutedEventArgs e)
        {
            ImageEnabled = !ImageEnabled;
        }
    }
}
