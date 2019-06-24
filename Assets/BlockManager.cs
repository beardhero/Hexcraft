using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static List<HexBlock> blocks;
    public GameObject blockPrefab;
    public WorldManager worldManager;
    //public TileSet tileSet;
    // Start is called before the first frame update
    void Start()
    {
        //blocks = new List<HexBlock>();
        //worldManager = GameObject.FindWithTag("World Manager").GetComponent<WorldManager>();
        //tileSet = worldManager.regularTileSet;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                GameObject hitObject = hit.transform.gameObject;
                BlockInfo info = hitObject.GetComponent<BlockInfo>();
                if (info != null)
                {
                    HexTile tile = WorldManager.activeWorld.tiles[info.tileIndex];
                    int tri = hit.triangleIndex;
                    if (tri < 6) //top
                    {
                        Debug.Log("Placing Top  " + tri);
                        PlaceBlock(tile, blocks[info.blockIndex].height + 1, false);
                    }
                    if (tri >= 6 && tri < 12) //bot
                    {
                        Debug.Log("Placing Bot  " + tri);
                        PlaceBlock(tile, blocks[info.blockIndex].height - 1, false);
                    }
                    if (tri >= 12) //side
                    {
                        //get neighbor
                        Vector3 point = hit.point;                        
                        HexTile n = WorldManager.activeWorld.tiles[tile.neighbors[0]];
                        Vector3 cand = point - (Vector3)n.hexagon.center;
                        float check = cand.magnitude;
                        for (int i = 1; i < tile.neighbors.Count; i++)
                        {
                            HexTile nt = WorldManager.activeWorld.tiles[tile.neighbors[i]];
                            float nextCheck = (point - (Vector3)nt.hexagon.center).magnitude;
                            if (nextCheck < check)
                            {
                                //Debug.Log("actually got in here");
                                n = nt;
                                check = nextCheck;
                            }
                        }
                        PlaceBlock(n, blocks[info.blockIndex].height, false);
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(ray, out hit, 100.0f))
            {
                if (hit.transform.gameObject.GetComponent<BlockInfo>() != null)
                { Destroy(hit.transform.gameObject);}
            }
        }
    }

    public void PlaceBlock(HexTile tile, float height, bool isBedrock)
    {
        if (blocks == null)
        {
            blocks = new List<HexBlock>();
        }
        HexBlock toPlace = new HexBlock(tile, height, isBedrock);
        GameObject block = RenderBlock(toPlace);
        blocks.Add(toPlace);
        BlockInfo info = block.GetComponent<BlockInfo>();
        info.blockIndex = blocks.Count-1;
        info.tileIndex = tile.index;
    }

    public GameObject RenderBlock(HexBlock hb)
    {
        if (worldManager == null)
        {
            worldManager = GameObject.FindWithTag("World Manager").GetComponent<WorldManager>();
        }
        GameObject output = (GameObject)Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        output.layer = 0;
        MeshFilter myFilter = output.GetComponent<MeshFilter>();
        MeshCollider myCollider = output.GetComponent<MeshCollider>();

        SerializableVector3 origin = WorldManager.activeWorld.origin;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        //test
        float uvTileWidth = 1.0f / 42.0f; //tileSet.tileWidth / texWidth;
        float uvTileHeight = 1.0f / 42.0f; //tileSet.tileHeight / texHeight;
        //TileType type = TileType.Water;
        IntCoord uvCoord = worldManager.regularTileSet.GetUVForType(hb.type);
        Vector2 uvOffset = new Vector2(uvCoord.x * uvTileWidth, uvCoord.y * uvTileHeight);

        BlockInfo info = output.GetComponent<BlockInfo>();

        // Center of hexagon
        int centerIndex = vertices.Count;
        //ht.hexagon.uv0i = uvs.Count;
        // Triangle 1
        vertices.Add(hb.topCenter); //0
        normals.Add((origin + hb.topCenter));
        uvs.Add(WorldRenderer.uv0 + uvOffset);

        //ht.hexagon.uv1i = uvs.Count;

        vertices.Add(hb.topv1); //1
        normals.Add((origin + hb.topv1));
        uvs.Add(WorldRenderer.uv1 + uvOffset);

        //ht.hexagon.uv2i = uvs.Count;

        vertices.Add(hb.topv2); //2
        normals.Add((origin + hb.topv2));
        uvs.Add(WorldRenderer.uv2 + uvOffset);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T2
        //ht.hexagon.uv3i = uvs.Count;
        vertices.Add(hb.topv3);
        normals.Add((origin + hb.topv3));
        uvs.Add(WorldRenderer.uv3 + uvOffset);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T3
        //ht.hexagon.uv4i = uvs.Count;
        vertices.Add(hb.topv4);
        normals.Add((origin + hb.topv4));
        uvs.Add(WorldRenderer.uv4 + uvOffset);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T4
        //ht.hexagon.uv5i = uvs.Count;
        vertices.Add(hb.topv5);
        normals.Add((origin + hb.topv5));
        uvs.Add(WorldRenderer.uv5 + uvOffset);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T5
        //ht.hexagon.uv6i = uvs.Count;
        vertices.Add(hb.topv6);
        normals.Add((origin + hb.topv6));
        uvs.Add(WorldRenderer.uv6 + uvOffset);

        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T6
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 6);

        //////////////////////////// bottom hex

        // Center of hexagon
        int botcenterIndex = vertices.Count;
        //ht.hexagon.uv0i = uvs.Count;
        // bTriangle 1
        vertices.Add(hb.botCenter); //7
        normals.Add((origin - hb.botCenter));
        uvs.Add(WorldRenderer.uv0 + uvOffset);

        //ht.hexagon.uv1i = uvs.Count;

        vertices.Add(hb.botv1); //8
        normals.Add((origin - hb.botv1));
        uvs.Add(WorldRenderer.uv1 + uvOffset);

        //ht.hexagon.uv2i = uvs.Count;

        vertices.Add(hb.botv2);//9
        normals.Add((origin + hb.botv2));
        uvs.Add(WorldRenderer.uv2 + uvOffset);

        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);
        

        // bT2
        //ht.hexagon.uv3i = uvs.Count;
        vertices.Add(hb.botv3);//10
        normals.Add((origin + hb.botv3));
        uvs.Add(WorldRenderer.uv3 + uvOffset);

        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);
        

        // bT3
        //ht.hexagon.uv4i = uvs.Count;
        vertices.Add(hb.botv4);//11
        normals.Add((origin + hb.botv4));
        uvs.Add(WorldRenderer.uv4 + uvOffset);

        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);
        

        // bT4
        //ht.hexagon.uv5i = uvs.Count; 
        vertices.Add(hb.botv5); //12
        normals.Add((origin + hb.botv5));
        uvs.Add(WorldRenderer.uv5 + uvOffset);

        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);
        

        // bT5
        //ht.hexagon.uv6i = uvs.Count;
        vertices.Add(hb.botv6); //13
        normals.Add((origin + hb.botv6));
        uvs.Add(WorldRenderer.uv6 + uvOffset);

        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);
        

        // bT6
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 6);
        triangles.Add(vertices.Count - 1);
        

        //sides
        triangles.Add(vertices.Count - 13);
        triangles.Add(vertices.Count - 8);
        triangles.Add(vertices.Count - 6);

        triangles.Add(vertices.Count - 8);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 6);

        triangles.Add(vertices.Count - 12);
        triangles.Add(vertices.Count - 13);
        triangles.Add(vertices.Count - 5);

        triangles.Add(vertices.Count - 13);
        triangles.Add(vertices.Count - 6);
        triangles.Add(vertices.Count - 5);

        triangles.Add(vertices.Count - 11);
        triangles.Add(vertices.Count - 12);
        triangles.Add(vertices.Count - 4);

        triangles.Add(vertices.Count - 12);
        triangles.Add(vertices.Count - 5);
        triangles.Add(vertices.Count - 4);

        triangles.Add(vertices.Count - 10);
        triangles.Add(vertices.Count - 11);
        triangles.Add(vertices.Count - 3);

        triangles.Add(vertices.Count - 11);
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);

        triangles.Add(vertices.Count - 9);
        triangles.Add(vertices.Count - 10);
        triangles.Add(vertices.Count - 2);

        triangles.Add(vertices.Count - 10);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);

        triangles.Add(vertices.Count - 8);
        triangles.Add(vertices.Count - 9);
        triangles.Add(vertices.Count - 1);

        triangles.Add(vertices.Count - 9);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        ///
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
