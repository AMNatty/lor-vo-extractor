using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace LoRAudioExtractor.Util
{
    public static class TypeTraversalUtil
    {
        private static void HandleObject(ICollection<string> output, ICollection<string> banks, string name, object? input)
        {
            switch (input)
            {
                case null:
                    return;
                
                case OrderedDictionary dictionary:
                    FindSoundEvents(output, banks, name, dictionary);
                    break;
                
                case List<object> list:
                    FindSoundEvents(output, banks, name, list);
                    break;
                
                case KeyValuePair<object, object> keyValuePair:
                    HandleObject(output, banks, keyValuePair.Key.ToString() ?? "", keyValuePair.Value);
                    break;
            }
        }
        
        public static void FindSoundEvents(ICollection<string> output, ICollection<string> banks, string name, OrderedDictionary input)
        {
            object? bankName = input["BankName"];
            string? bankNameStr = bankName?.ToString();

            if (bankNameStr != null)
                banks.Add(bankNameStr);
            
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
                HandleObject(output, banks, kvp.Key.ToString() ?? "", kvp.Value);
        }
        
        private static void FindSoundEvents(ICollection<string> output, ICollection<string> banks, string name, List<object> input)
        {
            foreach (var item in input)
                HandleObject(output, banks, name, item);
        }
    }
}