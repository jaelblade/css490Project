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
    class Calculator
    {
        public double attentionScore { get; private set; }
        KinectSensor sensor = null;
        CoordinateMapper coordinateMapper = null;

        public Calculator()
        {
            attentionScore = 50;
            sensor = KinectSensor.GetDefault();
            coordinateMapper = sensor.CoordinateMapper;
        }

        public double calculateAttentionScore(List<People> currentPeople)
        {
            if (currentPeople.Count <= 0)
            {
                return attentionScore;
            }
            //Count the total number of frames
            int peopleThisFrame = 0;
            int peoplePayingAttention = 0;
            int peopleWithEyesOpen = 0;
            int attentionScoreThisFrame = 0;

            // determine how many people there are, and how many are engaged/eyes open. 
            foreach (People p in currentPeople)
            {
                peopleThisFrame++;
                if( p.Engauged == DetectionResult.Yes || p.Engauged == DetectionResult.Maybe)
                    peoplePayingAttention++;
                if( p.EyesOpen == DetectionResult.Yes)
                    peopleWithEyesOpen++;
            }
            
            // calculate score for this frame (will be between 0 and 100)
            attentionScoreThisFrame = ((peoplePayingAttention / peopleThisFrame) + (peopleWithEyesOpen / peopleThisFrame)) * 50;
            // factor in this frame into previous score
            attentionScore = attentionScore * 0.8 + attentionScoreThisFrame * 0.2;

            return attentionScore;
        }

        //Calculate if the gaze directions converge
        
        private bool gazeConverges(List<People> currentPeople)
        {
            //If there are no people, there is no gaze and therefore false
            if (currentPeople.Count <= 0)
            {
                return false;
            }

            //If only one person, then gaze will always converge
            if (currentPeople.Count == 1)
            {
                return true;
            }

            //Create array of color points to convery the camera points into
            DepthSpacePoint[] currentDepthPoints = new DepthSpacePoint[currentPeople.Count];
            DepthSpacePoint[] vectoredDepthPoints = new DepthSpacePoint[currentPeople.Count];
            //convert all camera space points to Depth Space points
            for (int i = 0; i < currentPeople.Count; i++)
            {
                CameraSpacePoint vectoredPoint = currentPeople[i].HeadJoint;
                vectoredPoint.X -= currentPeople[i].FaceOrientaion.Y / currentPeople[i].FaceOrientaion.W;
                vectoredPoint.Y -= currentPeople[i].FaceOrientaion.X / currentPeople[i].FaceOrientaion.W;
                currentDepthPoints[i] = coordinateMapper.MapCameraPointToDepthSpace(currentPeople[i].HeadJoint);
                vectoredDepthPoints[i] = coordinateMapper.MapCameraPointToDepthSpace(vectoredPoint);
            }

            //calculate midpoint of all points
            //Take all points and calculate the lowest and highest X/Y values and find the midpoint
            float depthHighX = float.MinValue; //Start high values at min value for comparisons
            float depthLowX = float.MaxValue; //Start low values at the highest value for comparisons
            float depthHighY = float.MinValue;
            float depthLowY = float.MaxValue;

            foreach (DepthSpacePoint dsp in currentDepthPoints)
            {
                //Skip checking if both points are 0.0 meaning they didn't get tracked
                if (dsp.X == 0.0f && dsp.Y == 0.0f)
                {
                    continue;
                }
                //If the current X is lower than the known lowest X, current X is the new lowest X
                if (dsp.X < depthLowX)
                {
                    depthLowX = dsp.X;
                }
                //If the current X is higher than the known highest X, current X is the new highest X
                if (dsp.X > depthHighX)
                {
                    depthHighX = dsp.X;
                }
                //If the current Y is lower than the known lowest Y, current Y is the new lowest Y
                if (dsp.Y < depthLowY)
                {
                    depthLowY = dsp.Y;
                }
                //If the current Y is higher than the known highest Y, current Y is the new highest Y
                if (dsp.Y > depthHighY)
                {
                    depthHighY = dsp.Y;
                }
            }

            //Mid point is the addition of the high and low values divided by 2.0f due to float numbers
            //Get current midpoint
            DepthSpacePoint currentMidPoint = new DepthSpacePoint();
            currentMidPoint.X = (depthHighX + depthLowX) / 2.0f;
            currentMidPoint.Y = (depthHighY + depthLowY) / 2.0f;

            //Reset High and Low variables back to min amd max values
            depthHighX = float.MinValue; //Start high values at min value for comparisons
            depthLowX = float.MaxValue; //Start low values at the highest value for comparisons
            depthHighY = float.MinValue;
            depthLowY = float.MaxValue;

            foreach (DepthSpacePoint dsp in vectoredDepthPoints)
            {
                //Skip checking if both points are 0.0 meaning they didn't get tracked
                if (dsp.X == 0.0f && dsp.Y == 0.0f)
                {
                    continue;
                }
                //If the current X is lower than the known lowest X, current X is the new lowest X
                if (dsp.X < depthLowX)
                {
                    depthLowX = dsp.X;
                }
                //If the current X is higher than the known highest X, current X is the new highest X
                if (dsp.X > depthHighX)
                {
                    depthHighX = dsp.X;
                }
                //If the current Y is lower than the known lowest Y, current Y is the new lowest Y
                if (dsp.Y < depthLowY)
                {
                    depthLowY = dsp.Y;
                }
                //If the current Y is higher than the known highest Y, current Y is the new highest Y
                if (dsp.Y > depthHighY)
                {
                    depthHighY = dsp.Y;
                }
            }

            DepthSpacePoint vectoredMidPoint = new DepthSpacePoint();
            vectoredMidPoint.X = (depthHighX + depthLowX) / 2.0f;
            vectoredMidPoint.Y = (depthHighY + depthLowY) / 2.0f;

            //Get average of distances of the currentpoints
            Double currentAverages = 0.0;
            Double validPoints = 0.0;
            foreach (DepthSpacePoint dsp in currentDepthPoints)
            {
                if (dsp.X == 0.0f && dsp.Y == 0.0f)
                {
                    continue;
                }
                validPoints++;
                // Length between points is SQRT((X2-X1)^2 + (Y2-Y1)^2)
                Double xDifference = dsp.X - currentMidPoint.X; // Get X2-X1
                Double yDifference = dsp.Y - currentMidPoint.Y; // Get Y2-Y1
                xDifference = Math.Pow(xDifference, 2.0);   //Square the differences
                yDifference = Math.Pow(yDifference, 2.0);   // Square the differencs
                currentAverages += Math.Sqrt(xDifference + yDifference); // Square Root to get the distance between points
            }
            currentAverages = currentAverages / validPoints;

            Double vectoredAverages = 0.0;
            foreach (DepthSpacePoint dsp in vectoredDepthPoints)
            {
                Double xDifference = dsp.X - vectoredMidPoint.X; // Get X2-X1
                Double yDifference = dsp.Y - vectoredMidPoint.Y; // Get Y2-Y1
                xDifference = Math.Pow(xDifference, 2.0);   //Square the differences
                yDifference = Math.Pow(yDifference, 2.0);   // Square the differencs
                vectoredAverages += Math.Sqrt(xDifference + yDifference); // Square Root to get the distance between points
            }
            vectoredAverages = vectoredAverages / validPoints;

            return vectoredAverages <= currentAverages; ;
        }
    }
}
