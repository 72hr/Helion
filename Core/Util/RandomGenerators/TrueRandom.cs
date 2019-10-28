using System;

namespace Helion.Util.RandomGenerators
{
    public class TrueRandom : IRandom
    {
        private readonly Random m_random = new Random();

        public byte NextByte() => (byte)m_random.Next(256);

        public int NextDiff() => m_random.Next(256) - m_random.Next(256);
    }
}