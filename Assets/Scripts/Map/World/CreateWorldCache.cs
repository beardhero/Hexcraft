using UnityEngine;
using System.Collections;


public class CreateWorldCache : MonoBehaviour {

  public float scale;
  public int subdivisions;

  public static void BuildCache  (World world) 
  {
    // This is already being done in WorldManager.Initialize(!loadWorld)
    //world.PrepForCache(scale, subdivisions);

    try
    {
        BinaryHandler.WriteData<World>(world, World.cachePath);
        BinaryHandler.CompressWorld();
        Debug.Log ("World cache concluded.");
    }
    catch(System.Exception e)
    {
      Debug.LogError ("World cache fail: "+e);
    }
  }
	
}
