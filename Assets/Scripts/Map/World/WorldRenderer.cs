using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine.Jobs;
using Unity.Burst;
using Unity.Collections;
using System.Linq;
using System.Threading.Tasks;
using System;

public class WorldRenderer : MonoBehaviour
{
  public GameObject worldPrefab;
  public float tileWidth;
  public float tileHeight;
  public int tileCountW;
  public int tileCountH;
  public bool hexagonal; // false for triangle uvs
  public List<List<SphereTile>> hPlates;
  bool controlx;
  bool controly;
  bool controlz;
  public static Vector2 uv0,uv1,uv2,uv3,uv4,uv5,uv6;
  public static float uvTileWidth;
  public static float uvTileHeight;

  PolySphere activePolySphere;
 

// ========= Threaded gen ==============
  public IEnumerator ThreadedHexPlates(World world, TileSet tileSet, Action<List<GameObject>> callback){
    List<GameObject> output = new List<GameObject>();

    Vector3 origin = Vector3.zero;
    float texHeight = 2048;
    float texWidth = 2048;
    uvTileWidth = 514/texWidth;    // These values were taken from ruler measurements in photoshop
    uvTileHeight = 514/texHeight;   // Note that this is the tiling offset. UVs don't use the full height, hex in atlas is only 512x442
    float actualWidth = 510, actualHeight = 438;
    Vector2[] uvs = new Vector2[8];
    uvs[0] = new Vector2(actualWidth/2/texWidth, actualHeight/2/texHeight);  // center
    uvs[1] = new Vector2 (0, actualHeight/2/texHeight);  // left middle
    uvs[2] = new Vector2 (128f/texWidth, actualHeight/texHeight);  // upper left
    uvs[3] = new Vector2 (384f/texWidth, actualHeight/texHeight);  // upper right
    uvs[4] = new Vector2 (actualWidth/texWidth, actualHeight/2/texHeight);   // right middle
    uvs[5] = new Vector2 (384f/texWidth, 0);   // Lower right
    uvs[6] = new Vector2 (128f/texWidth, 0);   // Lower left
    uvs[7] = new Vector2 (13*uvTileWidth, 1*uvTileHeight);  // "Dark tex" @TODO: proper side texturing
    NativeArray<Vector2> uvOs = new NativeArray<Vector2>(uvs, Allocator.TempJob);
    int[] atlasCoords = WorldManager.instance.regularTileSet.GetUVsFlattened();
 
    List<JobHandle> plateJobHandles = new List<JobHandle>();
    List<PlateRenderJob> plateJobs = new List<PlateRenderJob>();

    for (int i = 0; i < world.numberOfPlates; i++)
    {
      int count = world.platesData[i].types.Length;
      int typeCount = System.Enum.GetNames(typeof(TileType)).Length;
      NativeArray<int> flatUvCoords = new NativeArray<int>(count*2, Allocator.TempJob);


      for (int x=0; x<count; x++){
        flatUvCoords[x] = atlasCoords[world.platesData[i].types[x]];    // X
        flatUvCoords[count+x] = atlasCoords[world.platesData[i].types[x] + typeCount];    // Y
      }
      
      PlateRenderJob job = new PlateRenderJob(){
        worldScale = 1 / 16.0f,    // Estimated radius of the world is 16 blocks
        uvWidth = uvTileWidth,
        uvHeight = uvTileHeight,
        nativeData = world.platesData[i],
        uvOffsets = uvOs,   // This array persists between all jobs and is disposed at the end
        uvCoords = flatUvCoords,
        vertices = new NativeList<Vector3>(Allocator.TempJob),
        normals = new NativeList<Vector3>(Allocator.TempJob),
        uvs = new NativeList<Vector2>(Allocator.TempJob),
        triangles = new NativeList<int>(Allocator.TempJob)
      };
      plateJobs.Add(job);
      plateJobHandles.Add(job.Schedule());
    }

    // Jobs now scheduled, wait a frame and go
    bool[] completeness = new bool[plateJobs.Count];
    // Jobs not complete
    while(!completeness.All(x => x)){

      yield return null;    // Give at least one full frame before starting to check

      for (int i=0; i<plateJobs.Count; i++){
        if (completeness[i])  continue;   // Already finished this one
        if (plateJobHandles[i].IsCompleted){
          completeness[i] = true;
          plateJobHandles[i].Complete();

          GameObject plate = GameObject.Instantiate(worldPrefab, origin, Quaternion.identity);
          Mesh m = new Mesh();
          m.vertices = plateJobs[i].vertices.ToArray();
          m.triangles = plateJobs[i].triangles.ToArray();
          m.normals = plateJobs[i].normals.ToArray();
          m.uv = plateJobs[i].uvs.ToArray();
          plate.GetComponent<MeshCollider>().sharedMesh = m;
          plate.GetComponent<MeshFilter>().sharedMesh = m;
          output.Add(plate);

          // Dispose/cleanup
          plateJobs[i].Dispose();
        }
      }
    }

    uvOs.Dispose();   // only larger-scope native array
    callback(output);
  }

