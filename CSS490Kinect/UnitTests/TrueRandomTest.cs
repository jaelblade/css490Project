using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [TestClass]
    public class TrueRandomTest
    {
        private Master.TrueRandom rand;
        public TrueRandomTest()
        {
            this.rand = new Master.TrueRandom();
        }

        [TestMethod]
        public void Next()
        {
            int first = rand.Next();
            int second = rand.Next();
            Assert.AreNotEqual(first, second, "Next() should not generate two random ints that are the same.");
        }

        [TestMethod]
        public void Next_with_seed()
        {
            int first = rand.Next(57);
            int second = rand.Next(23);

            Assert.IsTrue(first < 58);  // Within bounds?

            Assert.IsTrue(second < 23); // Within bounds?

            Assert.AreNotEqual(first, second, "Next() should not generate two random ints that are the same.");
        }
    }
}
