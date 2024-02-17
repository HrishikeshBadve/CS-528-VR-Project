using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarDataParser : MonoBehaviour
{
    public TextAsset dataSource;
    public GameObject starPrefab;

    public int numberOfStarsToGenerate = 100000;
    public float magnitudeToScaleRatio = 1.0f;

    public List<StarData> starList = new List<StarData>();
    // Start is called before the first frame update
    void Start()
    {
        ParseData();

        GenerateStars();
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
        for(int i=0; i<numberOfStarsToGenerate;i++)
        {
            GameObject instance=Instantiate(starPrefab,starList[i].position,Quaternion.LookRotation(starList[i].position)) as GameObject;
            instance.GetComponent<StarDataMonobehaviour>().data = starList[i];
            instance.transform.localScale *= magnitudeToScaleRatio*starList[i].absmag;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
