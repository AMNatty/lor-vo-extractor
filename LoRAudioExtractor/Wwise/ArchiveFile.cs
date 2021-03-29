using System;
using System.IO;

namespace LoRAudioExtractor.Wwise
{
    public abstract class ArchiveFile : IDisposable
    {
        public string Path { get; }
        
        private FileStream Stream { get; }

        public EndianBinaryReader Reader { get; }
        
        protected ArchiveFile(string path)
        {
            Console.WriteLine($"Loading archive: {path}");
            
            this.Path = path;
            this.Stream = File.Open(path, FileMode.Open, FileAccess.Read);
            this.Reader = new EndianBinaryReader(this.Stream);
        }
        
        public abstract string ArchiveTypeName { get; }
        
        public abstract uint MagicNumber { get; }
        
        public abstract string[] ArchiveFileExtensions { get; }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Console.WriteLine($"Unloading archive: {this.Path}");
            this.Reader.Dispose();
            this.Stream.Dispose();
        }
    }
}