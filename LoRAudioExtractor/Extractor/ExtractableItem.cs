using LoRAudioExtractor.Wwise;

namespace LoRAudioExtractor.Extractor
{
    public class ExtractableItem
    {
        public ArchiveEntry? ArchiveEntry { get; }

        public int? Offset => this.ArchiveEntry?.Offset;

        public int? Size => this.ArchiveEntry?.Size;

        public string? InternalName => this.ArchiveEntry?.Name;
        
        public string? Name => this.assignedName ?? this.InternalName;
        
        private readonly string? assignedName;

        public ExtractableItem(ArchiveEntry archiveEntry, string? assignedName)
        {
            this.ArchiveEntry = archiveEntry;
            this.assignedName = assignedName;
        }

        public ExtractableItem()
        {
            
        }
    }
}