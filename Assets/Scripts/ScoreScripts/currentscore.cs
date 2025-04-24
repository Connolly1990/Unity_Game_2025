using UnityEngine;
using TMPro;

public class currentscore : MonoBehaviour //This script controls the part of the UI that shows your current score
{
    [SerializeField] ScoreHolder scoreNumber; //getting the current score number from the ScoreHolder
    [SerializeField] TextMeshPro text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = scoreNumber.scoreNumber.ToString();
    }
}
