using UnityEngine;

public class ScoreHolder : MonoBehaviour
{//This Script has all the variables for the score, combo and multiplier variables.

    public int scoreNumber = 0; //The amount of points you've gotten from hitting cubes
    public int comboMeter = 0; //The amount of cubes you've hit consecutively
    public int comboMulti = 1; //The amount your score is multiplied per cube borken

    //[SerializeField] HighScore_SO highScore; //Remebers the highest score from all previous play sessions
    [SerializeField] JSON_Highscore highScore; //Remebers the highest score from all previous play sessions
    [SerializeField] NameInput whoAreYou; //Remebers the current player's name
    private int oldHighScore;
    private int oldSecondScore;
    private int oldThirdScore;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //This script is accessed by the Axe's "ProceduralMeshSlicer" script,
        oldHighScore = highScore.scoreData.highScore;//the previous highest score is the highscore
        oldSecondScore = highScore.scoreData.secondScore;//the previous 2nd highest score is the second highest score
        oldThirdScore = highScore.scoreData.thirdScore;//the previous 3rd highest score is the third highest score
    }
    
    // Update is called once per frame
    void Update()//Controls the combo multiplier
    {
        if (comboMeter < 5) //checking that combo is less than 5
        {
            comboMulti = 1; //score gets multiplied by 1
        }
        else if (comboMeter > 4 && comboMeter < 10) //if score is more than 4 and less than 10 (5-9)
        {
            comboMulti = 2; //score multiplier is now 2
        }
        else if(comboMeter > 9 && comboMeter < 15) //if score is between 10 and 14
        {
            comboMulti = 4; //score multiplier is now 4
        }
        else if (comboMeter >= 15) //from a combo of 15 onwards
        {
            comboMulti = 8; //score multiplier is 8
        }
    
        if (scoreNumber >= oldHighScore) //If old highscore islower than the current score
        {
            highScore.scoreData.thirdScore = highScore.scoreData.secondScore; //the old second place is moved to third
            highScore.scoreData.thirdName = highScore.scoreData.secondName;
            highScore.scoreData.secondScore = highScore.scoreData.highScore; //the old highest score is now second
            highScore.scoreData.secondName = highScore.scoreData.playerName;
            highScore.scoreData.highScore = scoreNumber; //the new highcsore is the current score
            highScore.scoreData.playerName = whoAreYou.yourName;
        }
        else if (scoreNumber >= oldSecondScore && scoreNumber <= oldHighScore)
        {
            highScore.scoreData.thirdScore = highScore.scoreData.secondScore; //the old second place is moved to third
            highScore.scoreData.thirdName = highScore.scoreData.secondName;
            highScore.scoreData.secondScore = scoreNumber; //the new second place score replaces the old one
            highScore.scoreData.secondName = whoAreYou.yourName;
        }
        else if (scoreNumber >= oldThirdScore && scoreNumber <= oldSecondScore)
        {
            highScore.scoreData.thirdScore = scoreNumber; //the new third place score replaces the old one
            highScore.scoreData.thirdName = whoAreYou.yourName;
        }
    }
}
