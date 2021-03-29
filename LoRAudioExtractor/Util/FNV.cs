using System.Text;

namespace LoRAudioExtractor.Util
{
    public class FNV
    {
        private const ulong DefaultBasis64 = 14695981039346656037;
        private const ulong DefaultPrime64 = 1099511628211;
        
        public static ulong Hash64(string path, ulong basis = DefaultBasis64, ulong prime = DefaultPrime64)
        {
            byte[] byteData = Encoding.ASCII.GetBytes(path.ToLower());
            ulong hash = basis;
            foreach (byte b in byteData)
            {
                hash *= prime;
                hash ^= b;
            }

            return hash;
        }
        
    }
}