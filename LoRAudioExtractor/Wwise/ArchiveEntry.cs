using System;
using System.IO;
using Concentus.Oggfile;
using Concentus.Structs;
using NAudio.Wave;

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
        
        public byte[] ExtractAndRead(out bool isWem)
        {
            isWem = false;
            
            try
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

                uint fileMagicNumber = reader.ReadUInt32B();

                if (fileMagicNumber == oggMagicNumber)
                {
                    reader.BaseStream.Seek(-sizeof(uint), SeekOrigin.Current);

                    byte[] data = reader.ReadBytes(chunkSize);

                    using MemoryStream inputStream = new (data);
                    using MemoryStream outputStream = new ();
                    using MemoryStream wavOutputStream = new ();

                    const int fs = 48000;
                    const int channels = 2;
                    
                    OpusDecoder decoder = new (fs, channels);
                    OpusOggReadStream oggReadStream = new (decoder, inputStream);

                    while (oggReadStream.HasNextPacket)
                    {
                        short[] packet = oggReadStream.DecodeNextPacket();

                        if (packet == null)
                            continue;
                            
                        foreach (short t in packet)
                            outputStream.Write(BitConverter.GetBytes(t));
                    }
                        
                    outputStream.Seek(0, SeekOrigin.Begin);
                    var wavStream = new RawSourceWaveStream(outputStream, new WaveFormat(fs, channels));
                    var waveProvider = wavStream.ToSampleProvider();
                    WaveFileWriter.WriteWavFileToStream(wavOutputStream, waveProvider.ToWaveProvider16());
                    return wavOutputStream.ToArray();
                }

                isWem = true;
                Console.WriteLine($"File probably not an OGG, will extract the entire RIFF: {this.Name}");
                reader.BaseStream.Position = this.Offset;
                
                return reader.ReadBytes(this.Size);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return this.Read();
            }
        }
    }
}