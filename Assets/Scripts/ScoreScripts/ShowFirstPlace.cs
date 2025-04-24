using UnityEngine;
using TMPro;

public class ShowFirstPlace : MonoBehaviour
{
    [SerializeField] JSON_Highscore highScore; //Remebers the highest score from all previous play sessions
    [SerializeField] TextMeshProUGUI first; //will show first place name here
    [SerializeField] TextMeshProUGUI firsts; //will show first place score here
    [SerializeField] TextMeshProUGUI second; //will show second place name here
    [SerializeField] TextMeshProUGUI seconds; //will show second place score here
    [SerializeField] TextMeshProUGUI third; //will show third place name here
    [SerializeField] TextMeshProUGUI thirds; //will show third place score here


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        /*
        first = GetComponent<TextMeshProUGUI>();
        firsts = GetComponent<TextMeshProUGUI>();
        second = GetComponent<TextMeshProUGUI>();
        seconds = GetComponent<TextMeshProUGUI>();
        third = GetComponent<TextMeshProUGUI>();
        thirds = GetComponent<TextMeshProUGUI>();
        */
    }



    // Update is called once per frame
    void Update()
    {
        first.text = highScore.scoreData.playerName.ToString();
        firsts.text = highScore.scoreData.highScore.ToString();
        second.text = highScore.scoreData.secondName.ToString();
        seconds.text = highScore.scoreData.secondScore.ToString();
        third.text = highScore.scoreData.thirdName.ToString();
        thirds.text = highScore.scoreData.thirdScore.ToString();
    }
}
