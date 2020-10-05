using System;
using LibNoise.Unity.Generator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class GlobalSettings{
    public static bool MirrorOverride = true;  // Set true to disable all networking and test in offline mode
} 
public class BlockManager : NetworkBehaviour
{
    //public static List<HexBlock> blocks;  // blocks was rolled into activeWorld.tiles.blocks
    public static List<GameObject> plates;
    public static List<Mesh> plateMeshes;
    public static List<BlockInfo> plateInfos;
    //public static Dictionary<int, int[]> blocksOnTile; //blocksOnTile was converted to activeWorld.tiles.blocks
    public static int[] heightmap; //top block by hex tile index
    public static int avgHeight;
    public static int cloudHeight = 200;
    public static float cloudDensity = .24f;
    public static float rayrange;

    public Transform worldTrans;
    public WorldManager worldManager;
    static World aW => WorldManager.activeWorld;
    //public Transform playerTrans;
    public TileType toPlace;
    public static int maxBlocks = 4608;
    public static int maxHeight = 60;
    public float updateStep = 1;
    public float updateTimer = 0;
    float uvTileWidth = 1.0f / 16f;
    float uvTileHeight = 1.0f / 16f;

    public static float hexScale = 99;
    public static float blockScaleFactor = 0.1f;
    public static float blockQuarterFactor = .025f;
    //private static float _blockScaleFactor = 0.1f;
    //public static float BlockScaleFactor { get => _blockScaleFactor / WorldManager.worldSubdivisions; set => _blockScaleFactor = value; }

