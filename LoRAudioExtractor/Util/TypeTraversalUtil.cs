using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace LoRAudioExtractor.Util
{
    public static class TypeTraversalUtil
    {
        private static void HandleObject(List<string> output, string name, object? input)
        {
            switch (input)
            {
                case null:
                    return;
                
                case OrderedDictionary dictionary:
                    FindSoundEvents(output, name, dictionary);
                    break;
                
                case List<object> list:
                    FindSoundEvents(output, name, list);
                    break;
                
                case KeyValuePair<object, object> keyValuePair:
                    HandleObject(output, keyValuePair.Key.ToString() ?? "", keyValuePair.Value);
                    break;
            }
        }
        
        public static void FindSoundEvents(List<string> output, string name, OrderedDictionary input)
        {
            if ((name == "SoundEvent" || name == "SpecifiedSoundEvent") && input.Contains("Events"))
            {
                object? events = input["Events"];

                if (events != null)
                {
                    
                    List<object> soundEvents = (List<object>) events;

                    foreach (object soundEventObj in soundEvents)
                    {
                        OrderedDictionary soundEvent = (OrderedDictionary) soundEventObj;

                        string? eventName = soundEvent["Event"]?.ToString();
                        
                        if (eventName != null)
                            output.Add(eventName);
                    }
                }
            }
            
            foreach (DictionaryEntry kvp in input)
                HandleObject(output, kvp.Key.ToString() ?? "", kvp.Value);
        }
        
        private static void FindSoundEvents(List<string> output, string name, List<object> input)
        {
            foreach (var item in input)
                HandleObject(output, name, item);
        }
    }
}