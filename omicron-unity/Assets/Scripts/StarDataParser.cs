using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
public class StarDataParser : MonoBehaviour
{
    private Dictionary<int, StarData> starDict = new Dictionary<int, StarData>();
    public TextAsset dataSource;
    //public GameObject player;
    public GameObject starPrefab;
    public TextMeshProUGUI distanceText;

    public int numberOfStarsToGenerate = 107546;
    public float speed = 10.0f;
    public float magnitudeToScaleRatio = 1.0f;
    public bool ChangeColors = true;

    public GameObject camcam;
    private string currentConstellationGroup = "modern"; // default 

    public List<StarData> starList = new List<StarData>();

    private TextAsset exoplanetDataSource;
    
    private Dictionary<int, int> exoplanetDict = new Dictionary<int, int>();
    private Dictionary<int, Color> initialStarColors = new Dictionary<int, Color>();
    public static Vector3 InitialPos;
    public static Quaternion InitialRot;

    // public Text distanceText;

    private GameObject constellationParent;
    private Dictionary<int, Vector3> initialStarScales = new Dictionary<int, Vector3>();
    
    void Start()
    {

        // InitializePlayerAndUI();

        exoplanetDataSource = Resources.Load<TextAsset>("cleaned_exoplanet_data");
        ParseData();

        ParseExoplanetData();

        GenerateStars();

        GenerateConstellations();

        InitialPos = camcam.transform.position;
        InitialRot = camcam.transform.rotation;

    }


