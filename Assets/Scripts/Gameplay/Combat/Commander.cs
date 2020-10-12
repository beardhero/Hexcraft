using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// === Helper Classes ===
public class PlayerStatus
{
  //public Queue<Command> pendingCommands;
}

// === Commander ===
public class Commander : MonoBehaviour
{
  public Transform trans;
  public PlayerStatus status;
  public Skirmish activeSkirmish;

  private void Start() {
    trans = transform;  //   Caching transforms is essential when they're being called every Update
  }
}