    [Command(ignoreAuthority = true)]
    public void CmdRayPlaceBlock(Vector3 rayPos, Vector3 rayFor) {
        bool quarterBlock = false;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            quarterBlock = true;
        }
        Ray ray = new Ray(rayPos, rayFor);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, rayrange))
        {
            GameObject hitObject = hit.transform.gameObject;
            BlockInfo info = hitObject.GetComponent<BlockInfo>();
            int plateInd = -1;
            //Find plate index
            for (int p = 0; p < plates.Count; p++)
            {
                if (plates[p] == hitObject) {
                    plateInd = p;
                }
            }
            if (plateInd == -1) {
                Debug.Log("error finding plate index");
            }

            //Debug.Log(tri);
            if (info != null)
            {
                //Find block we hit
                int tri = hit.triangleIndex;
                int blockIndex = tri / 24;      // 24 tris in a block

                int[] indices = info.blockIndices[blockIndex];
                HexBlock hb = WorldManager.activeWorld.tiles[indices[0]].blocks[indices[1]];  // info.hexBlocks are in triangle index order
                HexTile tile = WorldManager.activeWorld.tiles[hb.tileIndex];

                tri = tri % 24;

                // if (info.hexBlocks.Count >= maxBlocks)
                // {
                //     Debug.Log("Mana full");
                //     return;
                // }
                //float h = hb.height;
                //float bH = h - (h * blockScaleFactor);

                if (tri < 6) //top
                {
                    //Debug.Log("Placing Top  " + tri);
                    if (hb.blockHeight + 1 >= maxHeight)
                    {
                        Debug.Log("max height exceeded");
                        return;
                    }
                    RpcCreateBlock(hb.tileIndex, toPlace, hb.blockHeight + 1, false, quarterBlock, info.blockIndices.Count);
                    RpcAddToPlate(plateInd, hb.tileIndex);
                }
                if (tri >= 6 && tri < 12) //bot
                {
                    //Debug.Log("Placing Bot  " + tri);
                    if (hb.blockHeight - 1 < 0)
                    {
                        Debug.Log("min height exceeded");
                        return;
                    }
                    // HexBlock newBlock = new HexBlock();
                    RpcCreateBlock(hb.tileIndex, toPlace, hb.blockHeight - 1, false, quarterBlock, info.blockIndices.Count);
                    RpcAddToPlate(plateInd, hb.tileIndex);
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
                    // HexBlock newBlock = new HexBlock();
                    RpcCreateBlock(tile.neighbors[0], toPlace, hb.blockHeight, false, quarterBlock, info.blockIndices.Count);
                    RpcAddToPlate(n.plate, tile.neighbors[0]);
                }
            }
        }
    }

    [Command(ignoreAuthority = true)]
    public void CmdRayRemoveBlock(Vector3 rayPos, Vector3 rayFor) {
        Ray ray = new Ray(rayPos, rayFor);
        RaycastHit hit = new RaycastHit();
        if (Physics.Raycast(ray, out hit, rayrange))
        {
            GameObject hitObject = hit.transform.gameObject;
            BlockInfo info = hitObject.GetComponent<BlockInfo>();
            int plateInd = -1;
            //Find plate index
            for (int p = 0; p < plates.Count; p++)
            {
                if (plates[p] == hitObject)
                {
                    plateInd = p;
                }
            }
            if (plateInd == -1)
            {
                Debug.Log("error finding plate index");
            }
            //Find block we hit
            int tri = hit.triangleIndex;// % 24;
            int blockInPlate = tri / 24;

            if (info != null)
            {
                int[] indices = info.blockIndices[blockInPlate];
                Debug.Log(indices[0]+","+indices[1]);
                Debug.Log("unbreakable: "+WorldManager.activeWorld.tiles[indices[0]].blocks[indices[1]].unbreakable);

                if (!WorldManager.activeWorld.tiles[indices[0]].blocks[indices[1]].unbreakable)
                {
                    RpcRemoveFromPlate(plateInd, blockInPlate);
                }
            }
        }
    }

    [ClientRpc]
    public void RpcCreateBlock(int hexTileInd, TileType type, int blockHeight, bool unbreakable, bool quarterBlock, int indexInPlate)
    {
        HexBlock toPlace = new HexBlock();
        toPlace.tileIndex = hexTileInd;
        toPlace.type = type;
        toPlace.blockHeight = blockHeight;
        toPlace.unbreakable = unbreakable;
        toPlace.quarterBlock = quarterBlock;
        toPlace.index = aW.tiles[hexTileInd].blocks.Count;      // Note this is before it's added below, so no Count-1
        toPlace.indexInPlate = indexInPlate;
        toPlace.CreateBlock();
        WorldManager.activeWorld.tiles[hexTileInd].blocks.Add(toPlace);
    }

    public List<GameObject> BlockPlates(World world, TileSet tileSet, GameObject blockPrefab)
    {
        if (worldManager == null)
        {
            worldManager = GameObject.FindWithTag("World Manager").GetComponent<WorldManager>();

        }
        if (plates == null)
        {
            plates = new List<GameObject>();
        }
        if (plateInfos == null)
        {
            plateInfos = new List<BlockInfo>();
        }
        if (plateMeshes == null)
        {
            plateMeshes = new List<Mesh>();
        }
        List<GameObject> output = new List<GameObject>();
        //Create a mesh for each plate and put it in the list of outputs
        //Debug.Log("world.numberofPlates: " + world.numberOfPlates);
        for (int i = 0; i < world.numberOfPlates; i++)
        {

            output.Add(RenderBlockPlate(i, blockPrefab));

        }
        plates = output;

        return output;
    }

    public GameObject RenderBlockPlate(int plateIndex, GameObject blockPrefab)  // Q: blockPrefab is actually a plate prefab? And BlockInfo is actually PlateInfo?
    {
        GameObject output = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);
        output.transform.parent = worldTrans;

        output.layer = 0;
        MeshFilter myFilter = output.GetComponent<MeshFilter>();
        MeshCollider myCollider = output.GetComponent<MeshCollider>();

        SerializableVector3 origin = WorldManager.activeWorld.origin;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        BlockInfo info = output.GetComponent<BlockInfo>();
        info.plateIndex = plateIndex;
        info.blockIndices = new List<int[]>();

        int count = -1;

        foreach (HexTile ht in WorldManager.activeWorld.tiles)
        {
            if (ht.plate == plateIndex)     // Only render this block if it's supposed to go with this plate
            {
                if (info.plateOrigin == Vector3.zero)
                {
                    info.plateOrigin = ht.blocks[0].topCenter;
                }
                
                foreach (HexBlock hb in ht.blocks)
                {
                    count++;    // This is what matches up vertex numbering to hextile and hexblock
                    info.blockIndices.Add(new int[]{ht.index,hb.index});
                    hb.indexInPlate = count;

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
        }

        Mesh m = new Mesh();
        m.vertices = vertices.ToArray();
        m.triangles = triangles.ToArray();
        m.normals = normals.ToArray();
        m.uv = uvs.ToArray();

        myCollider.sharedMesh = m;
        myFilter.sharedMesh = m;

        plateMeshes.Add(m);
        plateInfos.Add(info);

        return output;
    }

    [ClientRpc]
    public void RpcAddToPlate(int plateId, int hexTileInd)
    {
        HexBlock hb = WorldManager.activeWorld.tiles[hexTileInd].blocks.Last();   // We know that this is always called after RPCCreatBlock and so the last block is what we want

        GameObject plate = plates[plateId];
        BlockInfo info = plate.GetComponent<BlockInfo>();

        MeshFilter mf = plate.GetComponent<MeshFilter>();
        MeshCollider mc = plate.GetComponent<MeshCollider>();
        Mesh m = mf.sharedMesh;

        SerializableVector3 origin = WorldManager.activeWorld.origin;
        List<Vector3> vertices = m.vertices.ToList();
        List<int> triangles = m.triangles.ToList();
        List<Vector3> normals = m.normals.ToList();
        List<Vector2> uvs = m.uv.ToList();

        
        // if (info.hexBlocks.Count >= maxBlocks)
        // {
        //     Debug.Log("Mana full");
        //     return;
        // }
        
        Debug.Log("index: "+hb.index+" | plateIndex: "+hb.indexInPlate);
        info.blockIndices.Add(new int[]{hexTileInd, hb.index});

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

    [ClientRpc]
    public void RpcRemoveFromPlate(int plateInd, int indicesIndex)
    {
        GameObject plate = plates[plateInd];
        MeshFilter mf = plate.GetComponent<MeshFilter>();
        MeshCollider mc = plate.GetComponent<MeshCollider>();
        Mesh m = mf.sharedMesh;

        BlockInfo info = plate.GetComponent<BlockInfo>();
        int[] indices = info.blockIndices[indicesIndex];
        HexBlock blockInWorld = WorldManager.activeWorld.tiles[indices[0]].blocks[indices[1]];
        Debug.Log("removing indexInPlate "+blockInWorld.indexInPlate);

        List<int> triangles = m.triangles.ToList();
        List<Vector3> vertices = m.vertices.ToList();
        List<Vector3> normals = m.normals.ToList();
        List<Vector2> uvs = m.uv.ToList();
        //get which index in blockindexes
        //int ind = 0;
        //remove vertices
        for (int v = vertices.Count - 1; v >= 0; v--)
        {
            if (v >= indicesIndex * 14 && v < (indicesIndex + 1) * 14)
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
            if (i >= (indicesIndex + 1) * 24 * 3)
            {
                //Debug.Log("subtracting tri" + i);
                //Debug.Log("before " + triangles[i]);
                triangles[i] -= 14;
                //if (triangles[i] < 0)
                //{ Debug.Log("fucked up tri" + triangles[i]); }
                //Debug.Log("after " + triangles[i]);
            }
            if (i >= indicesIndex * 24 * 3 && i < (indicesIndex + 1) * 24 * 3)
            {
                // Debug.Log("removing triangle " + i);
                //m.triangles[i] = -1;
                triangles.RemoveAt(i);
            }
        }

        //clean up block lists
        //decrement blockindex
        // foreach (GameObject p in plates)        // Do we need to alter every plate index?
        // {
            BlockInfo binfo = plate.GetComponent<BlockInfo>();
            for (int r = 0; r < binfo.blockIndices.Count; r++)
            {
                // Decrement all plate indexes above the one being removed
                if (WorldManager.activeWorld.tiles[binfo.blockIndices[r][0]].blocks[binfo.blockIndices[r][1]].indexInPlate > indicesIndex){
                    WorldManager.activeWorld.tiles[binfo.blockIndices[r][0]].blocks[binfo.blockIndices[r][1]].indexInPlate--;
                }

                // Now we alter tile index and the blockInfo index
                //   Skip checking blocks not in the same column or below the altered block
                if (binfo.blockIndices[r][0] != indices[0] || binfo.blockIndices[r][1] < indices[1])  
                    continue;

                WorldManager.activeWorld.tiles[binfo.blockIndices[r][0]].blocks[binfo.blockIndices[r][1]].index--;
                binfo.blockIndices[r][1]--;
            }
        //}
        /*decrement tile lookup
        for (int t = 0; t < WorldManager.activeWorld.tiles.Count; t++)
        {
            for (int b = 0; b < maxHeight; b++)
            {
                if (blocksOnTile[t][b] > blockInWorld)
                {
                    blocksOnTile[t][b]--;
                }
            }
        }*/

        WorldManager.activeWorld.tiles[indices[0]].blocks.RemoveAt(indices[1]);
        info.blockIndices.RemoveAt(indicesIndex);

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

    public void Populate(string seed)
    {
        // Set the surface heights
        Perlin perlin = new Perlin();
        perlin.Frequency = 0.0001f;//.0000002f;
        perlin.Lacunarity = 1.2f;//2.4f;      // How much the frequency increases with each octave
        perlin.Persistence = 1.1f;//.24f;  // How much the amplitude increases with each octave
        perlin.OctaveCount = 6;//6;
        float amplitude = maxHeight/12;//512f;
        perlin.Seed = BitConverter.ToInt32(SeedHandler.StringToBytes(seed), 0);
        heightmap = GenerateHeightmap(SeedHandler.StringToBytes(seed), perlin, amplitude);

        for (int h=0; h<heightmap.Length; h++)
        {
            avgHeight += heightmap[h];
            heightmap[h] += maxHeight/2;       // This is necessary to offset the perlin noise to compensate for negative values
        }
        avgHeight /= heightmap.Length;
        //int[] noBlock = new int[blocks.Count];

        //PerlinCaves(perlin, amplitude);

        AssignTypes();
        PlaceBlocks();

        //RefineWorld(SeedHandler.StringToBytes(seed));
    }
    public int[] GenerateHeightmap(byte[] seed, Perlin perlin, float amplitude)
    {
        Debug.Log("seed length" + seed.Length);
        int[] workingMap = new int[WorldManager.activeWorld.tiles.Count];

        //float glyphProb = 64;
        // for (int i = 0; i < seed.Length; i++)
        // {
        //     UnityEngine.Random.InitState(seed[i]);
        //     perlin.Seed = seed[i];
        //     //initialize 
        //     if (i == 0)
        //     {
        //         for (int h = 0; h < heightMap.Length; h++)
        //         {
        //             heightMap[h] = 64;
        //         }
        //     }
        //     PerlinHeightmapAdjust(heightMap, perlin, amplitude);
        // }

        PerlinHeightmapAdjust(workingMap, perlin, amplitude);

        return workingMap;
    }
    public void PerlinHeightmapAdjust(int[] workingMap, Perlin perlin, float amplitude)
    {
        for (int i = 0; i < WorldManager.activeWorld.tiles.Count; i++)
        {
            HexTile ht = WorldManager.activeWorld.tiles[i];
            //Get next height
            double perlinVal = perlin.GetValue(ht.hexagon.center.x * hexScale, ht.hexagon.center.y * hexScale, ht.hexagon.center.z * hexScale);
            double v1 = perlinVal * amplitude; 
            int h = (int)v1;
            //heightMap[i] += h;
            //heightMap[i] %= 256;
            workingMap[i] = h;
            if (h > 255)
                Debug.LogError("Tile #"+i+" exceeds 256: "+h);
        }
    }

    void AssignTypes(){
        // Assigns types purely by height (WIP)
        foreach (HexTile ht in WorldManager.activeWorld.tiles)
        {            
            ht.blocks = new List<HexBlock>();
            // Iterate from 0 (bedrock) up to heightmap[ht.index] (top layer)
            int top = BlockManager.heightmap[ht.index];
            if (top<0){
                Debug.LogError("heightmap got negative value: "+top);
                top = 1;
            }
            for (int i = 0; i < top; i++)
            {
                HexBlock blok = new HexBlock();
                blok.blockHeight = i;
                blok.tileIndex = ht.index;
                blok.index = i;

                if (i==0)
                    blok.type = TileType.Bedrock;
                else if (i==top)
                    blok.type = TileType.Arbor;
                else if (top-6 > i && i < top)
                    blok.type = TileType.Earth;
                else
                    blok.type = TileType.Metal;

                ht.blocks.Add(blok);
            }
        }
    }

    void PlaceBlocks(){
        foreach (HexTile ht in WorldManager.activeWorld.tiles)
        {
            int top = BlockManager.heightmap[ht.index];
            foreach (HexBlock blok in ht.blocks)
            {
               blok.quarterBlock = blok.blockHeight >= top-1;
                blok.unbreakable = false;

                blok.CreateBlock();
            }
        }
    }
}

