using UnityEngine;
using TMPro;

public class combometer : MonoBehaviour //This script controls the part of the UI that shows how many consectuctive hits you've gotten
{
    [SerializeField] ScoreHolder comboMeter; //getting the current combo number from the ScoreHolder
    [SerializeField] TextMeshPro text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = comboMeter.comboMeter.ToString();
    }
}
