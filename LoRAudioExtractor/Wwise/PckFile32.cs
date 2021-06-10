using System.Collections.Generic;
using System.IO;

namespace LoRAudioExtractor.Wwise
{
    public sealed class PckFile32 : ArchiveFile
    {
        public override string ArchiveTypeName => "Wwise 32-bit PCK Archive";

        public override uint MagicNumber => 0x414B504B;

        public override string[] ArchiveFileExtensions => new[]
        {
            ".pck"
        };

        public PckFile32(string path) : base(path)
        {
            uint magicNumber = this.Reader.ReadUInt32B();

            if (magicNumber != this.MagicNumber)
                throw new InvalidDataException($"Invalid magic number: {magicNumber:X}, expected {this.MagicNumber:X}");

            uint headerSize = this.Reader.ReadUInt32();

            uint bom = this.Reader.ReadUInt32();

            if (bom >= 0x01000000)
                throw new InvalidDataException("Big endian Wwise archives are not supported!");
        }

        public List<ArchiveEntry> GetEntries()
        {
            uint num1 = this.Reader.ReadUInt32();
            uint num2 = this.Reader.ReadUInt32();
            uint num3 = this.Reader.ReadUInt32();
            uint num4 = this.Reader.ReadUInt32();
            uint directoryCount = this.Reader.ReadUInt32();

            List<ArchiveEntry> entries = new();

            for (uint i = 0; i < directoryCount; i++)
            {
                uint dirHeaderSize = this.Reader.ReadUInt32();
                uint dirID = this.Reader.ReadUInt32();
                string dirName = this.Reader.ReadNullTerminatedString();
                
                uint itemCount1 = this.Reader.ReadUInt32();

                for (uint j = 0; j < itemCount1; j++)
                {
                    uint entryID = this.Reader.ReadUInt32();
                    uint num5 = this.Reader.ReadUInt32();
                    uint size = this.Reader.ReadUInt32();
                    uint offset = this.Reader.ReadUInt32();
                    uint directoryID = this.Reader.ReadUInt32();

                    if(directoryID != dirID)
                        throw new InvalidDataException($"Directory ID mismatch! {directoryID} != {dirID}");

                    entries.Add(new ArchiveEntry(this, entryID, $"{dirName}/{entryID}.bnk", offset, size));
                }
                
                uint itemCount2 = this.Reader.ReadUInt32();

                for (uint j = 0; j < itemCount2; j++)
                {
                    uint entryID = this.Reader.ReadUInt32();
                    uint num5 = this.Reader.ReadUInt32();
                    uint size = this.Reader.ReadUInt32();
                    uint offset = this.Reader.ReadUInt32();
                    uint directoryID = this.Reader.ReadUInt32();

                    if(directoryID != dirID)
                        throw new InvalidDataException($"Directory ID mismatch! {directoryID} != {dirID}");

                    entries.Add(new ArchiveEntry(this, entryID, $"{dirName}/{entryID}.wem", offset, size));
                }
            }

            return entries;
        }
    }
}