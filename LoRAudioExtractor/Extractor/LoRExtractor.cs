using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AssetStudio;
using LoRAudioExtractor.Util;
using LoRAudioExtractor.Wwise;
using Object = AssetStudio.Object;

namespace LoRAudioExtractor.Extractor
{
    public static class LoRExtractor
    {
        public enum AudioType
        {
            VO,
            SFX
        }
        
        public static List<ExtractableItem> OpenDirectory(string rootDir, AudioType audioType)
        {
            string liveDir = Path.Join(rootDir, "live");
            string patchableFilesDir = Path.Join(liveDir, "PatcherData", "PatchableFiles");
            string audioDir = Path.Join(patchableFilesDir, "Audio");
            string cardProfilesDir = Path.Join(patchableFilesDir, "AssetBundles");
            
            if (!Directory.Exists(rootDir) || 
                !Directory.Exists(liveDir) || 
                !Directory.Exists(patchableFilesDir) || 
                !Directory.Exists(audioDir) || 
                !Directory.Exists(cardProfilesDir))
            {
                throw new Exception("This is not a valid LoR directory");
            }
            
            string[] files = Directory.GetFiles(audioDir);

            string[] assetBundles = DirUtils.RecursivelyList(cardProfilesDir, "*.bbq");

            if (assetBundles.Length == 0)
                throw new Exception("No AssetBundles found in this directory, please make sure it is a valid LoR directory.");
            
            List<string> audioEvents = new ();
            HashSet<string> soundBanks = new ();
            
            AssetsManager assetsManager = new ();
            assetsManager.LoadFiles(assetBundles);
            
            foreach (var serializedFile in assetsManager.assetsFileList)
            {
                List<Object> objects = serializedFile.Objects;

                foreach (var objectObj in objects.Where(objectObj => objectObj is MonoBehaviour))
                {
                    var monoBehaviour = (MonoBehaviour) objectObj;
                    var nodes = monoBehaviour.serializedType.m_Type;

                    OrderedDictionary data = TypeTreeHelper.ReadType(nodes, monoBehaviour.reader);
                    TypeTraversalUtil.FindSoundEvents(audioEvents, soundBanks, monoBehaviour.m_Name, data);
                }
            }
            
            assetsManager.Clear();

            List<ExtractableItem> items = new ();

            switch (audioType)
            {
                case AudioType.VO:
                    foreach (string file in files)
                        LoadVO(file, items, audioEvents);
                    break;
                    
                case AudioType.SFX:
                    foreach (string file in files)
                        LoadSFX(file, items, audioEvents, soundBanks);
                    break;
                
                default:
                    throw new ArgumentNullException(nameof(audioType));
            }
            

            return items;
        }

        private static void LoadVO(string file, ICollection<ExtractableItem> items, IEnumerable<string> audioEvents)
        {
            if (!file.EndsWith(".pck"))
                return;

            string filename = Path.GetFileName(file);
                
            // A temporary way to distinguish VO banks and SFX banks
            if (!filename.Contains("_"))
                return;

            string locale = filename.Substring(0, filename.IndexOf("_", StringComparison.InvariantCulture));

            Dictionary<ulong, string> mappings = new ();

            foreach (var audioEvent in audioEvents)
                if (audioEvent.StartsWith("VO_"))
                    mappings[FNV.Hash64($"{locale}\\{audioEvent}".ToLowerInvariant())] = audioEvent;
                    
            try
            {
                PckFile64 package = new (file);
                
                List<ArchiveEntry> entries = package.GetEntries();
            
                foreach (ArchiveEntry archiveEntry in entries)
                {
                    string name = mappings.ContainsKey(archiveEntry.ID) ? mappings[archiveEntry.ID] : archiveEntry.Name;
                    items.Add(new ExtractableItem(archiveEntry, name));
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                throw new Exception($"An error has ocurred while opening {file}, no files will be loaded from this archive!");
            }
        }
        
        private static void LoadSFX(string file, ICollection<ExtractableItem> items, IEnumerable<string> audioEvents, IEnumerable<string> soundBanks)
        {
            if (!file.EndsWith(".pck"))
                return;

            string filename = Path.GetFileName(file);
                
            // A temporary way to distinguish VO banks and SFX banks
            if (filename.Contains("_"))
                return;

            try
            {
                PckFile32 package = new (file);
                
                List<ArchiveEntry> entries = package.GetEntries();
            
                foreach (ArchiveEntry archiveEntry in entries)
                {
                    items.Add(new ExtractableItem(archiveEntry, archiveEntry.Name));
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                throw new Exception($"An error has ocurred while opening {file}, no files will be loaded from this archive!");
            }
        }
    }
}