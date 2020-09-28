using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexPlayerController : Mirror.NetworkBehaviour {
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
	//float currentHeight = 0;
	//float testH = 0;
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
	public int maxJumps = 2;
	public Camera cam;
	public float zoomMax = 6f;
	public float zoomMin = 0f;
	public float camZoomStep = .1f;
	//public float camZoomStep = .3f;
	public float camRotateSpeed = 4.2f;
	public float camSens = .5f;
	public int spawnTile = 0;
    //private float rotationApex = -1f;
    //private Transform initialTrans;

    //public Runebook runeBook;
    // Use this for initialization
    void Start () {
        if (isLocalPlayer)
        {
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
            trans.position = aW.tiles[spawnTile].hexagon.center * 10f;
            origin = new Vector3(aW.origin.x, aW.origin.y, aW.origin.z);
            animator = player.GetComponent<Animator>();
            animator.enabled = true;
            animator.Play("Idle");
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
        if (isLocalPlayer)
        {
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
            gravityDir = (origin - trans.position).normalized;
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
                //rigbody.velocity += -trans.right * vert * walkSpeed;
                trans.RotateAround(trans.position, gravityDir, -horz * rotateSpeed);
                animator.Play("Walk");
            }

            //cam.transform.RotateAround(head.position.normalized, gravityDir.normalized, -camRotateSpeed * Input.GetAxis("Mouse X"));
            trans.RotateAround(trans.up, gravityDir, -camRotateSpeed * Input.GetAxis("Mouse X"));

            //if (Vector3.Dot(head.position, cam.transform.forward) >= rotationApex) { 
            //cam.transform.RotateAround(cam.transform.position, cam.transform.right, -camRotateSpeed * Input.GetAxis("Mouse Y")); 
            //}
            //Debug.Log(Vector3.Dot(head.position.normalized, cam.transform.forward.normalized));
            float camDot = Vector3.Dot(head.position.normalized, cam.transform.forward.normalized);
            if ((camDot <= .9 && Input.GetAxis("Mouse Y") > 0) || (camDot >= -.9 && Input.GetAxis("Mouse Y") < 0))
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
