using System;
using LibNoise.Unity.Generator;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using UnityEngine.Profiling;

public class BlockManager : NetworkBehaviour
{
    public static List<HexBlock> blocks;
    public static List<GameObject> plates;
    public static List<Mesh> plateMeshes;
    public static List<BlockInfo> plateInfos;
    public static Dictionary<int, int[]> blocksOnTile; //hex tile index to block index array, ascending order
    public static int[] heightmap; //top block by hex tile index
    public static int avgHeight;
    public static int cloudHeight = 200;
    public static float cloudDensity = .24f;
    public static float rayrange;
    static float hexScale = 99;

    public WorldManager worldManager;
    //public Transform playerTrans;
    public TileType toPlace;
    public static int maxBlocks = 4608;
    public static int maxHeight = 128;
    public float updateStep = 1;
    public float updateTimer = 0;
    float uvTileWidth = 1.0f / 16f;
    float uvTileHeight = 1.0f / 16f;

    public static float blockScaleFactor = 0.1f;
    public static float blockQuarterFactor = .025f;
    //private static float _blockScaleFactor = 0.1f;
    //public static float BlockScaleFactor { get => _blockScaleFactor / WorldManager.worldSubdivisions; set => _blockScaleFactor = value; }
    
    public HexBlock GetBlockByTileAndHeight(int tile, int blockHeight) //unnecessary
    {
        return blocks[blocksOnTile[tile][blockHeight]];
    }
    public void Biomes()
    {
        //set the initial biomes
        for (int i = 0; i < blocksOnTile.Count; i++)
        {
            int l = blocksOnTile[i].Length;
            for (int b = 0; b < l; b++)
            {
                if (b == l - 1)
                {
                    blocks[blocksOnTile[i][b]].ChangeType(TileType.Earth);
                }

                if (b <= l - 2 && b >= l - 6)
                {
                    blocks[blocksOnTile[i][b]].ChangeType(TileType.Arbor);
                }

                if (b < l - 6)
                {
                    blocks[blocksOnTile[i][b]].ChangeType(TileType.Metal);
                }
            }
        }
    }
    public void Populate(string seed)
    {
        // -- 1. Elevation map
        // -- 2. Heat Map
        // -- 3. Moisture map
        // -- 4. Rivers
        // -- 5. Biomes
        // -- 6. Caves
        // -- 7. Place Blocks

        heightmap = GenerateHeightmap(seed);
        foreach (int h in heightmap)
        {
            avgHeight += h;
        }
        avgHeight /= heightmap.Length;
        CreateBlocks();     // Assigns types currently using plate types
    }
    public int[] GenerateHeightmap(string seed)
    {
        int[] hmap = new int[WorldManager.activeWorld.tiles.Count];

        UnityEngine.Random.InitState(seed.GetHashCode());
        Perlin perlin = PerlinType.DefaultSurface(seed);
        float amplitude = maxHeight / 12;

        for (int i = 0; i < WorldManager.activeWorld.tiles.Count; i++)
        {
            HexTile ht = WorldManager.activeWorld.tiles[i];
            //Get next height
            // Note static float hexScale = 99;
            double perlinVal = perlin.GetValue(ht.hexagon.center.x * hexScale, ht.hexagon.center.y * hexScale, ht.hexagon.center.z * hexScale);
            double v1 = perlinVal * amplitude;//*i; 
            int h = (int)v1;
            hmap[i] = h + (int)(maxHeight/2f);      // Note that we pad the values to prevent negative heights
        }

        return hmap;
    }

    void CreateBlocks(){
        foreach (HexTile ht in WorldManager.activeWorld.tiles)
        {            
            // Iterate from 0 (bedrock) up to heightmap[ht.index] (top layer)
            int top = BlockManager.heightmap[ht.index];
            if (top<0){
                Debug.LogError("heightmap got negative value: "+top);
                top = 1;
            }
            for (int i = 0; i < top; i++)
            {
                HexBlock blok = CreateBlock(ht.index, ht.type, i, false, i >= top-1);

                // Or set by height
                // if (i==0)
                //     blok.type = TileType.Gray;
                // else if (i==top)
                //     blok.type = TileType.Arbor;
                // else if (top-6 > i && i < top)
                //     blok.type = TileType.Earth;
                // else
                //     blok.type = TileType.Metal;

                blocks.Add(blok);
            }
        }
    }

    
    //@TODO: client should do the raytracing and pass in blockIndex
    [Command(ignoreAuthority = true)]
    public void CmdRayPlaceBlock(Vector3 rayPos, Vector3 rayFor, TileType toplace) {
        Debug.Log("placing");
        toPlace = toplace;
        bool quarterBlock = false;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            quarterBlock = true;
        }
        Ray ray = new Ray(rayPos, rayFor);
        RaycastHit hit = new RaycastHit();
        Debug.Log("ray range " + rayrange);
        if (Physics.Raycast(ray, out hit, 10000))
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
                int blockIndex = tri / 24;

