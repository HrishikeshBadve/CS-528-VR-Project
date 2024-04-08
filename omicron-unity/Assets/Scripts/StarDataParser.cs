using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
public class StarDataParser : MonoBehaviour
{
    // Define a new struct to represent a constellation
    public struct Constellation
    {
        public GameObject constellationObject;
        public StarData star1;
        public StarData star2;
    }
    // Add a list to store the constellations
    public float velocityMultiplier = 1.0f;
    public List<Constellation> constellations = new List<Constellation>();
    private Dictionary<int, StarData> starDict = new Dictionary<int, StarData>();
    public TextAsset dataSource;
    public bool isStarMovementPaused = true;
    //private int frameCounter = 0;
    public GameObject starPrefab;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI timeText;
    private Dictionary<int, Vector3> originalVelocities = new Dictionary<int, Vector3>();
    private Dictionary<int, Vector3> originalPositions = new Dictionary<int, Vector3>();
    public int numberOfStarsToGenerate = 107546;  
    public float magnitudeToScaleRatio = 1.0f;
    public bool ChangeColors = true;
    public int frameCounter = 0;
    
    public Vector3 lastStarGenerationPosition = Vector3.zero;
    public GameObject camcam;
    private string currentConstellationGroup = "modern"; // default 

    public List<StarData> starList = new List<StarData>();

    private TextAsset exoplanetDataSource;
    public int isVelNegative = 1;
    private Dictionary<int, int> exoplanetDict = new Dictionary<int, int>();
    private Dictionary<int, Color> initialStarColors = new Dictionary<int, Color>();
    public static Vector3 InitialPos;
    public static Quaternion InitialRot;
    public float elapsedTime = 0f;
    

    // public Text distanceText;

    private GameObject constellationParent;
    private Dictionary<int, Vector3> initialStarScales = new Dictionary<int, Vector3>();
    
