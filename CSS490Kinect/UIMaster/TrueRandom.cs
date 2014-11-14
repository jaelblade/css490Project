using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Master
{
    /// <summary>
    /// Because Random isn't random enough. 
    /// </summary>
    public class TrueRandom
    {
        private Random fakeRandom;
        private int lastInt = DateTime.Now.Millisecond;
        public TrueRandom()
        {
            lastInt = DateTime.Now.Millisecond;
            this.fakeRandom = new Random(lastInt);
        }
        public int Next()
        {
            lastInt = this.fakeRandom.Next();
            this.fakeRandom = new Random(lastInt + DateTime.Now.Millisecond);
            return lastInt;
        }
        public int Next(int i)
        {
            lastInt = this.fakeRandom.Next(i);
            this.fakeRandom = new Random(lastInt + DateTime.Now.Millisecond);
            return lastInt;
        }
    }
}