  private struct PlateRenderJob : IJob
  {
    [ReadOnly] public float worldScale, uvWidth, uvHeight;
    [ReadOnly] public World.NativePlateData nativeData;
    [ReadOnly] public NativeArray<Vector2> uvOffsets;
    [ReadOnly] public NativeArray<int> uvCoords;
    public NativeList<Vector3> vertices, normals;
    public NativeList<Vector2> uvs;
    public NativeList<int> triangles;
    public void Execute ()
    {
      // Huge assumption here that all the arrays have the same length
      int count = nativeData.heights.Length;  

      // Add the origin point
      vertices.Add(Vector3.zero);
      uvs.Add(uvOffsets[7]);    // All fukt up
      normals.Add(Vector3.zero);
      
      for (int i=0; i<count; i++){
        float height = nativeData.heights[i] * worldScale;
        Vector2 uvAdd = new Vector2(uvCoords[i]*uvWidth, uvCoords[count+i]*uvHeight);

        // Center of hexagon
        int centerIndex = vertices.Length;
        // Triangle 1
        vertices.Add(nativeData.centers[i]*height);
        normals.Add(nativeData.centers[i]);
        uvs.Add (uvOffsets[0] + uvAdd);

        vertices.Add(nativeData.v1s[i]*height);
        normals.Add(nativeData.v1s[i]);
        uvs.Add(uvOffsets[1] + uvAdd);

        vertices.Add(nativeData.v2s[i]*height);
        normals.Add(nativeData.v2s[i]);
        uvs.Add(uvOffsets[2] + uvAdd);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 1);

        // T2
        vertices.Add(nativeData.v3s[i]*height);
        normals.Add(nativeData.v3s[i]);
        uvs.Add(uvOffsets[3] + uvAdd);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 1);

        // T3
        vertices.Add(nativeData.v4s[i]*height);
        normals.Add(nativeData.v4s[i]);
        uvs.Add(uvOffsets[4] + uvAdd);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 1);

