using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace LoRAudioExtractor.Wwise
{
    public sealed class PckFile64 : ArchiveFile
    {
        public override string ArchiveTypeName => "Wwise 64-bit PCK Archive";

        public override uint MagicNumber => 0x414B504B;

        public override string[] ArchiveFileExtensions => new[]
        {
            ".pck"
        };

        public PckFile64(string path) : base(path)
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
            uint directoryNamesLength = this.Reader.ReadUInt32();
            uint directoryEntriesSize1 = this.Reader.ReadUInt32();
            uint directoryEntriesSize2 = this.Reader.ReadUInt32();
            uint directoryTableSize = this.Reader.ReadUInt32();
            uint directoryCount = this.Reader.ReadUInt32();

            List<ArchiveEntry> entries = new();

            for (uint i = 0; i < directoryCount; i++)
            {
                uint dirOffset = this.Reader.ReadUInt32();
                uint dirID = this.Reader.ReadUInt32();
                string dirName = this.Reader.ReadNullTerminatedString();
                this.Reader.BaseStream.Seek(dirOffset - sizeof(uint), SeekOrigin.Current);
                uint itemCount = this.Reader.ReadUInt32();

                for (uint j = 0; j < itemCount; j++)
                {
                    ulong entryID = this.Reader.ReadUInt64();
                    uint num4 = this.Reader.ReadUInt32();
                    uint size = this.Reader.ReadUInt32();
                    uint offset = this.Reader.ReadUInt32();
                    uint directoryID = this.Reader.ReadUInt32();

                    if(directoryID != dirID)
                        throw new InvalidDataException($"Directory ID mismatch! {directoryID} != {dirID}");

                    entries.Add(new ArchiveEntry(this, entryID, $"{dirName}/{entryID}", offset, size));
                }
            }

            return entries;
        }
    }
}