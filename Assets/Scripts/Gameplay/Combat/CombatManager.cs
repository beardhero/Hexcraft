using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
public class CombatManager : MonoBehaviour
{
  public int roundTimer = 6;
  public static World activeWorld;
  public static CombatManager instance;
  List<Commander> commanders;
  List<Skirmish> skirmishes;
  //int combatThresholdDistance = 10;   // Ten tiles

  public void Initialize(World w)
  {
    instance = this;
    activeWorld = w;
    commanders = new List<Commander>();
    commanders.Add(new Commander());  // test
  }

  void Update()
  {
    if (instance == null || commanders.Count < 1) return;
    
    // Server checks distance between players and hostile units, initiating combat if within combatThresholdDistance
    foreach (Commander c in commanders)
    {
      if (c.activeSkirmish != null) continue;  // This commander already in a fight

      foreach(Commander c2 in commanders)
      {
        if (c2.activeSkirmish != null) continue;  // Commander being checked against already in a fight

        //if ((c.transform.position - c2.transform.position).sqrMagnitude < )
      }
      // If within distancethreshold, check if target is already in a skirmish
      // if so, add this to target's skirmish
      // else, create new skirmish and add both parties
    }
  }
}