using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StarData{
    public float hip;
    public float dist;
    public float mag;
    public float absmag;
    public string spect;

    public Vector3 position;
    public StarDataMonobehaviour associatedObject;
}
