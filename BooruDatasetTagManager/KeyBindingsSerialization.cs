using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using static BooruDatasetTagManager.KeyCodeConverter;

namespace BooruDatasetTagManager
{
    /// <summary>
    /// Converts shortcut keys to and from Keycode <-> String representations
    /// </summary>
    public class KeyCodeConverter {

        public static string StringEncode(Keys keys)
        {
            List<string> keyNames = new List<string>();

            if ((keys & Keys.Control) == Keys.Control)
            {
                keyNames.Add("Control");
            }
            if ((keys & Keys.Alt) == Keys.Alt)
            {
                keyNames.Add("Alt");
            }
            if ((keys & Keys.Shift) == Keys.Shift)
            {
                keyNames.Add("Shift");
            }

            // Mask off the modifier keys
            Keys keyCode = keys & Keys.KeyCode;
            if (keyCode != Keys.None)
            {
                keyNames.Add(keyCode.ToString());
            }

            return string.Join("+", keyNames);
        }
        public static Keys StringDecode(string keysString)
        {
            Keys result = Keys.None;

            string[] keyNames = keysString.Split('+').Select(s => s.Trim()).ToArray();
            foreach (string keyName in keyNames)
            {
                switch (keyName)
                {
                    case "Control":
                        result |= Keys.Control;
                        break;
                    case "Alt":
                        result |= Keys.Alt;
                        break;
                    case "Shift":
                        result |= Keys.Shift;
                        break;
                    default:
                        if (Enum.TryParse(keyName, out Keys parsedKey))
                        {
                            result |= parsedKey;
                        }
                        break;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Custom marhaller for the key-bindings, so we can get them pretty-printed with names
    /// instead of obscure windows Key-codes.
    /// </summary>
    public class KeyBindingJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter w, object value, JsonSerializer serializer)
        {
            var keyBindings = (KeyBindings)value;
            var jsonCompatibleDict = keyBindings.CommandKeyMap.ToDictionary(
                entry => entry.Key,
                entry => StringEncode(entry.Value)
                );
            serializer.Serialize(w, jsonCompatibleDict);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonCompatibleDict = serializer.Deserialize<Dictionary<string, string>>(reader);
            var commandKeyMap = jsonCompatibleDict.ToDictionary(
                entry => entry.Key,
                entry => StringDecode(entry.Value)
            );
            var keyBindings = new KeyBindings { CommandKeyMap = commandKeyMap };
            return keyBindings;
        }

        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(KeyBindings));
        }
    }
}