    void Start()
    {
        isStarMovementPaused = true;
        exoplanetDataSource = Resources.Load<TextAsset>("cleaned_exoplanet_data");
        ParseData();

        ParseExoplanetData();

        GenerateStars();

        GenerateConstellations();

        InvokeRepeating("RotateStars", 0f, 0.25f);

        InitialPos = camcam.transform.position;
        InitialRot = camcam.transform.rotation;

        foreach (StarData starData in starList)
        {
            originalVelocities[starData.hip] = starData.velocity;
            originalPositions[starData.hip] = starData.position;
        }

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
                    star.velocity = new Vector3(float.Parse(values[7]),float.Parse(values[8]),float.Parse(values[9]));
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

    public HashSet<int> GetStarsInConstellations()
    {
        HashSet<int> starsInConstellations = new HashSet<int>();
        TextAsset constellationFile = Resources.Load<TextAsset>("all_constellationship");
        if (constellationFile != null)
        {
            string[] lines = constellationFile.text.Split('\n');
            foreach (string line in lines)
            {
                string[] starhip = line.Trim().Split(' ');
                for(int i=2; i<starhip.Length; i++)
                {
                    if (int.TryParse(starhip[i], out int hip))
                    {
                        starsInConstellations.Add(hip);
                    }
                    else
                    {
                        Debug.LogError("Failed to parse Hipparcos number: " + starhip[i]);
                    }
                }
            }
        }
        return starsInConstellations;
    }


    void GenerateStarDynamic(StarData starData)
    {
        GameObject instance = Instantiate(starPrefab, starData.position, Quaternion.LookRotation(starData.position));
        MeshRenderer meshRenderer = instance.GetComponent<MeshRenderer>();
        LineRenderer lineRenderer = instance.GetComponent<LineRenderer>() ?? instance.AddComponent<LineRenderer>();

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

        starData.starObject = instance;
        StarDataMonobehaviour starMonobehaviour = instance.GetComponent<StarDataMonobehaviour>();
        starMonobehaviour.data = starData;
    }
    
    public void ToggleStarMovement()
    {
        isStarMovementPaused = !isStarMovementPaused;
    }

    void GenerateStars()
    {
        HashSet<int> starsInConstellations = GetStarsInConstellations();
        Vector3 cameraPosition = camcam.transform.position;  // Get the camera's position

        foreach(StarData starData in starList)
        {
            starData.originalPosition = starData.position;
            float distanceToCamera = Vector3.Distance(starData.position, cameraPosition);
            if (starsInConstellations.Contains(starData.hip) || distanceToCamera <= 50)
            {
                GameObject instance = Instantiate(starPrefab, starData.position, Quaternion.LookRotation(starData.position));

                // Disable shadows
                MeshRenderer meshRenderer = instance.GetComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

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

                

                starData.starObject = instance;
                StarDataMonobehaviour starMonobehaviour = instance.GetComponent<StarDataMonobehaviour>();
                starMonobehaviour.data = starData;

                instance.transform.localScale *= magnitudeToScaleRatio * starData.absmag;

                // Store the initial scale of the star
                initialStarScales[starData.hip] = instance.transform.localScale;
            }
        }
        //ChangeStarColors();
    }


    public void ChangeStarColors()
    {
        HashSet<int> starsInConstellations = GetStarsInConstellations();
        Vector3 cameraPosition = camcam.transform.position;  // Get the camera's position

        foreach (StarData starData in starList)
        {
            float distanceToCamera = Vector3.Distance(starData.position, cameraPosition);
            if (starsInConstellations.Contains(starData.hip) || distanceToCamera <= 50)
            {
                if (starData.starObject == null)
                {
                    GenerateStarDynamic(starData);
                }

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
            constellations.Clear();  // Clear the list
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

                            // Disable shadows
                            lineRenderer.receiveShadows = false;
                            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                            

                            // Change size (adjust the multiplier as needed)
                            starMonobehaviour1.transform.localScale *= 1.5f;
                            starMonobehaviour2.transform.localScale *= 1.5f;

                            // Create a line renderer
                            lineRenderer.positionCount = 2;
                            lineRenderer.SetPosition(0, starMonobehaviour1.transform.position);
                            lineRenderer.SetPosition(1, starMonobehaviour2.transform.position);
                            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                            // Use an Unlit shader
                            //lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
                            lineRenderer.material.color = Color.white;
                            lineRenderer.startWidth = 0.1f;
                            lineRenderer.endWidth = 0.1f;

                            // Parent the constellation to the constellationParent
                            constellation.transform.parent = constellationParent.transform;

                            // Add the constellation to the list
                            constellations.Add(new Constellation
                            {
                                constellationObject = constellation,
                                star1 = starDict[hip1],
                                star2 = starDict[hip2]
                            });
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
        //ResetStarPositions();
        currentConstellationGroup = "indian";
        GenerateConstellations();
    }

    public void OnJapaneseButtonClick()
    {
        //ResetStarPositions();
        currentConstellationGroup = "japanese";
        GenerateConstellations();
    }

    public void OnMaoriButtonClick()
    {
        //ResetStarPositions();
        currentConstellationGroup = "maori";
        GenerateConstellations();
    }

    public void OnNorseButtonClick()
    {
        //ResetStarPositions();
        currentConstellationGroup = "norse";
        GenerateConstellations();
    }

    public void OnSamoanButtonClick()
    {
        //ResetStarPositions();
        currentConstellationGroup = "samoan";
        GenerateConstellations();
    }

    public void OnResetConstellationButtonClick()
    {
        //ResetStarPositions();
        currentConstellationGroup = "modern";
        GenerateConstellations();
    }

    public void OnVirgoButtonClick()
    {
        Vector3 lookAtPosition = new Vector3((float)8.787,(float)5.726,(float)0.655);
        Vector3 virgoPosition = new Vector3((float)-11.489,(float)-2.112,(float)-0.296);
        // Move the camera to focus on the Virgo constellation
        camcam.transform.position = lookAtPosition;
        camcam.transform.LookAt(virgoPosition);
        currentConstellationGroup = "virgo";
        GenerateConstellations(); // Pass in the color cyan
    }



    public void RemoveConstellationsButton()
    {
        //ResetStarPositions();
        if (constellationParent != null)
        {
            Destroy(constellationParent);
        }
        constellations.Clear();
    }
    
    void RotateStars()
    {
        foreach (StarData starData in starList)
        {
            if (starData.starObject != null)
            {
                // Make the star face the camera
                starData.starObject.transform.LookAt(camcam.transform);

                // Rotate the star 180 degrees around the up axis
                starData.starObject.transform.Rotate(0, 180, 0);
            }
        }
    }

    public void ResetStarPositions()
    {
        foreach (StarData starData in starList)
        {
            if (starData.starObject != null)
            {
                starData.position = starData.originalPosition;
                starData.starObject.transform.position = starData.originalPosition;
            }
        } 
        // Reset the positions of the constellation lines
        foreach (Constellation constellation in constellations)
        {
            if (constellation.constellationObject != null)
            {
                LineRenderer lineRenderer = constellation.constellationObject.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // Assuming the constellation struct has references to the start and end stars
                    lineRenderer.SetPosition(0, constellation.star1.originalPosition);
                    lineRenderer.SetPosition(1, constellation.star2.originalPosition);
                }
            }
        }
        elapsedTime=0;
    }


    public void ChangeStarVelocities(float sliderValue)
    {
        // You can adjust this value to get the effect you want
        velocityMultiplier = sliderValue;

        foreach (StarData starData in starList)
        {
            // Multiply the original velocity by the velocity multiplier
            starData.velocity = originalVelocities[starData.hip] * velocityMultiplier * isVelNegative;
        }
    }

    public void ReverseStarVelocities()
    {
        foreach (StarData starData in starList)
        {
            // Negate the velocity
            starData.velocity = -starData.velocity;
            isVelNegative = -isVelNegative;
        }
    }

    public void ChangeStarDistances(float sliderValue)
    {
        // You can adjust this value to get the effect you want
        float distanceMultiplier = sliderValue;

        foreach (StarData starData in starList)
        {
            // Scale the original position by the distance multiplier
            starData.position = originalPositions[starData.hip] * distanceMultiplier;

            // Update the position of the star's GameObject
            if (starData.starObject != null)
            {
                starData.starObject.transform.position = starData.position;
            }
        }

        // Update the positions of the constellation lines
        foreach (Constellation constellation in constellations)
        {
            if (constellation.constellationObject != null)
            {
                LineRenderer lineRenderer = constellation.constellationObject.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    // Assuming the constellation struct has references to the start and end stars
                    lineRenderer.SetPosition(0, constellation.star1.position);
                    lineRenderer.SetPosition(1, constellation.star2.position);
                }
            }
        }
    }


    

    // Update is called once per frame
    void Update()
    {
        frameCounter++;
        if(isVelNegative==1 && !isStarMovementPaused)
        {
            elapsedTime += Time.deltaTime*velocityMultiplier;
        }
        else if(isVelNegative==-1 && !isStarMovementPaused)
        {
            elapsedTime -= Time.deltaTime*velocityMultiplier;
        } 
        
        timeText.text = "Time Elapsed: "+ elapsedTime.ToString("F2");

        // Calculate the distance from the camera to Sol in parsecs
        float distanceToSol = Vector3.Distance(camcam.transform.position, Vector3.zero);

        // Update the UI text
        distanceText.text = "Distance to Sol: " + distanceToSol.ToString("F2") + " parsecs";

        // Check the distance of each star to the camera
        Vector3 cameraPosition = camcam.transform.position;  // Get the camera's position
        float distanceMoved = Vector3.Distance(cameraPosition, lastStarGenerationPosition);
        if (distanceMoved >= 10)
        {
            foreach (StarData starData in starList)
            {
                // Calculate the distance from the star to the camera
                float distanceToCamera = Vector3.Distance(starData.position, cameraPosition);

                // Check if the star is within 50 parsecs of the camera and hasn't been generated yet
                if (distanceToCamera <= 50 && starData.starObject == null)
                {
                    GenerateStarDynamic(starData);
                }
            }
            lastStarGenerationPosition = cameraPosition;
        }

        
        if (!isStarMovementPaused)
        {
            // Update the position of each star
            foreach (StarData starData in starList)
            {
                if (starData.starObject != null)
                {
                    // Update the star's position based on its velocity and the elapsed time since the last frame
                    starData.position += starData.velocity * Time.deltaTime * 1000;

                    // Update the position of the star's GameObject
                    starData.starObject.transform.position = starData.position;
                }
                // Check if the star is part of a constellation
                
            }
            // Update the position of each constellation line
            foreach (Constellation constellation in constellations)
            {
                LineRenderer lineRenderer = constellation.constellationObject.GetComponent<LineRenderer>();
                lineRenderer.SetPosition(0, constellation.star1.position);
                lineRenderer.SetPosition(1, constellation.star2.position);
            }
        }
    }

}
