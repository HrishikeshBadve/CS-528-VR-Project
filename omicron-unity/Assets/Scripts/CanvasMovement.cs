using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasMovement : MonoBehaviour
{
    public Transform player;
    public Transform camcam;

    // Update is called once per frame
    void Update()
    {
        transform.position = player.position;
        transform.rotation=camcam.rotation;
        
    }
}
