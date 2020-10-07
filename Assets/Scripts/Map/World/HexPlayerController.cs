using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class HexPlayerController : NetworkBehaviour {
	Rigidbody rigbody;
	Transform trans;
	GameObject player;
	Transform head;
	Vector3 gravityDir;
	//Vector3 moveDir;
	//WorldManager wM;
	World aW;
	Vector3 origin;
	Animator animator;

    public BlockManager blockman;
	public float gravityScale = 0;
	public float walkSpeed = 0;
	public float runSpeed = 0;
	public float rotateSpeed = 6;
    public float jumpHeight = 0;
    public float scaleScaleFactor = .024f;
    public float gravityScaleFactor = .24f;
    public float jumpScaleFactor = 5f;
    public float walkScaleFactor = .12f;
    public float rayScaleFactor = 1f;
    public float zoomFactor = .24f;
	public bool canJump;
	public bool jumped;
	public int numberOfJumps;
	public int maxJumps = 20;
	public Camera cam;
	public float zoomMax = 6f;
	public float zoomMin = 0f;
	public float camZoomStep = .1f;
	//public float camZoomStep = .3f;
	public float camRotateSpeed = 4.2f;
	public float camSens = .5f;
	public int spawnTile = 0;
    public GameObject elementMenuHighlight;
    private RectTransform emh;
    Vector3 firePos = new Vector3(-438.8f, -222.6f, 0);
    Vector3 waterPos = new Vector3(-342.9f, -166.9f, 0);
    Vector3 airPos = new Vector3(-342.9f, -222.4f, 0);
    Vector3 earthPos = new Vector3(-438.8f, -167f, 0);
    Vector3 lightPos = new Vector3(-390.9f, -139.6f, 0);
    Vector3 darkPos = new Vector3(-390.9f, -250.6f, 0);

    //public Runebook runeBook;
    // Use this for initialization
    void Start () {
        if (isLocalPlayer)
        {
            // This hides the cursor
            Cursor.lockState = CursorLockMode.Locked;

            player = gameObject;
            trans = player.transform;
            //initialTrans = trans;
            head = gameObject.transform.GetChild(3);
            rigbody = gameObject.GetComponent<Rigidbody>();
            rigbody.useGravity = false;
            rigbody.freezeRotation = true;
            cam = gameObject.GetComponentInChildren<Camera>();
            //wM = GameObject.Find("WorldManager").GetComponent<WorldManager>();
            aW = WorldManager.activeWorld;
            animator = player.GetComponent<Animator>();
            animator.enabled = true;
            animator.Play("Idle");
            blockman = GameObject.FindGameObjectWithTag("Block Manager").GetComponent<BlockManager>(); 
            trans.position = aW.tiles[spawnTile].hexagon.center * 10f;
            //origin = new Vector3(aW.origin.x, aW.origin.y, aW.origin.z);
            emh = elementMenuHighlight.GetComponent<RectTransform>();
            emh.anchoredPosition3D = firePos;
            //runebook test
            //byte[] b = new byte[32];
            //for(int i = 0; i < 32; i++)
            //{
            //	b[i] = (byte)Random.Range(0,256);
            //}
            //runeBook = new Runebook(b);
            //GameObject runebook = Instantiate(runeBook.RunebookGO());
            /* @TODO
           Transform runebookTrans = runebook.transform;
           runebookTrans.parent = cam.transform;
           runebookTrans.position = cam.transform.position + cam.transform.forward;
           runebookTrans.LookAt(cam.transform);
           */
            //foreach(Rune r in runeBook.runes)
            //{
            //	Instantiate(r.RuneGO());
            //}
        }
        else {
            gameObject.GetComponent<Animator>().enabled = false;
            gameObject.GetComponent<CapsuleCollider>().enabled = false;
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.transform.GetChild(2).gameObject);
            Destroy(gameObject.transform.GetChild(3).gameObject);
            this.enabled = false;
        }
		
	}
    void Update()
    {
        RotateSkybox();
        if (isLocalPlayer)
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) {
                if(emh.anchoredPosition3D == firePos) { emh.anchoredPosition3D = earthPos; blockman.toPlace = TileType.Earth; }
                else if(emh.anchoredPosition3D == waterPos) { emh.anchoredPosition3D = airPos; blockman.toPlace = TileType.Air; }
                else if(emh.anchoredPosition3D == airPos) { emh.anchoredPosition3D = darkPos; blockman.toPlace = TileType.Dark; }
                else if(emh.anchoredPosition3D == earthPos) { emh.anchoredPosition3D = lightPos; blockman.toPlace = TileType.Light; }
                else if(emh.anchoredPosition3D == lightPos) { emh.anchoredPosition3D = waterPos; blockman.toPlace = TileType.Water; }
                else if(emh.anchoredPosition3D == darkPos) { emh.anchoredPosition3D = firePos; blockman.toPlace = TileType.Fire; }
                   
            } else if (Input.GetAxis("Mouse ScrollWheel") < 0f){
                if (emh.anchoredPosition3D == firePos) { emh.anchoredPosition3D = darkPos; blockman.toPlace = TileType.Dark; }
                else if (emh.anchoredPosition3D == darkPos) { emh.anchoredPosition3D = airPos; blockman.toPlace = TileType.Air; }
                else if (emh.anchoredPosition3D == lightPos) { emh.anchoredPosition3D = earthPos; blockman.toPlace = TileType.Earth; }
                else if (emh.anchoredPosition3D == earthPos) { emh.anchoredPosition3D = firePos; blockman.toPlace = TileType.Fire; }
                else if (emh.anchoredPosition3D == airPos) { emh.anchoredPosition3D = waterPos; blockman.toPlace = TileType.Water; }
                else if (emh.anchoredPosition3D == waterPos) { emh.anchoredPosition3D = lightPos; blockman.toPlace = TileType.Light; }
            }
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                Vector3 rp = cam.gameObject.transform.position;
                Vector3 rf = cam.gameObject.transform.forward;
                blockman.CmdRayPlaceBlock(rp, rf, blockman.toPlace);
            }

            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                Vector3 rp = cam.gameObject.transform.position;
                Vector3 rf = cam.gameObject.transform.forward;
                blockman.CmdRayRemoveBlock(rp, rf);
            }

            /*float f = Input.GetAxis("Mouse ScrollWheel");
            if (f > 0 || f < 0)
            {
                Vector3 v = cam.transform.position - head.position;
                if (v.magnitude <= zoomMax && f < 0) { cam.transform.position -= f * v * camZoomStep; }
                if (v.magnitude >= zoomMin && f > 0) { cam.transform.position -= f * v * camZoomStep; }
            }*/
            if (Input.GetKeyDown(KeyCode.Space) && numberOfJumps < maxJumps)
            {
                numberOfJumps++;
                jumped = true;
                animator.Play("Levitate");
                rigbody.AddForce(-gravityDir * jumpHeight);
                //jumped = false;
            }

            if (numberOfJumps >= maxJumps)
            {
                canJump = false;
            }
        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (isLocalPlayer)
        {
            //normalize down
            //gravityDir = (origin - trans.position).normalized;
            gravityDir = -trans.position.normalized;
            trans.rotation = Quaternion.FromToRotation(trans.up, -gravityDir) * trans.rotation;
            //gravity
            rigbody.AddForce(gravityDir * gravityScale * rigbody.mass, ForceMode.Acceleration);

            if (Input.GetAxis("Vertical") == 0 && Input.GetAxis("Horizontal") == 0)
            {
                animator.Play("Idle");
            }

            float vert = Input.GetAxis("Vertical");
            if (vert != 0)
            {
                rigbody.velocity += trans.forward * vert * walkSpeed;
                animator.Play("Walk");
            }

            float horz = Input.GetAxis("Horizontal");
            if (horz != 0)
            {
                rigbody.velocity += trans.right * horz * walkSpeed;
                //trans.RotateAround(trans.position, gravityDir, -horz * rotateSpeed);
                animator.Play("Walk");
            }

            //cam.transform.RotateAround(head.position.normalized, gravityDir.normalized, -camRotateSpeed * Input.GetAxis("Mouse X"));
            trans.RotateAround(trans.up, gravityDir, -camRotateSpeed * Input.GetAxis("Mouse X"));

            //if (Vector3.Dot(head.position, cam.transform.forward) >= rotationApex) { 
            //cam.transform.RotateAround(cam.transform.position, cam.transform.right, -camRotateSpeed * Input.GetAxis("Mouse Y")); 
            //}
            //Debug.Log(Vector3.Dot(head.position.normalized, cam.transform.forward.normalized));
            float camDot = Vector3.Dot(head.position.normalized, cam.transform.forward.normalized);
            if ((camDot <= .999 && Input.GetAxis("Mouse Y") > 0) || (camDot >= -.999 && Input.GetAxis("Mouse Y") < 0))
            {
                cam.transform.RotateAround(head.position, cam.transform.right, -camRotateSpeed * Input.GetAxis("Mouse Y"));
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                //reset cam rot
                cam.transform.rotation = Quaternion.identity;
            }
            //adjust scale, jumpheight, movespeed
            float mag = trans.position.magnitude;
            trans.localScale = Vector3.one * mag * scaleScaleFactor;
            gravityScale = mag * gravityScaleFactor;
            jumpHeight = mag * jumpScaleFactor;
            walkSpeed = mag * walkScaleFactor;
            runSpeed = mag * walkScaleFactor;
            zoomMax = mag * zoomFactor;
            //zoomMin = mag * zoomFactor;
            BlockManager.rayrange = mag * rayScaleFactor;
        }
    }

    void RotateSkybox(){
        RenderSettings.skybox.SetFloat("_Rotation", Time.time*10.0f);
        DynamicGI.UpdateEnvironment();
    }
	void OnCollisionEnter(Collision collision)
	{
        if (isLocalPlayer)
        {
            if (numberOfJumps > 0)
            {
                numberOfJumps = 0;
                if (jumped)
                {
                    jumped = false;
                    canJump = true;
                }
            }
        }
        //animator.Play("Idle");
	}

    void OnCollisionStay(Collision collision)
    {
        if (isLocalPlayer){
            numberOfJumps = 0;
            canJump = true;
            jumped = false; 
        }
	}
}
