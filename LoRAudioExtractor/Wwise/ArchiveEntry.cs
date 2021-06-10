using System;
using System.IO;

namespace LoRAudioExtractor.Wwise
{
    public class ArchiveEntry
    {
        public ulong ID { get; }
        
        public int Offset { get; }
        
        public int Size { get;  }

        public string Name { get; }
        
        public ArchiveFile Parent { get; }
        
        public ArchiveEntry(ArchiveFile parent, ulong id, string name, uint offset, uint size)
        {
            if (size > int.MaxValue || offset > int.MaxValue)
                throw new ArgumentException("Size or offset are too big!");

            this.ID = id;
            this.Parent = parent;
            this.Offset = (int) offset;
            this.Size = (int) size;
            this.Name = name;
        }

        public byte[] Read()
        {
            var reader = this.Parent.Reader;
            reader.BaseStream.Position = this.Offset;
            return reader.ReadBytes(this.Size);
        }
        
        public byte[] ExtractAndRead()
        {
            var reader = this.Parent.Reader;
            reader.BaseStream.Position = this.Offset;
            const uint riff = 0x52494646;
            uint magicNumber = reader.ReadUInt32B();

            if (magicNumber != riff)
                throw new InvalidDataException($"Invalid magic number: {magicNumber:X}, expected {riff:X} (RIFF)!");
                
            uint fileSize = reader.ReadUInt32() + sizeof(uint) * 2;

            if (fileSize != this.Size)                
                throw new InvalidDataException($"WAV size mismatch! {fileSize} bytes, expected {this.Size} bytes!");

            const uint wave = 0x57415645;
            
            uint waveSignature = reader.ReadUInt32B();

            if (waveSignature != wave)
                throw new InvalidDataException($"Invalid WAVE magic number: {waveSignature:X}, expected {wave:X} (WAVE)!");

            bool dataChunk;

            // const uint fmtChunkID = 0x666D7D20;
            const uint dataChunkID = 0x64617461;

            int chunkSize = 0;
            
            do
            {
                reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                
                uint chunkID = reader.ReadUInt32B();
                chunkSize = reader.ReadInt32();
                
                dataChunk = chunkID == dataChunkID;
            }
            while (!dataChunk);
            
            const uint oggMagicNumber = 0x4F676753;

            if (reader.ReadUInt32() != oggMagicNumber)
            {
                Console.WriteLine($"File probably not an OGG, will extract the entire RIFF: {this.Name}");
                reader.BaseStream.Position = this.Offset;
                
                return reader.ReadBytes(this.Size);
            }
                
            return reader.ReadBytes(chunkSize);
        }
    }
}