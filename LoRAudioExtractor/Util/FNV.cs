using System.Globalization;
using System.Text;

namespace LoRAudioExtractor.Util
{
    public static class FNV
    {
        private const ulong DefaultBasis64 = 14695981039346656037;
        private const ulong DefaultPrime64 = 1099511628211;
        
        public static ulong Hash64(string path, ulong basis = DefaultBasis64, ulong prime = DefaultPrime64)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(path);
            ulong hash = basis;
            foreach (byte b in byteData)
            {
                hash *= prime;
                hash ^= b;
            }

            return hash;
        }
        
        private const uint DefaultBasis32 = 2166136261;
        private const uint DefaultPrime32 = 16777619;
        
        private static uint Hash32B(byte[] byteData, uint basis = DefaultBasis32, uint prime = DefaultPrime32)
        {
            uint hash = basis;
            foreach (byte b in byteData)
            {
                hash *= prime;
                hash ^= b;
            }

            return hash;
        }
        
        public static uint Hash32(string path, uint basis = DefaultBasis32, uint prime = DefaultPrime32)
        {
            return Hash32B(Encoding.ASCII.GetBytes(path), basis, prime);
        }
        
        public static uint Hash30(string path, uint basis = DefaultBasis32, uint prime = DefaultPrime32)
        {
            uint hash = Hash32(path, basis, prime);
            const uint mask = 0x3fffffff;
            return (hash >> 30) ^ (hash & mask);
        }
        
        private static uint Hash30B(byte[] bytes, uint basis = DefaultBasis32, uint prime = DefaultPrime32)
        {
            uint hash = Hash32B(bytes, basis, prime);
            const uint mask = 0x3fffffff;
            return (hash >> 30) ^ (hash & mask);
        }

        public static uint HashUID(string uid, uint basis = DefaultBasis32, uint prime = DefaultPrime32)
        {
            byte[] uidBytes = new byte[16];
            string cleanUID = uid.Replace("_", "");
            int[] bytePositions = { 3, 2, 1, 0, 5, 4, 7, 6, 8, 9, 10, 11, 12, 13, 14, 15 };

            for (int i = 0; i < uidBytes.Length; i++)
            {
                const int byteChars = 2;
                int bytePos = bytePositions[i] * byteChars;
                string hexByte = cleanUID.Substring(bytePos, byteChars);
                uidBytes[i] = byte.Parse(hexByte, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return Hash32B(uidBytes, basis, prime);
        }
    }
}