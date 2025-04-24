using TMPro;
using UnityEngine;

public class combomultiplier : MonoBehaviour //This script controls the part of the UI that shows how much multiplier you're getting from your combo. Highest mult is 8
{
    [SerializeField] ScoreHolder comboMulti; //getting the combo's score multiplier from the ScoreHolder
    [SerializeField] TextMeshPro text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TextMeshPro>();
        text.text = " ";
    }

    // Update is called once per frame
    void Update()
    {
        if(comboMulti.comboMulti == 1) //want to make it so the multiplier dissappears if no combo is going
        {
            text.text = " ";
        }
        else
        {
            text.text = comboMulti.comboMulti.ToString();
        }
    }
}
