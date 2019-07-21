using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Culler : MonoBehaviour
{
    //BlockManager blockManager;
    //WorldManager worldManager;
    GameObject player;
    public Camera maincam;
    public float cullRadius = 1f;
    public float cullRadFactor = 1f;
    public float viewDistance = 1000f;
    public float radiusBuffer = 6f;
    public int viewBuffer = 100;
    
    //public float dotBuffer = 1f;
    //store the meshrenderers so we aren't getting components every frame
    List<MeshRenderer> meshRenderers;
    List<MeshCollider> meshColliders;
    List<BlockInfo> blockInfos;
    
    // Start is called before the first frame update
    void Start()
    {
        //blockManager = GameObject.FindWithTag("Block Manager").GetComponent<BlockManager>();
        //worldManager = GameObject.FindWithTag("World Manager").GetComponent<WorldManager>();
        player = GameObject.FindWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (meshRenderers == null)
        {
            meshRenderers = new List<MeshRenderer>();
            foreach (GameObject plate in BlockManager.plates)
            {
                meshRenderers.Add(plate.GetComponent<MeshRenderer>());
            }
        }
        if (meshColliders == null)
        {
            meshColliders = new List<MeshCollider>();
            foreach (GameObject plate in BlockManager.plates)
            {
                meshColliders.Add(plate.GetComponent<MeshCollider>());
            }
        }
        if (blockInfos == null)
        {
            blockInfos = new List<BlockInfo>();
            foreach (GameObject plate in BlockManager.plates)
            {
                blockInfos.Add(plate.GetComponent<BlockInfo>());
            }
        }
        Vector3 playerPos = player.transform.position;
        float playerMag = playerPos.magnitude;
        cullRadius = playerMag * cullRadFactor;
        for (int i = 0; i < BlockManager.plates.Count; i++)
        {
            //bool rend = false;
            //don't check every block, only for plates in range, turned off for now
            if ((blockInfos[i].plateOrigin - playerPos).magnitude < cullRadius) // && ((blockInfos[i].plateOrigin - playerPos).magnitude < dotBuffer || Vector3.Dot(blockInfos[i].plateOrigin,player.transform.forward) > 0))
            //foreach (int b in blockInfos[i].blockIndexes)
            {
                /*Vector3 point = BlockManager.blocks[b].topCenter;
                Vector3 screenPoint = maincam.WorldToScreenPoint(point);
                if (screenPoint.z < viewDistance)
                {
                    if ((point - playerPos).magnitude < radiusBuffer || (screenPoint.x > 0 - viewBuffer && screenPoint.x <= maincam.pixelWidth + viewBuffer && screenPoint.y > 0 - viewBuffer && screenPoint.y <= maincam.pixelHeight + viewBuffer))
                    {*/
                        meshRenderers[i].enabled = true;
                        meshColliders[i].enabled = true;
                        //rend = true;
                        //break;
                    }
                    else
                    {
                        meshRenderers[i].enabled = false;
                        meshColliders[i].enabled = false;
                    }
                }
            }
            /*if (!rend)
            {
                meshRenderers[i].enabled = false;
                meshColliders[i].enabled = false;
            }*/
        
    
}