    void ParseData()
    {
        string fullText = dataSource.text;
        string[] TextLines = fullText.Split('\n');

        bool firstLineParsed = false;
        //foreach(string line in TextLines)
        for(int i=0;i<numberOfStarsToGenerate;i++)
        {
            if(firstLineParsed)
            {
                string[] values = TextLines[i].Trim().Split(',');
                StarData star = new StarData();
                try
                {
                    star.dist = float.Parse(values[1]);
                    if(star.dist==0.0f)
                    {
                        continue;
                    }
                    star.hip = (int)float.Parse(values[0]);
                    star.mag = float.Parse(values[5]);
                    star.absmag = float.Parse(values[6]);
                    star.spect = values[10];
                    star.position = new Vector3(float.Parse(values[2]),float.Parse(values[3]),float.Parse(values[4]));
                    starList.Add(star);
                    starDict.Add(star.hip, star);
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
        foreach(StarData starData in starList)
        {
            GameObject instance = Instantiate(starPrefab, starData.position, Quaternion.LookRotation(starData.position));

            Color starColor;
            switch (starData.spect[0]) 
            {
                case 'O':
                    starColor = Color.blue;
                    break;
                case 'B':
                    starColor = new Color(0.67f, 0.89f, 1f);  // Bluish white
                    break;
                case 'A':
                    starColor = Color.white;
                    break;
                case 'F':
                    starColor = new Color(1f, 1f, 0.75f);  // Yellowish white
                    break;
                case 'G':
                    starColor = Color.yellow;
                    break;
                case 'K':
                    starColor = new Color(1f, 0.65f, 0.35f);  // Light orange
                    break;
                case 'M':
                    starColor = new Color(1f, 0.55f, 0.41f);  // Orangish red
                    break;
                default:
                    starColor = Color.white;
                    break;
            }
            instance.GetComponent<MeshRenderer>().material.color = starColor;
            initialStarColors[starData.hip] = starColor;

            LineRenderer lineRenderer = instance.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = instance.AddComponent<LineRenderer>();
            }
            lineRenderer.material.color = Color.white;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;

            starData.starObject = instance;
            StarDataMonobehaviour starMonobehaviour = instance.GetComponent<StarDataMonobehaviour>();
            starMonobehaviour.data = starData;

            instance.transform.localScale *= magnitudeToScaleRatio * starData.absmag;

            // Store the initial scale of the star
            initialStarScales[starData.hip] = instance.transform.localScale;
        }
        //ChangeStarColors();
    }


    public void ChangeStarColors()
    {
        foreach (StarData starData in starList)
        {
            GameObject instance = starData.starObject;

            if (ChangeColors)
            {
                if (exoplanetDict.ContainsKey(starData.hip))
                {
                    int numPlanets = exoplanetDict[starData.hip];
                    Color starColor;
                    switch (numPlanets)
                    {
                        case 1:
                            starColor = Color.red;
                            break;
                        case 2:
                            starColor = Color.blue;
                            break;
                        case 3:
                            starColor = Color.green;
                            break;
                        case 4:
                            starColor = Color.yellow;
                            break;
                        case 5:
                            starColor = Color.magenta;
                            break;
                        case 6:
                            starColor = Color.cyan;
                            break;
                        default:
                            starColor = Color.white;
                            break;
                    }
                    instance.GetComponent<MeshRenderer>().material.color = starColor;
                }
                else
                {
                    instance.GetComponent<MeshRenderer>().material.color = Color.white;
                }
                
            }
            else
            {
                if (initialStarColors.ContainsKey(starData.hip))
                {
                    instance.GetComponent<MeshRenderer>().material.color = initialStarColors[starData.hip];
                }
                
            }
        }
        ChangeColors = !ChangeColors;
    }

    public void ChangeConstellationGroup(string newGroup)
    {
        currentConstellationGroup = newGroup;
        GenerateConstellations();
    }
    

    public void GenerateConstellations()
    {
        // Destroy the old constellations
        if (constellationParent != null)
        {
            Destroy(constellationParent);
        }

        // Create a new parent GameObject for the constellations
        constellationParent = new GameObject("Constellations");

        TextAsset constellationFile = Resources.Load<TextAsset>(currentConstellationGroup + "_constellationship");
        if (constellationFile != null)
        {
            string[] lines = constellationFile.text.Split('\n');
            foreach (string line in lines)
            {
                string[] starhip = line.Trim().Split(' ');
                List<int> hipPairs = new List<int>();
                for(int i=2;i<starhip.Length;i++)
                {
                    hipPairs.Add(int.Parse(starhip[i]));
                }
                for (int i = 1; i < hipPairs.Count; i += 2)
                {
                    int hip1 = hipPairs[i];
                    int hip2 = hipPairs[i+1];

                    if (starDict.ContainsKey(hip1) && starDict.ContainsKey(hip2))
                    {
                        GameObject star1 = starDict[hip1].starObject;
                        GameObject star2 = starDict[hip2].starObject;
                        StarDataMonobehaviour starMonobehaviour1 = star1.GetComponent<StarDataMonobehaviour>();
                        StarDataMonobehaviour starMonobehaviour2 = star2.GetComponent<StarDataMonobehaviour>();

                        if (starMonobehaviour1 != null && starMonobehaviour2 != null)
                        {
                            // Reset the scales of the stars to their initial values
                            if (initialStarScales.ContainsKey(hip1) && initialStarScales.ContainsKey(hip2))
                            {
                                starMonobehaviour1.transform.localScale = initialStarScales[hip1];
                                starMonobehaviour2.transform.localScale = initialStarScales[hip2];
                            }

                            GameObject constellation = new GameObject();
                            LineRenderer lineRenderer = constellation.AddComponent<LineRenderer>();

                            // Change color
                            //starMonobehaviour1.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.5f, 0.0f);
                            //starMonobehaviour2.GetComponent<MeshRenderer>().material.color = new Color(1.0f, 0.5f, 0.0f);

                            // Change size (adjust the multiplier as needed)
                            starMonobehaviour1.transform.localScale *= 1.5f;
                            starMonobehaviour2.transform.localScale *= 1.5f;

                            // Create a line renderer
                            lineRenderer.positionCount = 2;
                            lineRenderer.SetPosition(0, starMonobehaviour1.transform.position);
                            lineRenderer.SetPosition(1, starMonobehaviour2.transform.position);
                            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                            lineRenderer.material.color = Color.white;
                            lineRenderer.startWidth = 0.04f;
                            lineRenderer.endWidth = 0.04f;

                            // Parent the constellation to the constellationParent
                            constellation.transform.parent = constellationParent.transform;
                        }
                        else
                        {
                            Debug.LogError($"Hipparcos catalog numbers {hip1} and {hip2} found, but associatedObject not set in StarDataMonobehaviour.");
                        }
                    }
                    else
                    {
                        Debug.LogError($"Hipparcos catalog numbers {hip1} and {hip2} not found in starList.");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Constellation file not found.");
        }
    }

    void ParseExoplanetData()
    {
        string fullText = exoplanetDataSource.text;
        string[] lines = fullText.Split('\n');

        // Skip the first line (header row)
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Trim().Split(',');
            if (int.TryParse(values[0], out int hip) && int.TryParse(values[1], out int numPlanets))
            {
                exoplanetDict[hip] = numPlanets;
            }
        }
    }

    public void ResetPosition()
    {
        camcam.transform.position = InitialPos;
        camcam.transform.rotation = InitialRot;
    }

    public void OnIndianButtonClick()
    {
        currentConstellationGroup = "indian";
        GenerateConstellations();
    }

    public void OnJapaneseButtonClick()
    {
        currentConstellationGroup = "japanese";
        GenerateConstellations();
    }

    public void OnMaoriButtonClick()
    {
        currentConstellationGroup = "maori";
        GenerateConstellations();
    }

    public void OnNorseButtonClick()
    {
        currentConstellationGroup = "norse";
        GenerateConstellations();
    }

    public void OnSamoanButtonClick()
    {
        currentConstellationGroup = "samoan";
        GenerateConstellations();
    }

    public void OnResetConstellationButtonClick()
    {
        currentConstellationGroup = "modern";
        GenerateConstellations();
    }

    public void OnVirgoButtonClick()
    {
        currentConstellationGroup = "virgo";
        GenerateConstellations();
    }

    public void RemoveConstellations()
    {
        if (constellationParent != null)
        {
            Destroy(constellationParent);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Basic player movement
        // Get the player's input
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");
        float moveUp = Input.GetKey(KeyCode.R) ? 1f : 0f;
        float moveDown = Input.GetKey(KeyCode.F) ? -1f : 0f;

        // Create a movement vector in the player's local space
        Vector3 movement = new Vector3(moveVertical, moveUp + moveDown, moveHorizontal);


        Debug.Log("Camera position: " + camcam.transform.position);

        // Calculate the distance from the camera to Sol in parsecs
        float distanceToSol = Vector3.Distance(camcam.transform.position, Vector3.zero);

        // Update the UI text
        distanceText.text = "Distance to Sol: " + distanceToSol.ToString("F2") + " parsecs";

    }

}
