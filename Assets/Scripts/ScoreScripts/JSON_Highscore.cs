using UnityEngine;

[System.Serializable]
public class ScoreData
{
    public int highScore = 4;
    public string playerName = "JOE";
    public int secondScore = 2;
    public string secondName = "EGG";
    public int thirdScore = 1;
    public string thirdName = "OOP";
}

public class JSON_Highscore : MonoBehaviour
{
    public ScoreData scoreData;

    void Start()
    {
        // Example of loading JSON from a file
        string json = System.IO.File.ReadAllText("JSON_Highscore.json");
        scoreData = JsonUtility.FromJson<ScoreData>(json);

        // Example of saving JSON to a file
        string jsonToSave = JsonUtility.ToJson(scoreData);
        System.IO.File.WriteAllText("scoreData.json", jsonToSave);
    }
}
