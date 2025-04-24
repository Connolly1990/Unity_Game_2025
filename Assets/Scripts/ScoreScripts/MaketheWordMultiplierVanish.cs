using TMPro;
using UnityEngine;

public class MaketheWordMultiplierVanish : MonoBehaviour
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
        if (comboMulti.comboMulti == 1) //want to make it so the multiplier dissappears if no combo is going
        {
            text.text = " ";
        }
        else
        {
            text.text = "Multiplier:";
        }
    }
}
