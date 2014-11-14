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

        public Calculator()
        {
            attentionScore = 50;
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
    }
}
