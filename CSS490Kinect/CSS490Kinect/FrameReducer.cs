using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.ComponentModel;

namespace CSS490Kinect
{
    class FrameReducer: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        //Public Data access
        public int CurrentBodyCount { get; private set; }

        public int CurrentFaceCount { get; private set; }

        //Private class variables
        private List<People> currentPeople;

        //Kinect Sensor Variable
        private KinectSensor sensor = null;

        //Body Frame Variables

        //BodyFrameReder
        private BodyFrameReader bodyFrameReader = null;

        //Array of bodies
        private Body[] bodies = null;

        //Number of tracked bodies
        private int bodyCount;

        private int currentBodiesTracked;

        //FaceFrame Variables

        //Face Sources
        private FaceFrameSource[] faceFrameSources = null;

        //Face Reader
        private FaceFrameReader[] faceFrameReaders = null;

        //Face Results
        private FaceFrameResult[] faceFrameResults = null;

        // specify the required face frame results
        FaceFrameFeatures faceFrameFeatures =
           FaceFrameFeatures.RotationOrientation
            | FaceFrameFeatures.FaceEngagement
            | FaceFrameFeatures.LeftEyeClosed
            | FaceFrameFeatures.RightEyeClosed;

        public FrameReducer()
        {
            //constructer code
            InitKinect();

            currentPeople = new List<People>();
            currentBodiesTracked = 0;

            //After Kinect is intiialized, the Face Reader Events need to be monitored
            for (int i = 0; i < bodyCount; i++)
            {
                if (faceFrameReaders[i] != null)
                {
                    faceFrameReaders[i].FrameArrived += Reader_FaceFrameArrived;
                }
            }

            if (bodyFrameReader != null)
            {
                bodyFrameReader.FrameArrived += bodyFR_FrameArrived;
            }
        }

        public void InitKinect()
        {
            //Get first active kinect Sensor
            sensor = KinectSensor.GetDefault();

            //If no sensor, do nothing
            if (sensor == null)
            {
                return;
            }

            //Open connection to sensor
            sensor.Open();

            //Initialize specific kinect Features
            InitKinectBody(); //body Information

            InitKinectFace(); //Face information
        }

        //Initialize the Body Frame Features
        private void InitKinectBody()
        {
            //get the body frame reader
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();

            //Handle for Body Frame Arrival
            bodyFrameReader.FrameArrived += bodyFR_FrameArrived;


            //Set maximum body count
            bodyCount = sensor.BodyFrameSource.BodyCount;

            //allocate body array
            bodies = new Body[bodyCount];

        }

        //Initialize the Face Frame Features
        private void InitKinectFace()
        {
            //Initialize array for FaceFrames
            faceFrameSources = new FaceFrameSource[bodyCount];
            faceFrameReaders = new FaceFrameReader[bodyCount];
            faceFrameResults = new FaceFrameResult[bodyCount];

            //Link the number of FaceFrame sources to the body count
            for (int i = 0; i < bodyCount; i++)
            {
                faceFrameSources[i] = new FaceFrameSource(sensor, 0, faceFrameFeatures);
                faceFrameReaders[i] = faceFrameSources[i].OpenReader();
            }


        }

        //Get Body Index of the Face Frame Source
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < bodyCount; i++)
            {
                if (faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }

        //Body frame arrivalevent
        void bodyFR_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var bodyFrame = e.FrameReference.AcquireFrame())
            {
                //Check if frame is valid
                if (bodyFrame != null)
                {
                    //Refresh the body array with body data from frame
                    bodyFrame.GetAndRefreshBodyData(bodies);
                    int frameBodyCount = 0;
                    //iterate through each face source
                    for (int i = 0; i < bodyCount; i++)
                    {
                        //Check if body is tracked
                        if (bodies[i].IsTracked)
                        {
                            frameBodyCount++;
                            //Update face frame source with tracked ID
                            faceFrameSources[i].TrackingId = bodies[i].TrackingId;
                        }
                    }
                    //If the number of bodies tracked changes, then clear the current list of people to refresh information
                    if (currentBodiesTracked != frameBodyCount)
                    {
                        currentPeople.Clear();
                        currentBodiesTracked = frameBodyCount;
                    }
                }
            }
            //Update UI Information
            UpdateBodyStatus();
        }

        //Function to update bound information on the UI
        private void UpdateBodyStatus()
        {
            //Body and face tracking status can be different, must track both individually
            int currentBodies = 0;
            int facesTracked = 0;
            for (int i = 0; i < bodyCount; i++)
            {
                if (bodies[i].IsTracked)
                {
                    currentBodies++;
                }
                if (faceFrameSources[i].IsTrackingIdValid)
                {
                    facesTracked++;
                }
            }
            CurrentBodyCount = currentBodies;
            CurrentFaceCount = facesTracked;
        }

        //Face Frame Arrival
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    //Get the index in the body source to store results in a FaceFrameResult array
                    int index = GetFaceSourceIndex(faceFrame.FaceFrameSource);
                    if (faceFrame.IsTrackingIdValid)
                    {
                        //Store FaceFrame into a results array for faster processing
                        faceFrameResults[index] = faceFrame.FaceFrameResult;

                        //Determine if a trackingID already exists within the list of people
                        //If the person exists, delete from the list in order to update the current information
                        int indexOfID = indexOfTrackingID(faceFrameResults[index].TrackingId);
                        if ( indexOfID > -1)
                        {
                            currentPeople.RemoveAt(indexOfID);
                        }

                        //Add the new or updated person into the people list
                        //Get the dictionary of Faceproperties
                        IReadOnlyDictionary<FaceProperty, DetectionResult> faceProperties = faceFrameResults[index].FaceProperties;

                        //DetectionResult variables must be declared before querying information with TryGetValue
                        DetectionResult engaged;
                        DetectionResult leftEyeClosed;
                        DetectionResult rightEyeClosed;
                        //Consolidte information for both eyes
                        DetectionResult eyesOpen;

                        //Query for the specific facial informations
                        faceProperties.TryGetValue(FaceProperty.Engaged, out engaged);
                        faceProperties.TryGetValue(FaceProperty.LeftEyeClosed, out leftEyeClosed);
                        faceProperties.TryGetValue(FaceProperty.RightEyeClosed, out rightEyeClosed);

                        //Eyes are open only if both eyes aren't closed.
                        if (leftEyeClosed == DetectionResult.No && rightEyeClosed == DetectionResult.No)
                        {
                            eyesOpen = DetectionResult.Yes;
                        }
                        else
                        {
                            eyesOpen = DetectionResult.No;
                        }

                        //Add the result into the People class and add to the Current People List
                        currentPeople.Add(new People(engaged, eyesOpen, faceFrameResults[index].TrackingId));
                    }
                    else
                    {
                        //If there is no valid tracking ID, clear the results for the specific body index
                        faceFrameResults[index] = null;
                    }
                }
            }
        }

        //Return the current list of people
        public List<People> GetPeople()
        {
            return currentPeople;
        }

        //Return the index location of the TrackingID if it exists within the current People List
        //Returns -1 if TrackingID doesn't exist
        private int indexOfTrackingID(ulong trackingID)
        {
            int index = -1;

            for (int i = 0; i < currentPeople.Count; i++)
            {
                if (currentPeople.ElementAt(i).TrackingID == trackingID)
                {
                    index = i;
                }
            }

            return index;
        }
    }
}