                HexBlock hb = blocks[info.blockIndexes[blockIndex]];
                //Debug.Log(info.blockIndexes[blockIndex]);
                HexTile tile = WorldManager.activeWorld.tiles[hb.tileIndex];
                tri = tri % 24;

                if (info.blockIndexes.Count >= maxBlocks)
                {
                    Debug.Log("Mana full");
                    return;
                }
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
                    Debug.Log("placing at height " + hb.blockHeight + 1);
                    blocks.Add(CreateBlock(hb.tileIndex, toPlace, hb.blockHeight + 1, false, quarterBlock));
                    AddToPlate(plateInd);
                    RpcCreateBlock(hb.tileIndex, toPlace, hb.blockHeight + 1, false, quarterBlock);
                    RpcAddToPlate(plateInd);
                }
                if (tri >= 6 && tri < 12) //bot
                {
                    //Debug.Log("Placing Bot  " + tri);
                    if (hb.blockHeight - 1 < 0)
                    {
                        Debug.Log("min height exceeded");
                        return;
                    }
                    blocks.Add(CreateBlock(hb.tileIndex, toPlace, hb.blockHeight - 1, false, quarterBlock));
                    AddToPlate(plateInd);
                    RpcCreateBlock(hb.tileIndex, toPlace, hb.blockHeight - 1, false, quarterBlock);
                    RpcAddToPlate(plateInd);
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
                    blocks.Add(CreateBlock(n.index, toPlace, hb.blockHeight, false, quarterBlock));
                    AddToPlate(n.plate);
                    RpcCreateBlock(n.index, toPlace, hb.blockHeight, false, quarterBlock);
                    RpcAddToPlate(n.plate);
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
                Debug.LogError("error finding plate index");
            }
            //Find block we hit
            int tri = hit.triangleIndex;// % 24;
            int blockInPlate = tri / 24;

            if (info != null)
            {
                int blockInWorld = info.blockIndexes[blockInPlate];
                if (!blocks[blockInWorld].unbreakable)
                {
                    RemoveFromPlate(plateInd, blockInWorld);
                    RpcRemoveFromPlate(plateInd, blockInWorld);
                }
            }
            else {
                Debug.LogError("block info null " + plateInd + "  " + blockInPlate);
            }
        }
    }

    [ClientRpc]
    public void RpcCreateBlock(int hexTileInd, TileType type, int blockHeight, bool isBreakable, bool quarterBlock)
    {
        if (!isServer)
        {
            blocks.Add(CreateBlock(hexTileInd, type, blockHeight, isBreakable, quarterBlock));
        }
    }

    public HexBlock CreateBlock(int hexTileInd, TileType type, int blockHeight, bool isBreakable, bool quarterBlock)
    {
        if (blocks == null)
        {
            blocks = new List<HexBlock>();
            //blocks = new HexBlock[42 * WorldManager.worldSubdivisions * WorldManager.activeWorld.tiles.Count];
        }
        if (blocksOnTile == null)
        {
            blocksOnTile = new Dictionary<int, int[]>();
        }
        if (!blocksOnTile.ContainsKey(hexTileInd))
        {
            blocksOnTile[hexTileInd] = new int[maxHeight];
        }
        HexBlock toPlace = new HexBlock(hexTileInd, type, blockHeight, isBreakable, quarterBlock);//, isBedrock);

        //add to tile lookup
        //blocksOnTile[tile.index][blockHeight] = blocks.Count;

        //blocks.Add(toPlace);
        return toPlace;
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
        for (int i = 0; i < world.numberOfPlates; i++)
        {
            output.Add(RenderBlockPlate(blocks, i, blockPrefab));
        }
        plates = output;

        return output;
    }

    public GameObject RenderBlockPlate(List<HexBlock> blocks, int p, GameObject blockPrefab)
    {
        GameObject output = Instantiate(blockPrefab, Vector3.zero, Quaternion.identity);

        output.layer = 0;
        MeshFilter myFilter = output.GetComponent<MeshFilter>();
        MeshCollider myCollider = output.GetComponent<MeshCollider>();

        SerializableVector3 origin = WorldManager.activeWorld.origin;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        //test
        //tileSet.tileHeight / texHeight;
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
                if (info.plateOrigin == Vector3.zero)
                {
                    info.plateOrigin = hb.topCenter;
                }
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
        //Debug.Log("plate " + p + " block count " + info.blockIndexes.Count);
        //@BUG fix the plate generation, quick fix for this plateOrigin bug
        /*if (info.plateOrigin == Vector3.zero)
        {
            info.plateOrigin = blocks[info.blockIndexes[0]].topCenter;
        }*/
        ///
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
    public void RpcAddToPlate(int plateId)
    {
        if (!isServer)
        {
            int blockInd = blocks.Count - 1;
            Debug.Log("block index on adding " + blockInd);
            GameObject plate = plates[plateId];
            MeshFilter mf = plate.GetComponent<MeshFilter>();
            MeshCollider mc = plate.GetComponent<MeshCollider>();
            Mesh m = mf.sharedMesh;

            SerializableVector3 origin = WorldManager.activeWorld.origin;
            List<Vector3> vertices = m.vertices.ToList();
            List<int> triangles = m.triangles.ToList();
            List<Vector3> normals = m.normals.ToList();
            List<Vector2> uvs = m.uv.ToList();
            
            BlockInfo info = plate.GetComponent<BlockInfo>();
            if (info.blockIndexes.Count >= maxBlocks)
            {
                Debug.Log("Mana full");
                return;
            }
            info.blockIndexes.Add(blockInd);
            Debug.Log("block index: " + blockInd);
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
        }
    }

    public void AddToPlate(int plateId)
    {
        int blockInd = blocks.Count - 1; //created block just before
        GameObject plate = plates[plateId];
        MeshFilter mf = plate.GetComponent<MeshFilter>();
        MeshCollider mc = plate.GetComponent<MeshCollider>();
        Mesh m = mf.sharedMesh;

        SerializableVector3 origin = WorldManager.activeWorld.origin;
        List<Vector3> vertices = m.vertices.ToList();
        List<int> triangles = m.triangles.ToList();
        List<Vector3> normals = m.normals.ToList();
        List<Vector2> uvs = m.uv.ToList();

        BlockInfo info = plate.GetComponent<BlockInfo>();
        if (info.blockIndexes.Count >= maxBlocks)
        {
            Debug.Log("Mana full");
            return;
        }
        info.blockIndexes.Add(blockInd);
        Debug.Log("block index: " + blockInd);
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

    [ClientRpc]
    public void RpcRemoveFromPlate(int plateInd, int blockInWorld)
    {
        if (!isServer)
        {
            GameObject plate = plates[plateInd];
            //HexBlock hb = blocks[block];
            MeshFilter mf = plate.GetComponent<MeshFilter>();
            MeshCollider mc = plate.GetComponent<MeshCollider>();
            Mesh m = mf.sharedMesh;

            //Debug.Log("rpc remove from plate " + plateInd + "  " + blockInWorld);
            BlockInfo info = plate.GetComponent<BlockInfo>();
            int blockInPlate = info.blockIndexes.IndexOf(blockInWorld);

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
                    //if (triangles[i] < 0)
                    //{ Debug.Log("fucked up tri" + triangles[i]); }
                    //Debug.Log("after " + triangles[i]);
                }
                if (i >= blockInPlate * 24 * 3 && i < (blockInPlate + 1) * 24 * 3)
                {
                    // Debug.Log("removing triangle " + i);
                    //m.triangles[i] = -1;
                    triangles.RemoveAt(i);
                }
            }

            //clean up block lists
            //decrement blockindex
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
            Debug.Log("blockInPlate " + blockInPlate);
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

    public void RemoveFromPlate(int plateInd, int blockInWorld)
    {
        GameObject plate = plates[plateInd];
        //HexBlock hb = blocks[block];
        MeshFilter mf = plate.GetComponent<MeshFilter>();
        MeshCollider mc = plate.GetComponent<MeshCollider>();
        Mesh m = mf.sharedMesh;

        //Debug.Log("rpc remove from plate " + plateInd + "  " + blockInWorld);
        BlockInfo info = plate.GetComponent<BlockInfo>();
        int blockInPlate = info.blockIndexes.IndexOf(blockInWorld);

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
                //if (triangles[i] < 0)
                //{ Debug.Log("fucked up tri" + triangles[i]); }
                //Debug.Log("after " + triangles[i]);
            }
            if (i >= blockInPlate * 24 * 3 && i < (blockInPlate + 1) * 24 * 3)
            {
                // Debug.Log("removing triangle " + i);
                //m.triangles[i] = -1;
                triangles.RemoveAt(i);
            }
        }

        //clean up block lists
        //decrement blockindex
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

