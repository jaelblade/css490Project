using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSS490Kinect
{
    class People
    {
        public bool EyesOpen { get; private set; }
        public bool Engauged { get; private set;}

        public People (bool engauged, bool eyesOpen) {
            this.Engauged = engauged;
            this.EyesOpen = eyesOpen;
        }
    }
}
