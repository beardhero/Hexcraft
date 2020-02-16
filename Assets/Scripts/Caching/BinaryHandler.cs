using UnityEngine;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Reflection;
using System.IO;
using System.IO.Compression;
using System;

public class BinaryHandler {

/**
* Helper function to serialize and write data to disk.
*/
  public static void WriteData<T>(T data, string path) {
        //Get application specific path
        path = Application.dataPath + path;
   
        Stream stream = File.Open(path, FileMode.Create);
    BinaryFormatter bformatter = new BinaryFormatter();
    bformatter.Binder = new VersionDeserializationBinder();
    bformatter.Serialize(stream, data);
    stream.Close();
  }

    /**
     * Helper function to read and unserialize data from disk.
     */
    public static T ReadData<T>(string path) where T : new()
    {
        //Get application specific path
        //path = Application.dataPath + path;
        // Declare the hashtable reference.
        T output = new T();

        // Open the file containing the data that you want to deserialize.
        //string sfile = Path.GetFileNameWithoutExtension("currentWorld");
        //Debug.Log("file: " + sfile);
        TextAsset loading = Resources.Load("currentWorld") as TextAsset;
        MemoryStream stream = new MemoryStream(loading.bytes);
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();

            // Deserialize the hashtable from the file and 
            // assign the reference to the local variable.
            output = (T)formatter.Deserialize(stream);
        }
        catch (SerializationException e)
        {
            Console.WriteLine("Failed to deserialize. Reason: " + e.Message);
            throw;
        }
        finally
        {
            stream.Close();
        }

        //FileStream ffs = (FileStream)stream;  //new FileStream(path, FileMode.Open);

        return output;
    }

    public static void CompressWorld()
    {
        //compression test
        TextAsset t = Resources.Load("currentWorld") as TextAsset;
        byte[] bytes = t.bytes;
        string path = Application.dataPath + "/Resources/compressedWorld.gz";
        using (FileStream fileToCompress = File.Create(path))
        {
            using (DeflateStream compressionStream = new DeflateStream(fileToCompress, System.IO.Compression.CompressionLevel.Optimal))//CompressionMode.Compress))
            {
                compressionStream.Write(bytes, 0, bytes.Length);
            }
        }

    }

    public static T DecompressWorldAndRead<T>() where T : new()
    {
        T output = new T();

        byte[] decompressedBytes = new byte[0];
        using (FileStream fileToDecompress = File.Open("compressedWorld.gz", FileMode.Open))
        {
            using (DeflateStream decompressionStream = new DeflateStream(fileToDecompress, CompressionMode.Decompress))
            {
                decompressionStream.Read(decompressedBytes, 0, 0);
            }
        }

        return output;
    }


    public sealed class VersionDeserializationBinder : SerializationBinder
  {
      public override Type BindToType( string assemblyName, string typeName )
      {
          if ( !string.IsNullOrEmpty( assemblyName ) && !string.IsNullOrEmpty( typeName ) )
          {
              Type typeToDeserialize = null;

              assemblyName = Assembly.GetExecutingAssembly().FullName;

              // The following line of code returns the type.
              typeToDeserialize = Type.GetType( String.Format( "{0}, {1}", typeName, assemblyName ) );

              return typeToDeserialize;
          }

          return null;
      }
  }
}
