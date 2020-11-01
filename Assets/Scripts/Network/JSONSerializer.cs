using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class JSONSerializer {
    public static void WriteTextFile(object data, string path)
    {
        JsonSerializer serializer = new JsonSerializer();
        
        //serializer.Formatting = Formatting.Indented;  // indentation increases file size by 200%
        serializer.FloatParseHandling = FloatParseHandling.Decimal;
        
        using (StreamWriter sw = new StreamWriter(Application.dataPath+path))   // Note DO NOT use any encoding options
        using (JsonWriter writer = new JsonTextWriter(sw))
        {
            serializer.Serialize(writer, data);
        }
    }
}
