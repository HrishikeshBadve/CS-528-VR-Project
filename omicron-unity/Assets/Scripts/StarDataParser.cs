using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarDataParser : MonoBehaviour
{
    public TextAsset dataSource;
    public GameObject starPrefab;

    public int numberOfStarsToGenerate = 107546;
    public float magnitudeToScaleRatio = 1.0f;

    public List<StarData> starList = new List<StarData>();
    // Start is called before the first frame update
    void Start()
    {
        ParseData();

        GenerateStars();

        GenerateConstellationApus();
        GenerateConstellationVirgo();
    }

    void ParseData()
    {
        string fullText = dataSource.text;
        string[] TextLines = fullText.Split('\n');

        bool firstLineParsed = false;
        foreach(string line in TextLines)
        {
            if(firstLineParsed)
            {
                string[] values = line.Split(',');
                StarData star = new StarData();
                try
                {
                    star.dist = float.Parse(values[1]);
                    if(star.dist==0.0f)
                    {
                        continue;
                    }
                    star.hip=float.Parse(values[0]);
                    star.mag = float.Parse(values[5]);
                    star.absmag = float.Parse(values[6]);
                    star.spect = values[10];
                    star.position = new Vector3(float.Parse(values[2]),float.Parse(values[3]),float.Parse(values[4]));
                    starList.Add(star);
                }
                catch(System.IndexOutOfRangeException)
                {
                    continue;
                }
            }
            else
            {
                firstLineParsed=true;  
            }
        }
        
        //Sort star list by relativeMagnitude
        starList.Sort((x,y)=> y.mag.CompareTo(x.mag));
    }

    void GenerateStars()
    {
        for (int i = 0; i < numberOfStarsToGenerate; i++)
        {
            GameObject instance = Instantiate(starPrefab, starList[i].position, Quaternion.LookRotation(starList[i].position)) as GameObject;
            StarDataMonobehaviour starMonobehaviour = instance.GetComponent<StarDataMonobehaviour>();
            starMonobehaviour.data = starList[i];
        
            // Set the associatedObject property in StarData
            starList[i].associatedObject = starMonobehaviour;
        
            instance.transform.localScale *= magnitudeToScaleRatio * starList[i].absmag;

            // Debug log statement
            Debug.Log($"Star {i}, Hipparcos: {starList[i].hip}, associatedObject: {starList[i].associatedObject}");
        }
    }

    void GenerateConstellationApus()
    {
        float[] starhip = {72370f, 81065f, 81852f};

        for (int i = 0; i < starhip.Length - 1; i++)
        {
            StarData star1 = starList.Find(s => s.hip == starhip[i]);
            StarData star2 = starList.Find(s => s.hip == starhip[i + 1]);

            if (star1 != null && star2 != null)
            {
                StarDataMonobehaviour starMonobehaviour1 = star1.associatedObject;
                StarDataMonobehaviour starMonobehaviour2 = star2.associatedObject;

                if (starMonobehaviour1 != null && starMonobehaviour2 != null)
                {
                    // Change color
                    starMonobehaviour1.GetComponent<MeshRenderer>().material.color = Color.red;
                    starMonobehaviour2.GetComponent<MeshRenderer>().material.color = Color.red;

                    // Change size (adjust the multiplier as needed)
                    starMonobehaviour1.transform.localScale *= 1.5f;
                    starMonobehaviour2.transform.localScale *= 1.5f;

                    // Create a line renderer
                    LineRenderer lineRenderer = starMonobehaviour1.gameObject.AddComponent<LineRenderer>();
                    lineRenderer.positionCount = 2;
                    lineRenderer.SetPosition(0, starMonobehaviour1.transform.position);
                    lineRenderer.SetPosition(1, starMonobehaviour2.transform.position);
                    //lineRenderer.material = new Material(Shader.Find("Standard"));
                    //lineRenderer.material.color = Color.red;
                    //lineRenderer.startColor = Color.red;
                    //lineRenderer.endColor = Color.red;
                    lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    lineRenderer.startColor = Color.red;
                    lineRenderer.endColor = Color.red;
                    lineRenderer.startWidth = 0.05f;
                    lineRenderer.endWidth = 0.05f;
                }
                else
                {
                    Debug.LogError($"Hipparcos caalog numbers {starhip[i]} and {starhip[i + 1]} found, but associatedObject not set in StarDataMonobehaviour.");
                }
            }
            else
            {
                Debug.LogError($"Hipparcos catalog numbers {starhip[i]} and {starhip[i + 1]} not found in starList.");
            }
        }
    }

    void GenerateConstellationVirgo()
    {
        float[] starhip = {57380f, 60129f, 60129f, 61941f, 61941f, 65474f, 65474f, 69427f, 69427f, 69701f, 69701f, 71957f, 65474f, 66249f, 66249f, 68520f, 68520f, 72220f, 66249f, 63090f, 63090f, 63608f, 63090f, 61941f };
        
    }


    // Update is called once per frame
    void Update()
    {  

    }
}
