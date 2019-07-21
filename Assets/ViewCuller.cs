using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewCuller : MonoBehaviour
{
    MeshRenderer meshRenderer;
    MeshCollider meshCollider;
    //int numVis;
    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        
    }
    void OnBecameInvisible()
    {
        if (meshRenderer == null || meshCollider == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }
        meshRenderer.enabled = false;
        meshCollider.enabled = false;
        //numVis--;
        //Debug.Log(numVis);
    }

    void OnBecameVisible()
    {
        if (meshRenderer == null || meshCollider == null)
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshCollider = GetComponent<MeshCollider>();
        }
        meshRenderer.enabled = true;
        meshCollider.enabled = true;
        //numVis++;
        //Debug.Log(numVis);
    }
}
