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
        private int framesCaptured = 0;
        KinectSensor sensor = null;
        public event PropertyChangedEventHandler PropertyChanged;
        FrameReducer frameReducer = null;
        StreamWriter writer = new StreamWriter("C:\\sessiondata.csv");
        

        //Main
        public MainWindow()
        {
            // initialize the components (controls) of the window
            sensor = KinectSensor.GetDefault();

            //Add the frame arrival event to the main UI for updating information
            sensor.BodyFrameSource.FrameCaptured += BodyFrameSource_FrameCaptured;

            InitializeComponent();

            //Initialize the FrameReducer
            frameReducer = new FrameReducer();
            
            
        }

        //Framecaptured Evenet
        void BodyFrameSource_FrameCaptured(object sender, FrameCapturedEventArgs e)
        {
            //Count the total number of frames
            framesCaptured++;
            //Update the Text Information in the UI
            BodiesTracked.Text = "Bodies Tracked: " + frameReducer.CurrentBodyCount;
            FacesTracked.Text = "Faces Tracked: " + frameReducer.CurrentFaceCount;
            FrameCount.Text = "" + framesCaptured;
            PeopleInfo.Text =  updatePeopleInfo();

        }

        //Turn the list of people into string information
        //Each person will be line deliminted on the UI
        private string updatePeopleInfo()
        {
            
            int counter = 0;
            string currentPeopleInfo = "";
            List<People> currentPeople = frameReducer.GetPeople();
            foreach (People p in currentPeople)
            {
                currentPeopleInfo += "Tracking ID: " + p.TrackingID + " Engaged: " + p.Engauged + " EyesOpen: " + p.EyesOpen + "\n";
                writer.WriteLine("{0},{1},{2}", p.TrackingID, counter, p.Engauged);
            }
            counter++;
            return currentPeopleInfo;
        }

    }
}
