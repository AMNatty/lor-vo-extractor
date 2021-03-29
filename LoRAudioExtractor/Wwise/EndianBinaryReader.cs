using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LoRAudioExtractor.Wwise
{
    public static class BinaryReaderExtension
    {
        public static string ReadNullTerminatedString(this BinaryReader reader)
        {
            StringBuilder stringBuilder = new ();
            
            for (char c = reader.ReadChar(); c != '\0'; c = reader.ReadChar())
                stringBuilder.Append(c);

            return stringBuilder.ToString();
        }
        
        public static string ReadAsciiNullTerminatedString(this BinaryReader reader)
        {
            List<byte> stringBuilder = new ();
            
            for (byte c = reader.ReadByte(); c != 0; c = reader.ReadByte())
                stringBuilder.Add(c);

            return Encoding.ASCII.GetString(stringBuilder.ToArray());
        }
    }
    
    public class EndianBinaryReader : BinaryReader
    {
        public enum Endianness
        {
            Little,
            Big,
        }
        
        private Endianness DefaultEndianness { get; }

        public EndianBinaryReader(Stream input, Endianness defaultEndiannes = Endianness.Little) : base(input, Encoding.Unicode)
        {
            this.DefaultEndianness = defaultEndiannes;
        }

        public EndianBinaryReader(Stream input, bool leaveOpen, Endianness defaultEndiannes = Endianness.Little) : base(input, Encoding.Unicode, leaveOpen)
        {
            this.DefaultEndianness = defaultEndiannes;
        }
        
        public override short ReadInt16() => this.DefaultEndianness == Endianness.Little ? this.ReadInt16L() : this.ReadInt16B();
        public override ushort ReadUInt16() => this.DefaultEndianness == Endianness.Little ? this.ReadUInt16L() : this.ReadUInt16B();
        public override int ReadInt32() => this.DefaultEndianness == Endianness.Little ? this.ReadInt32L() : this.ReadInt32B();
        public override uint ReadUInt32() => this.DefaultEndianness == Endianness.Little ? this.ReadUInt32L() : this.ReadUInt32B();
        public override long ReadInt64() => this.DefaultEndianness == Endianness.Little ? this.ReadInt64L() : this.ReadInt64B();
        public override ulong ReadUInt64() => this.DefaultEndianness == Endianness.Little ? this.ReadUInt64L() : this.ReadUInt64B();
        public override float ReadSingle() => this.DefaultEndianness == Endianness.Little ? this.ReadSingleL() : this.ReadSingleB();
        public override double ReadDouble() => this.DefaultEndianness == Endianness.Little ? this.ReadDoubleL() : this.ReadDoubleB();
        
        public short ReadInt16L() => BinaryPrimitives.ReadInt16LittleEndian(this.ReadBytes(sizeof(short)));
        public ushort ReadUInt16L() => BinaryPrimitives.ReadUInt16LittleEndian(this.ReadBytes(sizeof(ushort)));
        public int ReadInt32L() => BinaryPrimitives.ReadInt32LittleEndian(this.ReadBytes(sizeof(int)));
        public uint ReadUInt32L() => BinaryPrimitives.ReadUInt32LittleEndian(this.ReadBytes(sizeof(uint)));
        public long ReadInt64L() => BinaryPrimitives.ReadInt64LittleEndian(this.ReadBytes(sizeof(long)));
        public ulong ReadUInt64L() => BinaryPrimitives.ReadUInt64LittleEndian(this.ReadBytes(sizeof(ulong)));
        public float ReadSingleL() => BinaryPrimitives.ReadSingleLittleEndian(this.ReadBytes(sizeof(float)));
        public double ReadDoubleL() => BinaryPrimitives.ReadDoubleLittleEndian(this.ReadBytes(sizeof(double)));
        
        public short ReadInt16B() => BinaryPrimitives.ReadInt16BigEndian(this.ReadBytes(sizeof(short)));
        public ushort ReadUInt16B() => BinaryPrimitives.ReadUInt16BigEndian(this.ReadBytes(sizeof(ushort)));
        public int ReadInt32B() => BinaryPrimitives.ReadInt32BigEndian(this.ReadBytes(sizeof(int)));
        public uint ReadUInt32B() => BinaryPrimitives.ReadUInt32BigEndian(this.ReadBytes(sizeof(uint)));
        public long ReadInt64B() => BinaryPrimitives.ReadInt64BigEndian(this.ReadBytes(sizeof(long)));
        public ulong ReadUInt64B() => BinaryPrimitives.ReadUInt64BigEndian(this.ReadBytes(sizeof(ulong)));
        public float ReadSingleB() => BinaryPrimitives.ReadSingleBigEndian(this.ReadBytes(sizeof(float)));
        public double ReadDoubleB() => BinaryPrimitives.ReadDoubleBigEndian(this.ReadBytes(sizeof(double)));

    }
}