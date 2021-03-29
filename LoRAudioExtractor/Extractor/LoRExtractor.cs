using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using AssetStudio;
using LoRAudioExtractor.Util;
using LoRAudioExtractor.Wwise;
using Object = AssetStudio.Object;

namespace LoRAudioExtractor.Extractor
{
    public static class LoRExtractor
    {
        public static List<ExtractableItem> OpenDirectory(string rootDir)
        {
            string liveDir = Path.Join(rootDir, "live");
            string patchableFilesDir = Path.Join(liveDir, "PatcherData", "PatchableFiles");
            string audioDir = Path.Join(patchableFilesDir, "Audio");
            string cardProfilesDir = Path.Join(patchableFilesDir, "AssetBundles", "assets", "assetlibrary", "cardprofiles");
            
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

            AssetsManager assetsManager = new ();
            assetsManager.LoadFiles(assetBundles);

            List<string> audioEvents = new ();
            
            foreach (var serializedFile in assetsManager.assetsFileList)
            {
                List<Object> objects = serializedFile.Objects;

                foreach (var objectObj in objects.Where(objectObj => objectObj is MonoBehaviour))
                {
                    var monoBehaviour = (MonoBehaviour) objectObj;
                    var nodes = monoBehaviour.serializedType.m_Nodes;

                    OrderedDictionary data = TypeTreeHelper.ReadType(nodes, monoBehaviour.reader);
                    TypeTraversalUtil.FindSoundEvents(audioEvents, monoBehaviour.m_Name, data);
                }
            }
            
            assetsManager.Clear();

            List<ExtractableItem> items = new ();
            
            foreach (string file in files)
            {
                if (!file.EndsWith(".pck"))
                    continue;

                string filename = Path.GetFileName(file);
                
                // A temporary way to distinguish VO banks and SFX banks
                if (!filename.Contains("_"))
                    continue;

                string locale = filename.Substring(0, filename.IndexOf("_", StringComparison.InvariantCulture));

                Dictionary<ulong, string> mappings = new ();

                foreach (var audioEvent in audioEvents)
                    mappings[FNV.Hash64($"{locale}\\{audioEvent}")] = audioEvent;
                    
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

            return items;
        }
    }
}