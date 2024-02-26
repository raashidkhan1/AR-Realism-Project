using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShadowPlane : MonoBehaviour {
    
    public Transform cubeTransform;
     public GameObject shadowPlane;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update() {
        var position = cubeTransform.position;
        shadowPlane.transform.position = new Vector3(position.x, position.y - 0.1f, position.z);
    }
}
