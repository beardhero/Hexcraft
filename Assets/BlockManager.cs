using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BlockManager : MonoBehaviour
{
    public static List<HexBlock> blocks;
    public static List<GameObject> plates;
    public static float blockScaleFactor = 0.024f;
    public GameObject blockPrefab;
    public WorldManager worldManager;
    public TileType toPlace;
    
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
                //Find block we hit
                int tri = hit.triangleIndex;// % 24;
                int blockIndex = tri / 24;
                
                HexBlock hb = blocks[info.blockIndexes[blockIndex]];
                Debug.Log(info.blockIndexes[blockIndex]);
                HexTile tile = WorldManager.activeWorld.tiles[hb.tileIndex];
                tri = tri % 24;

                Debug.Log(tri);
                if (info != null)
                {
                    float h = hb.height;
                    float bH = h - (h * blockScaleFactor);
                    
                    if (tri < 6) //top
                    {
                        Debug.Log("Placing Top  " + tri);
                        CreateBlock(tile, toPlace, h + (h * blockScaleFactor), h, false);
                        AddToPlate(hitObject, blocks.Count -1);
                        
                    }
                    if (tri >= 6 && tri < 12) //bot
                    {
                        Debug.Log("Placing Bot  " + tri);
                        CreateBlock(tile, toPlace, bH, bH - (bH * blockScaleFactor), false);
                        AddToPlate(hitObject, blocks.Count -1);
                    }
                    if (tri >= 12 && tri < 24) //side
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
                                n = nt;
                                check = nextCheck;
                            }
                        }
                        CreateBlock(n, toPlace, h, bH, false);
                        AddToPlate(hitObject, blocks.Count - 1);
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
                GameObject hitObject = hit.transform.gameObject;
                BlockInfo info = hitObject.GetComponent<BlockInfo>();
                //Find block we hit
                int tri = hit.triangleIndex;// % 24;
                int blockInPlate = tri / 24;
                
                if (info != null)
                {
                    int blockInWorld = info.blockIndexes[blockInPlate];
                    //Debug.Log("removing from world " + blockInWorld);
                    //Debug.Log("removing from plate " + blockInPlate);
                    Debug.Log("hit tri " + tri);
                    RemoveFromPlate(hitObject, blockInPlate, blockInWorld);
                }//Dstroy(hit.transform.gameObject);}
            }
        }
    }

    public HexBlock CreateBlock(HexTile tile, TileType type, float height, float botHeight, bool isBedrock)
    {
        if (blocks == null)
        {
            blocks = new List<HexBlock>();
        }
        HexBlock toPlace = new HexBlock(tile, type, height, botHeight);//, isBedrock);
        //GameObject block = RenderBlock(toPlace);
        blocks.Add(toPlace);
        return toPlace;
    }

    public List<GameObject> BlockPlates(World world, TileSet tileSet)
    {
        if (worldManager == null)
        {
            worldManager = GameObject.FindWithTag("World Manager").GetComponent<WorldManager>();
        }
        List<GameObject> output = new List<GameObject>();
        //Create a mesh for each plate and put it in the list of outputs
        //Debug.Log("world.numberofPlates: " + world.numberOfPlates);
        for (int i = 0; i < world.numberOfPlates; i++)
        {
            output.Add(RenderBlockPlate(blocks, i));
        }
        plates = output;
        return output;
    }

    public GameObject RenderBlockPlate(List<HexBlock> blocks, int p)
    {
        
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
        float uvTileWidth = 1.0f / 16f; //tileSet.tileWidth / texWidth;
        float uvTileHeight = 1.0f / 16f; //tileSet.tileHeight / texHeight;
                                         //TileType type = TileType.Water;
        //int bNum = 0;
        BlockInfo info = output.GetComponent<BlockInfo>();
        info.plateIndex = p;
        info.blockIndexes = new List<int>();
        for (int i = 0; i < blocks.Count; i++)
        {
            HexBlock hb = blocks[i];
            if (hb.plate == p)
            {
                //bNum++;
                info.blockIndexes.Add(i);
                //info.blockCount = bNum;
                

                //info.tileIndex = hb.tileIndex;
                //info.topTris = new List<int>();
                //info.botTris = new List<int>();
                //info.sideTris = new List<int>();

                IntCoord uvCoord = worldManager.regularTileSet.GetUVForType(hb.type);
                Vector2 uvOffset = new Vector2(uvCoord.x * uvTileWidth, uvCoord.y * uvTileHeight);

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

                //info.topTris.Add(triangles.Count);
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);

                // T2
                //ht.hexagon.uv3i = uvs.Count;
                vertices.Add(hb.topv3);
                normals.Add((origin + hb.topv3));
                uvs.Add(WorldRenderer.uv3 + uvOffset);

                //info.topTris.Add(triangles.Count);
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);

                // T3
                //ht.hexagon.uv4i = uvs.Count;
                vertices.Add(hb.topv4);
                normals.Add((origin + hb.topv4));
                uvs.Add(WorldRenderer.uv4 + uvOffset);

                //info.topTris.Add(triangles.Count);
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);

                // T4
                //ht.hexagon.uv5i = uvs.Count;
                vertices.Add(hb.topv5);
                normals.Add((origin + hb.topv5));
                uvs.Add(WorldRenderer.uv5 + uvOffset);

                //info.topTris.Add(triangles.Count);
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);

                // T5
                //ht.hexagon.uv6i = uvs.Count;
                vertices.Add(hb.topv6);
                normals.Add((origin + hb.topv6));
                uvs.Add(WorldRenderer.uv6 + uvOffset);

                //info.topTris.Add(triangles.Count);
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);

                // T6
                //info.topTris.Add(triangles.Count);
                triangles.Add(centerIndex);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 6);

                //////////////////////////// bottom hex

                // Center of hexagon
                int botcenterIndex = vertices.Count;
                //ht.hexagon.uv0i = uvs.Count;
                // bTriangle 1
                vertices.Add(hb.botCenter); //7
                normals.Add((origin + hb.botCenter));
                uvs.Add(WorldRenderer.uv0 + uvOffset);

                //ht.hexagon.uv1i = uvs.Count;

                vertices.Add(hb.botv1); //8
                normals.Add((origin + hb.botv1));
                uvs.Add(WorldRenderer.uv3 + uvOffset);

                //ht.hexagon.uv2i = uvs.Count;

                vertices.Add(hb.botv2);//9
                normals.Add((origin + hb.botv2));
                uvs.Add(WorldRenderer.uv4 + uvOffset);
                //bT1
                //info.botTris.Add(triangles.Count);
                triangles.Add(botcenterIndex);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 2);


                // bT2
                //ht.hexagon.uv3i = uvs.Count;
                vertices.Add(hb.botv3);//10
                normals.Add((origin + hb.botv3));
                uvs.Add(WorldRenderer.uv5 + uvOffset);

                //info.botTris.Add(triangles.Count);
                triangles.Add(botcenterIndex);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 2);


                // bT3
                //ht.hexagon.uv4i = uvs.Count;
                vertices.Add(hb.botv4);//11
                normals.Add((origin + hb.botv4));
                uvs.Add(WorldRenderer.uv6 + uvOffset);

                //info.botTris.Add(triangles.Count);
                triangles.Add(botcenterIndex);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 2);


                // bT4
                //ht.hexagon.uv5i = uvs.Count; 
                vertices.Add(hb.botv5); //12
                normals.Add((origin + hb.botv5));
                uvs.Add(WorldRenderer.uv1 + uvOffset);

                //info.botTris.Add(triangles.Count);
                triangles.Add(botcenterIndex);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 2);


                // bT5
                //ht.hexagon.uv6i = uvs.Count;
                vertices.Add(hb.botv6); //13
                normals.Add((origin + hb.botv6));
                uvs.Add(WorldRenderer.uv2 + uvOffset);

                //info.botTris.Add(triangles.Count);
                triangles.Add(botcenterIndex);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 2);

                // bT6
                //info.botTris.Add(triangles.Count);
                triangles.Add(botcenterIndex);
                triangles.Add(vertices.Count - 6);
                triangles.Add(vertices.Count - 1);


                //sides
                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 13);
                triangles.Add(vertices.Count - 8);
                triangles.Add(vertices.Count - 6);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 8);
                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 6);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 12);
                triangles.Add(vertices.Count - 13);
                triangles.Add(vertices.Count - 5);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 13);
                triangles.Add(vertices.Count - 6);
                triangles.Add(vertices.Count - 5);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 11);
                triangles.Add(vertices.Count - 12);
                triangles.Add(vertices.Count - 4);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 12);
                triangles.Add(vertices.Count - 5);
                triangles.Add(vertices.Count - 4);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 10);
                triangles.Add(vertices.Count - 11);
                triangles.Add(vertices.Count - 3);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 11);
                triangles.Add(vertices.Count - 4);
                triangles.Add(vertices.Count - 3);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 9);
                triangles.Add(vertices.Count - 10);
                triangles.Add(vertices.Count - 2);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 10);
                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 2);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 8);
                triangles.Add(vertices.Count - 9);
                triangles.Add(vertices.Count - 1);

                //info.sideTris.Add(triangles.Count);
                triangles.Add(vertices.Count - 9);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);
            }
        }
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

    public void AddToPlate(GameObject plate, int blockInd)//HexBlock hb)
    {
        MeshFilter mf = plate.GetComponent<MeshFilter>();
        MeshCollider mc = plate.GetComponent<MeshCollider>();
        Mesh m = mf.sharedMesh;

        SerializableVector3 origin = WorldManager.activeWorld.origin;
        float uvTileWidth = 1.0f / 16f;
        float uvTileHeight = 1.0f / 16f;

        List<Vector3> vertices = m.vertices.ToList();
        List<int> triangles = m.triangles.ToList();
        List<Vector3> normals = m.normals.ToList();
        List<Vector2> uvs = m.uv.ToList();

        BlockInfo info = plate.GetComponent<BlockInfo>();
        info.blockIndexes.Add(blockInd);
        HexBlock hb = blocks[blockInd];
        //info.blockCount++;
        //info.tileIndex = hb.tileIndex;
        //info.topTris = new List<int>();
        //info.botTris = new List<int>();
        //info.sideTris = new List<int>();

        IntCoord uvCoord = worldManager.regularTileSet.GetUVForType(hb.type);
        Vector2 uvOffset = new Vector2(uvCoord.x * uvTileWidth, uvCoord.y * uvTileHeight);

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

        //info.topTris.Add(triangles.Count);
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T2
        //ht.hexagon.uv3i = uvs.Count;
        vertices.Add(hb.topv3);
        normals.Add((origin + hb.topv3));
        uvs.Add(WorldRenderer.uv3 + uvOffset);

        //info.topTris.Add(triangles.Count);
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T3
        //ht.hexagon.uv4i = uvs.Count;
        vertices.Add(hb.topv4);
        normals.Add((origin + hb.topv4));
        uvs.Add(WorldRenderer.uv4 + uvOffset);

        //info.topTris.Add(triangles.Count);
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T4
        //ht.hexagon.uv5i = uvs.Count;
        vertices.Add(hb.topv5);
        normals.Add((origin + hb.topv5));
        uvs.Add(WorldRenderer.uv5 + uvOffset);

        //info.topTris.Add(triangles.Count);
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T5
        //ht.hexagon.uv6i = uvs.Count;
        vertices.Add(hb.topv6);
        normals.Add((origin + hb.topv6));
        uvs.Add(WorldRenderer.uv6 + uvOffset);

        //info.topTris.Add(triangles.Count);
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        // T6
        //info.topTris.Add(triangles.Count);
        triangles.Add(centerIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 6);

        //////////////////////////// bottom hex

        // Center of hexagon
        int botcenterIndex = vertices.Count;
        //ht.hexagon.uv0i = uvs.Count;
        // bTriangle 1
        vertices.Add(hb.botCenter); //7
        normals.Add((origin + hb.botCenter));
        uvs.Add(WorldRenderer.uv0 + uvOffset);

        //ht.hexagon.uv1i = uvs.Count;

        vertices.Add(hb.botv1); //8
        normals.Add((origin + hb.botv1));
        uvs.Add(WorldRenderer.uv3 + uvOffset);

        //ht.hexagon.uv2i = uvs.Count;

        vertices.Add(hb.botv2);//9
        normals.Add((origin + hb.botv2));
        uvs.Add(WorldRenderer.uv4 + uvOffset);
        //bT1
        //info.botTris.Add(triangles.Count);
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);


        // bT2
        //ht.hexagon.uv3i = uvs.Count;
        vertices.Add(hb.botv3);//10
        normals.Add((origin + hb.botv3));
        uvs.Add(WorldRenderer.uv5 + uvOffset);

        //info.botTris.Add(triangles.Count);
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);


        // bT3
        //ht.hexagon.uv4i = uvs.Count;
        vertices.Add(hb.botv4);//11
        normals.Add((origin + hb.botv4));
        uvs.Add(WorldRenderer.uv6 + uvOffset);

        //info.botTris.Add(triangles.Count);
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);


        // bT4
        //ht.hexagon.uv5i = uvs.Count; 
        vertices.Add(hb.botv5); //12
        normals.Add((origin + hb.botv5));
        uvs.Add(WorldRenderer.uv1 + uvOffset);

        //info.botTris.Add(triangles.Count);
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);


        // bT5
        //ht.hexagon.uv6i = uvs.Count;
        vertices.Add(hb.botv6); //13
        normals.Add((origin + hb.botv6));
        uvs.Add(WorldRenderer.uv2 + uvOffset);

        //info.botTris.Add(triangles.Count);
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 2);

        // bT6
        //info.botTris.Add(triangles.Count);
        triangles.Add(botcenterIndex);
        triangles.Add(vertices.Count - 6);
        triangles.Add(vertices.Count - 1);


        //sides
        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 13);
        triangles.Add(vertices.Count - 8);
        triangles.Add(vertices.Count - 6);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 8);
        triangles.Add(vertices.Count - 1);
        triangles.Add(vertices.Count - 6);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 12);
        triangles.Add(vertices.Count - 13);
        triangles.Add(vertices.Count - 5);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 13);
        triangles.Add(vertices.Count - 6);
        triangles.Add(vertices.Count - 5);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 11);
        triangles.Add(vertices.Count - 12);
        triangles.Add(vertices.Count - 4);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 12);
        triangles.Add(vertices.Count - 5);
        triangles.Add(vertices.Count - 4);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 10);
        triangles.Add(vertices.Count - 11);
        triangles.Add(vertices.Count - 3);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 11);
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 9);
        triangles.Add(vertices.Count - 10);
        triangles.Add(vertices.Count - 2);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 10);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 8);
        triangles.Add(vertices.Count - 9);
        triangles.Add(vertices.Count - 1);

        //info.sideTris.Add(triangles.Count);
        triangles.Add(vertices.Count - 9);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 1);

        m.vertices = vertices.ToArray();
        m.triangles = triangles.ToArray();
        m.normals = normals.ToArray();
        m.uv = uvs.ToArray();
        mf.sharedMesh = m;
        mc.sharedMesh = m;
        
        //return output;
    }

    public void RemoveFromPlate(GameObject plate, int blockInPlate, int blockInWorld)
    {
        //HexBlock hb = blocks[block];
        MeshFilter mf = plate.GetComponent<MeshFilter>();
        MeshCollider mc = plate.GetComponent<MeshCollider>();
        Mesh m = mf.sharedMesh;
        BlockInfo info = plate.GetComponent<BlockInfo>();
        List<int> triangles = m.triangles.ToList();
        List<Vector3> vertices = m.vertices.ToList();
        List<Vector3> normals = m.normals.ToList();
        List<Vector2> uvs = m.uv.ToList();
        //get which index in blockindexes
        //int ind = 0;
        //remove vertices
        for (int v = vertices.Count - 1; v >= 0; v--)
        {
            if (v >= blockInPlate * 14 && v < (blockInPlate + 1) * 14)
            {
                //Debug.Log("removing vertex " + v);
                vertices.RemoveAt(v);
                normals.RemoveAt(v);
                uvs.RemoveAt(v);
                //vertices[v] *= 1.01f;//Vector3.zero;

            }
        }
        //remove triangles
        
        for (int i = triangles.Count - 1; i >= 0; i--)
        {
            if (i >= (blockInPlate + 1) * 24 * 3)
            {
                //Debug.Log("subtracting tri" + i);
                //Debug.Log("before " + triangles[i]);
                triangles[i] -= 14;
                if (triangles[i] < 0)
                { Debug.Log("fucked up tri" + triangles[i]); }
                //Debug.Log("after " + triangles[i]);
            }
            if (i >= blockInPlate * 24 * 3 && i < (blockInPlate+1) * 24 * 3)
            {
                Debug.Log("removing triangle " + i);
                //m.triangles[i] = -1;
                triangles.RemoveAt(i);
            }
        }

        //clean up block lists
        
        foreach (GameObject p in plates)
        {
            BlockInfo binfo = p.GetComponent<BlockInfo>();
            for (int r = 0; r < binfo.blockIndexes.Count; r++)
            {
                if (binfo.blockIndexes[r] > blockInWorld)
                {
                    binfo.blockIndexes[r]--;
                }
            }
        }
        
        info.blockIndexes.RemoveAt(blockInPlate);
        blocks.RemoveAt(blockInWorld);
        //reset mesh
        Mesh newmesh = new Mesh();
        newmesh.vertices = vertices.ToArray();
        newmesh.normals = normals.ToArray();
        newmesh.uv = uvs.ToArray();
        newmesh.triangles = triangles.ToArray();

        m = newmesh;
        //m.vertices = vertices.ToArray();
        //m.triangles = triangles.ToArray();
        mf.sharedMesh = m;
        mc.sharedMesh = m;
    }
}

