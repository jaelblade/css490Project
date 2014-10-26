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
        List<People> currentPeople = null;
        

        //Main
        public MainWindow()
        {
            // initialize the components (controls) of the window
            sensor = KinectSensor.GetDefault();

            InitializeComponent();

            //Initialize the FrameReducer
            frameReducer = new FrameReducer();
            
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
                currentPeopleInfo += "Tracking ID: " + p.TrackingID + " Engaged: " + p.Engauged + " EyesOpen: " + p.EyesOpen + "\n";
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

    }
}
