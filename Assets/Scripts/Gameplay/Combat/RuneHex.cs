using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]

public struct Spells
{
    public SingleTargetHex[] SingleTargetHexes;
    public AreaTargetHex[] AreaTargetHexes;
    public LifeHex[] LifeHexes;
}
//public enum Targetting{Single, AoE, Life}
public abstract class RuneHex{
    public byte[] ID;
    public List<int> castedTiles;
    public float amplitude;
    //public int generations;
    public float castTime;
    public float cooldown;
    public int manaCost;
    public int glyph;
    public TileType element;
    public GameObject effect;
    public GameObject instance;

	public abstract void Initialize(List<int> casted);
	public abstract IEnumerator Cast(int origin);
    public void InstantiateAboveTile(int tile, float height, Vector3 scale)
    {
        Vector3 tileVec = WorldManager.activeWorld.tiles[tile].hexagon.center;
        Vector3 tileVecNorm = tileVec.normalized;
        Vector3 startPos = tileVec + tileVecNorm * height; //above the tile by 1 unit 
        Vector3 startingScale = scale;
        effect.transform.localScale = startingScale;
        instance = GameObject.Instantiate(effect, startPos, Quaternion.identity);
    }
    public IEnumerator MoveUpScaleUpSpin(float height, float spinSpeed, Vector3 toScale)
    {
        Transform instanceTrans = instance.transform;
        Vector3 worldOrigin = CombatManager.activeWorld.origin;
        Vector3 startingPos = instanceTrans.position,
        endingPos = startingPos + (startingPos - worldOrigin).normalized * height; //+t.hexagon.normal;
        //Quaternion startingRot = instanceTrans.rotation,
        //endingRot = Quaternion.identity;//Quaternion.LookRotation(endingPos - startingPos, tileVecNorm);
        Vector3 rotationAxis = instanceTrans.position - worldOrigin;
        float time = 1.0f;

        for (float i = 0; i < time; i += Time.deltaTime)
        {
            instanceTrans.position = Vector3.Lerp(startingPos, endingPos, i / time);
            //instanceTrans.rotation = Quaternion.Slerp(startingRot, endingRot, i / time);
            instanceTrans.Rotate(rotationAxis, spinSpeed);
            instanceTrans.localScale = Vector3.Lerp(instance.transform.localScale, toScale, i / time);
            yield return null;
        }
        //yield return null;
    }
}

public abstract class SingleTargetHex : RuneHex
{
	public abstract override void Initialize(List<int> casted);
	public abstract override IEnumerator Cast(int origin);
}

public abstract class AreaTargetHex : RuneHex
{
	public abstract override void Initialize(List<int> casted);
	public abstract override IEnumerator Cast(int origin);
}

public abstract class LifeHex : RuneHex
{
	public int generations;
    public bool[] rules;
    public int spell;
    public abstract override void Initialize(List<int> casted);
    public abstract override IEnumerator Cast(int origin);
    public void Seed()
    {
        //we'll take the sum of the neighborhood
        //the rules determine what neighborhood sum is required to make a cell survive or increase by 1 generation
        //a cell's generation increases if its neighborhood sums to a true bool's index
        rules = new bool[13];
        for (int i = 0; i < ID.Length; i++)
        {
            UnityEngine.Random.InitState((int)ID[i]);
            int on = UnityEngine.Random.Range(0, 13);
            rules[on] = !rules[on];
        }
    }
    public IEnumerator Live()
    {
        List<HexTile> tiles = CombatManager.activeWorld.tiles;
        List<int> next = new List<int>();
        List<int> toAdd = new List<int>();
        List<int> glyphed = new List<int>();
        for (int i = 0; i < castedTiles.Count; i++)
        {
            next.Add(castedTiles[i]);
        }
        
        //be alive for a few gens, then give castedTiles
        for (int i = 0; i < generations; i++)
        {
            //start at origin
            //check neighbor's tiletype, height, and state
            foreach (int t in next)
            {
                HexTile ht = tiles[t];
                //find out value of this tile in next gen
                //check neighbors
                int sum = 0;
                foreach (int n in ht.neighbors)
                {
                    HexTile nt = tiles[n];
                    if (nt.generation > ht.generation)
                    {
                        sum += 2;
                    }
                    if (nt.generation == ht.generation)
                    {
                        sum += 1;
                    }
                    //no need to write the += 0 case
                    toAdd.Add(n);
                }
                if (rules[sum])
                {
                    if (ht.generation == World.zeroState)
                    {
                        ht.generation = World.oneState;
                        ht.ChangeType(ht.type);
                    }
                    if (ht.generation == World.oneState)
                    {
                        ht.generation = glyph;
                        glyphed.Add(ht.index);
                        ht.ChangeType(ht.type);
                    }
                    if (ht.generation == glyph)
                    {
                        //glyph next step
                        ht.generation = World.oneState;
                        glyphed.Remove(ht.index);
                        ht.ChangeType(ht.type);
                    }
                }
                
            }
            for (int n = 0; n < toAdd.Count; n++)
            {
                if (!next.Contains(toAdd[n]))
                {
                    next.Add(toAdd[n]);
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
        //cast spells on glyphs
        foreach (int t in glyphed)
        {
            RuneHex rh = new WaterAttackI();// spells.SingleTargetHexes[glyph];
                                            //castedtiles test
            List<int> c = new List<int>();
            c.Add(t);
            rh.Initialize(c);
            //Debug.Log("getting here");
            rh.Cast(t);
            yield return null;
        }
        yield return null;
    }
}
public class WaterBurstI : LifeHex
{
    public override void Initialize(List<int> casted)
    {
        glyph = 5;
        generations = 3;
        //effect = (GameObject)Resources.Load("Effects/Water/Worb");
        amplitude = 1;
        castTime = 0.2f;
        cooldown = 1;
        manaCost = 1;
        castedTiles = casted;
        //test
        ID = new byte[32] {0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31};
        Seed();
    }
    public override IEnumerator Cast(int origin)
    {
        castedTiles.Add(origin);
        WorldManager.activeWorld.tiles[origin].generation = glyph;
        IEnumerator livingHex = Live();
        yield return livingHex;
    }
}
public class WaterAttackI : SingleTargetHex{
	public override void Initialize(List<int> casted)
	{
		//effect = (GameObject)Resources.Load("Effects/Water/Worb");
		castedTiles = casted;
		amplitude = 1;
		castTime = 0.2f;
		cooldown = 1;
		manaCost = 1;
	}
	public override IEnumerator Cast(int origin) 
	{
        InstantiateAboveTile(origin, 0f, Vector3.one);
        IEnumerator r = MoveUpScaleUpSpin(2f, 1f, new Vector3(6f,6f,6f));
        yield return r;
        
        //RFX1_TransformMotion tm = instance.GetComponentInChildren<RFX1_TransformMotion>();
        //tm.enabled = true;
        
        yield return null;
	} 
}


