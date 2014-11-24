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
        public const int BODYCOUNT = 6;

        private bool needFrame;
        private TimeSpan lastFrameTime;

        public int BodyFramesProcessed { get; private set; }
        public int FaceFramesProcessed { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        //Public Data access
        public int CurrentBodyCount { get; private set; }

        public int CurrentFaceCount { get; private set; }

        //Private class variables
        private List<People> currentPeople;

        //Kinect Sensor Variable
        private KinectSensor sensor = null;

        //Multisource Frame Reader
        private MultiSourceFrameReader msFrameReader = null;

        //Body Frame Variables

        //Array of bodies
        private Body[] bodies = null;


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
            | FaceFrameFeatures.PointsInColorSpace
            | FaceFrameFeatures.FaceEngagement
            | FaceFrameFeatures.LeftEyeClosed
            | FaceFrameFeatures.RightEyeClosed;

        public FrameReducer()
        {
            //constructer code
            InitKinect();

            BodyFramesProcessed = 0;
            FaceFramesProcessed = 0;
            currentPeople = new List<People>();
            currentBodiesTracked = 0;
            needFrame = false;

            //After Kinect is intiialized, the Face Reader Events need to be monitored
            for (int i = 0; i < BODYCOUNT; i++)
            {
                if (faceFrameReaders[i] != null)
                {
                    faceFrameReaders[i].FrameArrived += Reader_FaceFrameArrived;
                }
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
            InitKinectMultiFrame();

            InitKinectFace(); //Face information
        }

        private void InitKinectMultiFrame()
        {
            msFrameReader = sensor.OpenMultiSourceFrameReader(
                                        FrameSourceTypes.Color
                                        | FrameSourceTypes.Depth
                                        | FrameSourceTypes.Body);

            msFrameReader.MultiSourceFrameArrived += msFrameReader_MultiSourceFrameArrived;

            //Set Body data
            bodies = new Body[BODYCOUNT];

        }

        void msFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (needFrame)
            {
                //get the multisource frame so we can gather other frames from it
                var msFrame = e.FrameReference.AcquireFrame();

                //Check the body frame
                using (var bodyFrame = msFrame.BodyFrameReference.AcquireFrame())
                {
                    if (bodyFrame != null)
                    {
                        BodyFramesProcessed++;
                        //Refresh the body array with body data from frame
                        bodyFrame.GetAndRefreshBodyData(bodies);
                        lastFrameTime = bodyFrame.RelativeTime;
                        int frameBodyCount = 0;
                        //iterate through each face source
                        for (int i = 0; i < BODYCOUNT; i++)
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
                    UpdateBodyStatus();
                }
            }
        }


        //Initialize the Face Frame Features
        private void InitKinectFace()
        {
            //Initialize array for FaceFrames
            faceFrameSources = new FaceFrameSource[BODYCOUNT];
            faceFrameReaders = new FaceFrameReader[BODYCOUNT];
            faceFrameResults = new FaceFrameResult[BODYCOUNT];

            //Link the number of FaceFrame sources to the body count
            for (int i = 0; i < BODYCOUNT; i++)
            {
                faceFrameSources[i] = new FaceFrameSource(sensor, 0, faceFrameFeatures);
                faceFrameReaders[i] = faceFrameSources[i].OpenReader();
            }


        }

        //Get Body Index of the Face Frame Source
        private int GetFaceSourceIndex(FaceFrameSource faceFrameSource)
        {
            int index = -1;

            for (int i = 0; i < BODYCOUNT; i++)
            {
                if (faceFrameSources[i] == faceFrameSource)
                {
                    index = i;
                    break;
                }
            }

            return index;
        }


        //Function to update bound information on the UI
        private void UpdateBodyStatus()
        {
            //Body and face tracking status can be different, must track both individually
            int currentBodies = 0;
            int facesTracked = 0;
            for (int i = 0; i < BODYCOUNT; i++)
            {
                if (bodies[i] != null)
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
            }
            CurrentBodyCount = currentBodies;
            CurrentFaceCount = facesTracked;
        }

        //Face Frame Arrival
        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            if (needFrame || FaceFramesProcessed%10 != 0)
            {
                using (var faceFrame = e.FrameReference.AcquireFrame())
                {
                    if (faceFrame != null)
                    {
                        FaceFramesProcessed++;
                        //Get the index in the body source to store results in a FaceFrameResult array
                        int index = GetFaceSourceIndex(faceFrame.FaceFrameSource);
                        if (faceFrame.IsTrackingIdValid)
                        {
                            //Store FaceFrame into a results array for faster processing
                            faceFrameResults[index] = faceFrame.FaceFrameResult;

                            //Determine if a trackingID already exists within the list of people
                            //If the person exists, delete from the list in order to update the current information
                            int indexOfID = indexOfTrackingID(faceFrameResults[index].TrackingId);
                            if (indexOfID > -1)
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
                            Vector4 faceOrientation = new Vector4();

                            //Get the bodies index of the face if it exists
                            int bodyIndex = -1;
                            for (int i = 0; i < BODYCOUNT; i++)
                            {
                                if (bodies[i].TrackingId == faceFrame.TrackingId)
                                {
                                    bodyIndex = i;
                                    break;
                                }
                            }

                            CameraSpacePoint headInCameraSpace = new CameraSpacePoint();

                            if (bodyIndex >= 0)
                            {
                                Joint headJoint;
                                bodies[bodyIndex].Joints.TryGetValue(JointType.Head, out headJoint);

                                if (headJoint != null)
                                {
                                    headInCameraSpace = headJoint.Position;
                                }
                            }

                            

                            //Query for the specific facial informations
                            faceProperties.TryGetValue(FaceProperty.Engaged, out engaged);
                            faceProperties.TryGetValue(FaceProperty.LeftEyeClosed, out leftEyeClosed);
                            faceProperties.TryGetValue(FaceProperty.RightEyeClosed, out rightEyeClosed);
                            
                            if (faceFrameResults[index].FaceRotationQuaternion != null)
                            {
                                faceOrientation = faceFrameResults[index].FaceRotationQuaternion;
                            }

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
                            currentPeople.Add(new People(engaged, eyesOpen, faceFrameResults[index].TrackingId, faceOrientation, headInCameraSpace));
                        }
                        else
                        {
                            //If there is no valid tracking ID, clear the results for the specific body index
                            faceFrameResults[index] = null;
                        }
                    }
                }
            }
            if (FaceFramesProcessed % 10 == 0)
            {
                needFrame = false;
            }
        }

        //Return the current list of people
        public List<People> GetPeople()
        {
            //Set flag for need frame
            needFrame = true;
            int retries = 0;
            //Wait 100 ms for a frame to arrive from Kinect
            System.Threading.Thread.Sleep(100);
            //Get the current time
            //Check if the last frame is less than a second old
            while (lastFrameTime.TotalSeconds > 1.0)
            {
                //Wait 50 milliseconds for another frame
                System.Threading.Thread.Sleep(50);
                //Get current time to replace
                //Increment retry count
                retries++;

                //Break from loop after 10 retries and return any list of people
                if (retries >= 10)
                {
                    break;
                }
            }
            //Set flag to false to stop processing frames
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
                    break;
                }
            }

            return index;
        }
    }
}
