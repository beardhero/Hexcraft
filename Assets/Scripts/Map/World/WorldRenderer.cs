using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
 
  public List<GameObject> HexPlates(World world, TileSet tileSet)
  {
    List<GameObject> output = new List<GameObject>();
    //Populate polysphere.hPlates based on hextile plate index
    //First find number of plates
    /*
    hPlates = new List<List<HexTile>>();
    for (int i = 0; i <= world.numberOfPlates; i++)
    {
      hPlates.Add(new List<HexTile>());
    }
    */
    

    //Create a mesh for each plate and put it in the list of outputs
    for (int i = 0; i < world.numberOfPlates; i++)
    {
      output.Add(HexPlate(world, tileSet, i));
    }
    return output;
  }

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
      
      Vector2 darkBorder = new Vector2(.9f,.5f);  // A black point off the map
      
      foreach (HexTile ht in world.tiles)
			{
				if (ht.plate == i)
				{
          IntCoord uvCoord = WorldManager.instance.regularTileSet.GetUVForType(ht.type);
          Vector2 uvOffset = new Vector2(uvCoord.x * uvTileWidth, uvCoord.y * uvTileHeight);
					
          float height = 10+ht.height;

					// Origin point, every tile unfortunately repeats origin (@TODO and one vertex) for uv purposes
					int originIndex = vertices.Count;
					vertices.Add(origin*height);
					uvs.Add(darkBorder);
					normals.Add(ht.hexagon.center - origin);

					// Center of hexagon
					int centerIndex = vertices.Count;
					ht.hexagon.uv0i = uvs.Count; 
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

          // For each side add two vertices 
					// Side 1
					// triangles.Add(originIndex);
					// triangles.Add(vertices.Count - 1);
					// triangles.Add(vertices.Count - 2);

					// // Side 2
					// triangles.Add(originIndex);
					// triangles.Add(vertices.Count - 2);
					// triangles.Add(vertices.Count - 3);

					// // Side 3
					// triangles.Add(originIndex);
					// triangles.Add(vertices.Count - 3);
					// triangles.Add(vertices.Count - 4);

					// // Side 4
					// triangles.Add(originIndex);
					// triangles.Add(vertices.Count - 4);
					// triangles.Add(vertices.Count - 5);

					// // Side 5
					// triangles.Add(originIndex);
					// triangles.Add(vertices.Count - 5);
					// triangles.Add(vertices.Count - 6);
          
					// // Side 6 extra vertex
					// triangles.Add(originIndex);
					// triangles.Add(vertices.Count - 6);
					// triangles.Add(vertices.Count - 1);
          
				}
			}
    }
    else //Triangle, assumed that the texture's tiles have equilateral triangle dimensions
    {
      Debug.Log("triangle uvs"); 
      float uv2x = 1.0f / tileCountW;
      float uv1x = uv2x / 2;
      float uv1y = 1.0f / tileCountH;
      Vector2 uv0 = Vector2.zero,
              uv2 = new Vector2(uv2x, 0),
              uv1 = new Vector2(uv1x, uv1y);
      //Generate quadrant
      foreach (HexTile ht in world.tiles)
      {
        if (ht.plate == i)
        {
          IntCoord uvCoord = tileSet.GetUVForType(ht.type);
          //Debug.Log("xCoord: "+ uvCoord.x + "  type: "+ ht.type);
          Vector2 uvOffset = new Vector2((uvCoord.x * uv2.x), (uvCoord.y * uv1.y));

          // Origin point
          int originIndex = vertices.Count;
          vertices.Add(origin);
          uvs.Add(uv1 + uvOffset);
          normals.Add(ht.hexagon.center - origin);

          // Center of hexagon
          int centerIndex = vertices.Count;

          // Triangle 1
          vertices.Add(ht.hexagon.center);
          normals.Add((origin + ht.hexagon.center));
          uvs.Add(uv1 + uvOffset);

          vertices.Add(ht.hexagon.v1);
          normals.Add((origin + ht.hexagon.v1));
          uvs.Add(uv0 + uvOffset);

          vertices.Add(ht.hexagon.v2);
          normals.Add((origin + ht.hexagon.v2));
          uvs.Add(uv2 + uvOffset);

          triangles.Add(centerIndex);
          triangles.Add(vertices.Count - 2);
          triangles.Add(vertices.Count - 1);

          // T2
          vertices.Add(ht.hexagon.v3);
          normals.Add((origin + ht.hexagon.v3));
          uvs.Add(uv0 + uvOffset);

          triangles.Add(centerIndex);
          triangles.Add(vertices.Count - 2);
          triangles.Add(vertices.Count - 1);

          // T3
          vertices.Add(ht.hexagon.v4);
          normals.Add((origin + ht.hexagon.v4));
          uvs.Add(uv2 + uvOffset);

          triangles.Add(centerIndex);
          triangles.Add(vertices.Count - 2);
          triangles.Add(vertices.Count - 1);

          // T4
          vertices.Add(ht.hexagon.v5);
          normals.Add((origin + ht.hexagon.v5));
          uvs.Add(uv0 + uvOffset);

          triangles.Add(centerIndex);
          triangles.Add(vertices.Count - 2);
          triangles.Add(vertices.Count - 1);

          // T5
          vertices.Add(ht.hexagon.v6);
          normals.Add((origin + ht.hexagon.v6));
          uvs.Add(uv2 + uvOffset);

          triangles.Add(centerIndex);
          triangles.Add(vertices.Count - 2);
          triangles.Add(vertices.Count - 1);

          // T6
          triangles.Add(centerIndex);
          triangles.Add(vertices.Count - 1);
          triangles.Add(vertices.Count - 6);


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

          // Side 6
          triangles.Add(originIndex);
          triangles.Add(vertices.Count - 6);
          triangles.Add(vertices.Count - 1);
        }
      }
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

