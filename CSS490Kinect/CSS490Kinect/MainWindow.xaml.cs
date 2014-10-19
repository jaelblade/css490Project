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

        //Kinect Sensor Variable
        private KinectSensor sensor = null;

        //Coordinate mapper for location on image
        private CoordinateMapper coordMapper = null;

        
        
        //ColorCamera Variables

        //Size of RBG pixelin bitmap
        private readonly int bytePerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        //FrameReader for color output
        private ColorFrameReader colorFR = null;

        //Array of color picels
        private byte[] colorPixels = null;

        //Writeable Bitmap link
        private WriteableBitmap colorBitmap = null;

        //Body Frame Variables

        //BodyFrameReder
        private BodyFrameReader bodyFR = null;

        //Array of bodies
        private Body[] bodies = null;

        //Number of tracked bodies
        private int bodyCount;

        //FaceFrame Variables

        //Face Sources
        private FaceFrameSource[] faceFSs = null;

        //Face Reader
        private FaceFrameReader[] faceFRs = null;

        //Face Results
        private FaceFrameResult[] faceFResults = null;

        // specify the required face frame results
        FaceFrameFeatures faceFrameFeatures =
            FaceFrameFeatures.BoundingBoxInColorSpace
            | FaceFrameFeatures.PointsInColorSpace
            | FaceFrameFeatures.RotationOrientation
            | FaceFrameFeatures.FaceEngagement
            | FaceFrameFeatures.Glasses
            | FaceFrameFeatures.Happy
            | FaceFrameFeatures.LeftEyeClosed
            | FaceFrameFeatures.RightEyeClosed
            | FaceFrameFeatures.LookingAway
            | FaceFrameFeatures.MouthMoved
            | FaceFrameFeatures.MouthOpen;


        //Main
        public MainWindow()
        {
            
            // initialize the components (controls) of the window
            InitializeComponent();
            InitKinect();
        }

        private void InitKinect()
        {
            //Get first active kinect Sensor
            sensor = KinectSensor.GetDefault();

            // set IsAvailableChanged event notifier
            sensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            //Get coordinate mapper
            coordMapper = sensor.CoordinateMapper;

            //If no sensor, do nothing
            if (sensor == null)
            {
                return;
            }

            //Open connection to sensor
            sensor.Open();

            InitKinectCamera();

            InitKinectBody();

            InitKinectFace();
        }

        
        //Initialize the Body Frame Features
        private void InitKinectBody()
        {
            //get the body frame reader
            bodyFR = sensor.BodyFrameSource.OpenReader();

            //Handle for Body Frame Arrival
            bodyFR.FrameArrived += bodyFR_FrameArrived;

            //Set maximum body count
            bodyCount = sensor.BodyFrameSource.BodyCount;

            //allocate body array
            bodies = new Body[bodyCount];
        }


        //Initialize the Face Frame Features
        private void InitKinectFace()
        {
            faceFSs = new FaceFrameSource[bodyCount];
            faceFRs = new FaceFrameReader[bodyCount];
            faceFResults = new FaceFrameResult[bodyCount];

            for (int i = 0; i < bodyCount; i++)
            {
                faceFSs[i] = new FaceFrameSource(sensor, 0, faceFrameFeatures);
                faceFRs[i] = faceFSs[i].OpenReader();
            }


        }


        //Initialize the Color Camera
        private void InitKinectCamera()
        {
            //Get frame description for color
            FrameDescription frameDesc = sensor.ColorFrameSource.FrameDescription;

            //Get color frame reader
            colorFR = sensor.ColorFrameSource.OpenReader();

            //Allocate pixel array
            colorPixels = new byte[frameDesc.Width * frameDesc.Height * bytePerPixel];

            //Create writable bitmap
            colorBitmap = new WriteableBitmap(frameDesc.Width, frameDesc.Height,96,96,PixelFormats.Bgr32,null);

            //Link the 
            CameraImage.Source = colorBitmap;

            //hookup event
            colorFR.FrameArrived += colorFR_FrameArrived;
        }

        //Get Body Index of the Face Frame Source
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < bodyCount; i++)
            {
                if (faceFSs[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        //Color frame arrival event
        void colorFR_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                //Check if frame is valid
                if (colorFrame != null)
                {
                    //get frame description
                    FrameDescription fDesc = colorFrame.FrameDescription;

                    //Copy data into array
                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        colorFrame.CopyRawFrameDataToArray(colorPixels);
                    }
                    else
                    {
                        colorFrame.CopyConvertedFrameDataToArray(colorPixels, ColorImageFormat.Bgra);
                    }

                    //Copy output to bitmap
                    colorBitmap.WritePixels(
                        new Int32Rect(0,0,fDesc.Width,fDesc.Height),
                        colorPixels,
                        fDesc.Width * bytePerPixel,
                        0);
                }
            }
        }

        //Body frame arrivalevent
        void bodyFR_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    //Refresh the body array with body data from frame
                    bodyFrame.GetAndRefreshBodyData(bodies);

                    //iterate through each face source
                    for (int i = 0; i < bodyCount; i++)
                    {
                        //Check if body is tracked
                        if (bodies[i].IsTracked)
                        {
                            //Update face frame source with tracked ID
                            faceFSs[i].TrackingId = bodies[i].TrackingId;
                        }
                    }
                }
            }
            UpdateBodyStatus();
        }

        private void UpdateBodyStatus()
        {
            int currentBodies = 0;
            int facesTracked = 0;
            for (int i = 0; i < bodyCount; i++)
            {
                if (bodies[i].IsTracked)
                {
                    currentBodies++;
                }
                if (faceFSs[i].IsTrackingIdValid)
                {
                    facesTracked++;
                }
            }
            BodiesTracked.Text = "Bodies Currently Tracked: " + currentBodies;
            FacesTracked.Text = "Faces Currently Tracked: " + facesTracked;
        }


        //Sensor Change Event
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (sensor != null)
            {
                KinectStatus.Text = sensor.IsAvailable ? Properties.Resources.RunningStatusText : Properties.Resources.SensorNotAvailableStatusText;
            }
        }

        //Main Window Loaded Execution Events
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < bodyCount; i++)
            {
                if (faceFRs[i] != null)
                {
                    faceFRs[i].FrameArrived += Reader_FaceFrameArrived;
                }
            }

            if (bodyFR != null)
            {
                bodyFR.FrameArrived += bodyFR_FrameArrived;
            }
        }


        //Face Frame Arrival
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    int index = GetFaceSourceIndex(faceFrame.FaceFrameSource);
                    if (faceFrame.IsTrackingIdValid)
                    {
                        faceFResults[index] = faceFrame.FaceFrameResult;
                    }
                    else
                    {
                        faceFResults[index] = null;
                    }
                }
            }
        }
    }
}
