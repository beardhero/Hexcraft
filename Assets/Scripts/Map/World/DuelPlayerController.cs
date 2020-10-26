using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuelPlayerController : MonoBehaviour {
	Rigidbody rigbody;
	Transform trans;
	GameObject player;
	Transform head;
	Vector3 gravityDir;
	Vector3 moveDir;
	WorldManager wM;
	World aW;
	//Vector3 origin;
	Animator animator;
	//Camera cam;
	Actor actor;
	[HideInInspector]public Ray ray;
  	[HideInInspector]public RaycastHit hit;
	public int spawnTile = 0;
	public int onTile;
	public bool cast;
	//public RuneHex runeHex;
	// Use this for initialization
	void Start () {
		actor = GetComponent<Actor>();
		player = this.gameObject;
		trans = player.transform;
		//head = GameObject.Find("Head").transform;
		rigbody = GetComponent<Rigidbody>();
		rigbody.useGravity = false;
		rigbody.freezeRotation = true;
		//cam = Camera.main;
		wM = GameObject.Find("WorldManager").GetComponent<WorldManager>();
		aW = WorldManager.activeWorld;
		trans.position = aW.tiles[spawnTile].hexagon.center;
		//origin = new Vector3(aW.origin.x, aW.origin.y, aW.origin.z);
		animator = player.GetComponent<Animator>();
		animator.enabled = true;
		animator.Play("Idle");
        onTile = spawnTile;
        //runehex test
        byte[] testID = new byte[32] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
        List<int> testCast = aW.GetTilesInRadius(1, onTile);
        //runeHex = new Rune(testID).runeHex;
		//runeHex.Initialize(testCast);
		//move = false;
		//cast = true;
	}

	void Update () {
		if (Input.GetKeyDown(KeyCode.Mouse0))
    	{
			
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
      		if(Physics.Raycast(ray, out hit, 100.0f))
      		{ 
        		Debug.Log("casted");
        		int to = wM.GetHitTile(hit);
				Debug.Log("hit tile: " + to);
				StartCoroutine(actor.Move(to));
     		}
    	}
		//cast test
		if (Input.GetKeyDown(KeyCode.Mouse1))
    	{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
      		if(Physics.Raycast(ray, out hit, 100.0f))
      		{ 
        		Debug.Log("casted");
        		int to = wM.GetHitTile(hit);
				Debug.Log("hit tile: " + to);
				//StartCoroutine(runeHex.Cast(to));
     		}
    	}
	}
}
