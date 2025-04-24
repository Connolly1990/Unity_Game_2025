using UnityEngine;

[CreateAssetMenu(fileName = "HighScore", menuName = "Dingle/HighScore")]
public class HighScore_SO : ScriptableObject
{
    public int highScore;
    public string playerName;
    public int secondScore;
    public string secondName;
    public int thirdScore;
    public string thirdName;
}
