﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace CSS490Kinect
{
    class People
    {
        public ulong TrackingID { get; private set; }
        public DetectionResult EyesOpen { get; private set; }
        public DetectionResult Engauged { get; private set;}

        public People(DetectionResult engauged, DetectionResult eyesOpen, ulong trackingID)
        {
            this.Engauged = engauged;
            this.EyesOpen = eyesOpen;
            this.TrackingID = trackingID;
        }
    }
}