        // T4
        vertices.Add(nativeData.v5s[i]*height);
        normals.Add(nativeData.v5s[i]);
        uvs.Add(uvOffsets[5] + uvAdd);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 1);

        // T5
        vertices.Add(nativeData.v6s[i]*height);
        normals.Add(nativeData.v6s[i]);
        uvs.Add(uvOffsets[6] + uvAdd);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 1);

        // T6
        triangles.Add(centerIndex);
        triangles.Add(vertices.Length - 1);   //1
        triangles.Add(vertices.Length - 6);   //6

        // Add six vertices with new UVs
        vertices.Add(nativeData.v1s[i]*height);
        normals.Add(nativeData.sideNorm1[i]);
        uvs.Add(uvOffsets[7] + uvOffsets[1]);   // These uvs are all wrong

        vertices.Add(nativeData.v2s[i]*height);
        normals.Add(nativeData.sideNorm2[i]);
        uvs.Add(uvOffsets[7] + uvOffsets[2]);

        vertices.Add(nativeData.v3s[i]*height);
        normals.Add(nativeData.sideNorm3[i]);
        uvs.Add(uvOffsets[7] + uvOffsets[3]);

        vertices.Add(nativeData.v4s[i]*height);
        normals.Add(nativeData.sideNorm4[i]);
        uvs.Add(uvOffsets[7] + uvOffsets[4]);

        vertices.Add(nativeData.v5s[i]*height);
        normals.Add(nativeData.sideNorm5[i]);
        uvs.Add(uvOffsets[7] + uvOffsets[5]);

        vertices.Add(nativeData.v6s[i]*height);
        normals.Add(nativeData.sideNorm6[i]);
        uvs.Add(uvOffsets[7] + uvOffsets[6]);

        // Side 1
        triangles.Add(0);   // Zero is the origin index
        triangles.Add(vertices.Length - 1);
        triangles.Add(vertices.Length - 2);

        // Side 2
        triangles.Add(0);
        triangles.Add(vertices.Length - 2);
        triangles.Add(vertices.Length - 3);

        // Side 3
        triangles.Add(0);
        triangles.Add(vertices.Length - 3);
        triangles.Add(vertices.Length - 4);

        // Side 4
        triangles.Add(0);
        triangles.Add(vertices.Length - 4);
        triangles.Add(vertices.Length - 5);

        // Side 5
        triangles.Add(0);
        triangles.Add(vertices.Length - 5);
        triangles.Add(vertices.Length - 6);
        
        // Side 6 extra vertex
        triangles.Add(0);
        triangles.Add(vertices.Length - 6);
        triangles.Add(vertices.Length - 1);
      }
    }
    public void Dispose(){
      nativeData.Dispose();
      //uvOffsets.Dispose();    // Save offsets they are used by each job in batch
      uvCoords.Dispose();
      vertices.Dispose();
      normals.Dispose();
      uvs.Dispose();
      triangles.Dispose();
    }
  }
  // ========== /Threaded Gen ========

  // Deprecated
  public List<GameObject> HexPlates(World world, TileSet tileSet)
  {
    List<GameObject> output = new List<GameObject>();
    

    //Create a mesh for each plate and put it in the list of outputs
    for (int i = 0; i < world.numberOfPlates; i++)
    {
      output.Add(HexPlate(world, tileSet, i));
    }
    return output;
  }
  // Deprecated
  public GameObject HexPlate(World world, TileSet tileSet, int i)
  {
    GameObject output = (GameObject)Instantiate(worldPrefab, Vector3.zero, Quaternion.identity);
    output.layer = 0;
    MeshFilter myFilter = output.GetComponent<MeshFilter>();
    MeshCollider myCollider = output.GetComponent<MeshCollider>();

    SerializableVector3 origin = world.origin;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();

    //Switch between UV Modes
    if (hexagonal) //Hexagonal uvs
    {
      //Copypasta from worldrenderer
      float texHeight = 8192f;
      float texWidth = 8192f;
      uvTileWidth = 514/texWidth;    // These values were taken from ruler measurements in photoshop
      uvTileHeight = 514/texHeight;   // Note that this is the tiling offset. UVs don't use the full height, hex in atlas is only 512x442
      float actualWidth = 510, actualHeight = 438;
      //384, 128
      uv0 = new Vector2(actualWidth/2/texWidth, actualHeight/2/texHeight);  // center
			uv1 = new Vector2 (0, actualHeight/2/texHeight);  // left middle
			uv2 = new Vector2 (128f/texWidth, actualHeight/texHeight);  // upper left
			uv3 = new Vector2 (384f/texWidth, actualHeight/texHeight);  // upper right
			uv4 = new Vector2 (actualWidth/texWidth, actualHeight/2/texHeight);   // right middle
			uv5 = new Vector2 (384f/texWidth, 0);   // Lower right
			uv6 = new Vector2 (128f/texWidth, 0);   // Lower left
      
      int[] darkTex = {13,0};   // Coordinates in the atlas
      Vector2 darkBorder = new Vector2(darkTex[0]*uvTileWidth, darkTex[1]*uvTileHeight);  // A black point off the map

      // Origin point, every tile unfortunately repeats origin (@TODO and one vertex) for uv purposes
      int originIndex = vertices.Count;
      vertices.Add(origin);
      uvs.Add(darkBorder+uv0);
      normals.Add(Vector3.zero);
      
      float worldScale = 1 / 16.0f;    // Estimated radius of the world is 16 blocks

      foreach (HexTile ht in world.tiles)
			{
				if (ht.plate == i)
				{
          IntCoord uvCoord = WorldManager.instance.regularTileSet.GetUVForType(ht.type);
          Vector2 uvOffset = new Vector2(uvCoord.x * uvTileWidth, uvCoord.y * uvTileHeight);
					
          float height = 1 + (ht.height * worldScale);

					// Center of hexagon
					int centerIndex = vertices.Count;
					ht.hexagon.uv0i = uvs.Count;  // Are these used for tile type switching?)
					// Triangle 1
					vertices.Add(ht.hexagon.center*height);
					normals.Add((origin + ht.hexagon.center));
					uvs.Add (uv0 + uvOffset);

					ht.hexagon.uv1i = uvs.Count;

					vertices.Add(ht.hexagon.v1*height);
					normals.Add((origin + ht.hexagon.v1));
					uvs.Add(uv1 + uvOffset);

					ht.hexagon.uv2i = uvs.Count;

					vertices.Add(ht.hexagon.v2*height);
					normals.Add((origin + ht.hexagon.v2));
					uvs.Add(uv2 + uvOffset);

					triangles.Add(centerIndex);
					triangles.Add(vertices.Count - 2);
					triangles.Add(vertices.Count - 1);

					// T2
				  ht.hexagon.uv3i = uvs.Count;
					vertices.Add(ht.hexagon.v3*height);
					normals.Add((origin + ht.hexagon.v3));
					uvs.Add(uv3 + uvOffset);

					triangles.Add(centerIndex);
					triangles.Add(vertices.Count - 2);
					triangles.Add(vertices.Count - 1);

					// T3
					ht.hexagon.uv4i = uvs.Count;
					vertices.Add(ht.hexagon.v4*height);
					normals.Add((origin + ht.hexagon.v4));
					uvs.Add(uv4 + uvOffset);

					triangles.Add(centerIndex);
					triangles.Add(vertices.Count - 2);
					triangles.Add(vertices.Count - 1);

					// T4
					ht.hexagon.uv5i = uvs.Count;
					vertices.Add(ht.hexagon.v5*height);
					normals.Add((origin + ht.hexagon.v5));
					uvs.Add(uv5 + uvOffset);

					triangles.Add(centerIndex);
					triangles.Add(vertices.Count - 2);
					triangles.Add(vertices.Count - 1);

					// T5
					ht.hexagon.uv6i = uvs.Count;
					vertices.Add(ht.hexagon.v6*height);
					normals.Add((origin + ht.hexagon.v6));
					uvs.Add(uv6 + uvOffset);

					triangles.Add(centerIndex);
					triangles.Add(vertices.Count - 2);
					triangles.Add(vertices.Count - 1);

					// T6
					triangles.Add(centerIndex);
					triangles.Add(vertices.Count - 1);   //1
					triangles.Add(vertices.Count - 6);   //6

          // Add six vertices with new UVs
          vertices.Add(ht.hexagon.v1*height);
          normals.Add(ht.hexagon.sideNormal1);
          uvs.Add(darkBorder+uv1);

          vertices.Add(ht.hexagon.v2*height);
          normals.Add(ht.hexagon.sideNormal2);
          uvs.Add(darkBorder+uv2);

          vertices.Add(ht.hexagon.v3*height);
          normals.Add(ht.hexagon.sideNormal3);
          uvs.Add(darkBorder+uv3);

          vertices.Add(ht.hexagon.v4*height);
          normals.Add(ht.hexagon.sideNormal4);
          uvs.Add(darkBorder+uv4);

          vertices.Add(ht.hexagon.v5*height);
          normals.Add(ht.hexagon.sideNormal5);
          uvs.Add(darkBorder+uv5);

          vertices.Add(ht.hexagon.v6*height);
          normals.Add(ht.hexagon.sideNormal6);
          uvs.Add(darkBorder+uv6);

					// Side 1
					triangles.Add(originIndex);
					triangles.Add(vertices.Count - 1);
					triangles.Add(vertices.Count - 2);

					// Side 2
					triangles.Add(originIndex);
					triangles.Add(vertices.Count - 2);
					triangles.Add(vertices.Count - 3);

					// Side 3
					triangles.Add(originIndex);
					triangles.Add(vertices.Count - 3);
					triangles.Add(vertices.Count - 4);

					// Side 4
					triangles.Add(originIndex);
					triangles.Add(vertices.Count - 4);
					triangles.Add(vertices.Count - 5);

					// Side 5
					triangles.Add(originIndex);
					triangles.Add(vertices.Count - 5);
					triangles.Add(vertices.Count - 6);
          
					// Side 6 extra vertex
					triangles.Add(originIndex);
					triangles.Add(vertices.Count - 6);
					triangles.Add(vertices.Count - 1);
          
				}
			}
    }
    else //Triangle, assumed that the texture's tiles have equilateral triangle dimensions
    {
      Debug.Log("triangle uvs"); 
      // float uv2x = 1.0f / tileCountW;
      // float uv1x = uv2x / 2;
      // float uv1y = 1.0f / tileCountH;
      // Vector2 uv0 = Vector2.zero,
      //         uv2 = new Vector2(uv2x, 0),
      //         uv1 = new Vector2(uv1x, uv1y);
      // //Generate quadrant
      // foreach (HexTile ht in world.tiles)
      // {
      //   if (ht.plate == i)
      //   {
      //     IntCoord uvCoord = tileSet.GetUVForType(ht.type);
      //     //Debug.Log("xCoord: "+ uvCoord.x + "  type: "+ ht.type);
      //     Vector2 uvOffset = new Vector2((uvCoord.x * uv2.x), (uvCoord.y * uv1.y));

      //     // Origin point
      //     int originIndex = vertices.Count;
      //     vertices.Add(origin);
      //     uvs.Add(uv1 + uvOffset);
      //     normals.Add(ht.hexagon.center - origin);

      //     // Center of hexagon
      //     int centerIndex = vertices.Count;

      //     // Triangle 1
      //     vertices.Add(ht.hexagon.center);
      //     normals.Add((origin + ht.hexagon.center));
      //     uvs.Add(uv1 + uvOffset);

      //     vertices.Add(ht.hexagon.v1);
      //     normals.Add((origin + ht.hexagon.v1));
      //     uvs.Add(uv0 + uvOffset);

      //     vertices.Add(ht.hexagon.v2);
      //     normals.Add((origin + ht.hexagon.v2));
      //     uvs.Add(uv2 + uvOffset);

      //     triangles.Add(centerIndex);
      //     triangles.Add(vertices.Count - 2);
      //     triangles.Add(vertices.Count - 1);

      //     // T2
      //     vertices.Add(ht.hexagon.v3);
      //     normals.Add((origin + ht.hexagon.v3));
      //     uvs.Add(uv0 + uvOffset);

      //     triangles.Add(centerIndex);
      //     triangles.Add(vertices.Count - 2);
      //     triangles.Add(vertices.Count - 1);

      //     // T3
      //     vertices.Add(ht.hexagon.v4);
      //     normals.Add((origin + ht.hexagon.v4));
      //     uvs.Add(uv2 + uvOffset);

      //     triangles.Add(centerIndex);
      //     triangles.Add(vertices.Count - 2);
      //     triangles.Add(vertices.Count - 1);

      //     // T4
      //     vertices.Add(ht.hexagon.v5);
      //     normals.Add((origin + ht.hexagon.v5));
      //     uvs.Add(uv0 + uvOffset);

      //     triangles.Add(centerIndex);
      //     triangles.Add(vertices.Count - 2);
      //     triangles.Add(vertices.Count - 1);

      //     // T5
      //     vertices.Add(ht.hexagon.v6);
      //     normals.Add((origin + ht.hexagon.v6));
      //     uvs.Add(uv2 + uvOffset);

      //     triangles.Add(centerIndex);
      //     triangles.Add(vertices.Count - 2);
      //     triangles.Add(vertices.Count - 1);

      //     // T6
      //     triangles.Add(centerIndex);
      //     triangles.Add(vertices.Count - 1);
      //     triangles.Add(vertices.Count - 6);

      //     // Side 1
      //     triangles.Add(originIndex);
      //     triangles.Add(vertices.Count - 1);
      //     triangles.Add(vertices.Count - 2);

      //     // Side 2
      //     triangles.Add(originIndex);
      //     triangles.Add(vertices.Count - 2);
      //     triangles.Add(vertices.Count - 3);

      //     // Side 3
      //     triangles.Add(originIndex);
      //     triangles.Add(vertices.Count - 3);
      //     triangles.Add(vertices.Count - 4);

      //     // Side 4
      //     triangles.Add(originIndex);
      //     triangles.Add(vertices.Count - 4);
      //     triangles.Add(vertices.Count - 5);

      //     // Side 5
      //     triangles.Add(originIndex);
      //     triangles.Add(vertices.Count - 5);
      //     triangles.Add(vertices.Count - 6);

      //     // Side 6
      //     triangles.Add(originIndex);
      //     triangles.Add(vertices.Count - 6);
      //     triangles.Add(vertices.Count - 1);
      //   }
      // }
    }
    //Debug.Log(uv1);
    //Debug.Log(uv2);
    //Debug.Log(uv0);
    //LabelCenters(sphere.finalTris);
    //LabelNeighbors(sphere);



    //GameObject centerMarker = (GameObject)GameObject.Instantiate(centerMarkerPrefab, tri.center, Quaternion.identity);
    Mesh m = new Mesh();
    m.vertices = vertices.ToArray();
    m.triangles = triangles.ToArray();
    m.normals = normals.ToArray();
    m.uv = uvs.ToArray();

    myCollider.sharedMesh = m;
    myFilter.sharedMesh = m;

    return output; 
  }
  
}

