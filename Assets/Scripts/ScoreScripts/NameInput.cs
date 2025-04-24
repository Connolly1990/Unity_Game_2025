using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;

public class NameInput : MonoBehaviour
{
    //[SerializeField] JSON_Highscore highScore;
    [SerializeField] TextMeshProUGUI text; //will write name here
    [SerializeField] int nameNumber; //Name is calculated on a scale of 010101 to 262626
    [SerializeField] int bigNumber; //For calculating changes made to the big number
    [SerializeField] int midNumber; //For calculating changes made to the middle number
    [SerializeField] int smallNumber; //For calculating changes made to the smaller number
    public string yourName; //Player's name

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        nameNumber = 10101;
        bigNumber = 10000;
        midNumber = 100;
        smallNumber = 1;
    }

    public void UpFirst()
    {
        nameNumber = nameNumber + 10000;
        bigNumber = bigNumber + 10000;
        if(nameNumber >= 270000)
        {
            nameNumber = nameNumber - 250000;
            bigNumber = bigNumber - 250000;
        }
    }

    public void DownFirst()
    {
        nameNumber = nameNumber - 10000;
        bigNumber = bigNumber - 10000;
        if (nameNumber < 10000)
        {
            nameNumber = nameNumber + 260000;
            bigNumber = bigNumber + 260000;
        }
    }

    public void UpSecond()
    {
        nameNumber = nameNumber + 100;
        midNumber = midNumber + 100;
        if (midNumber >= 2700)
        {
            nameNumber = nameNumber - 2500;
            midNumber = midNumber - 2500;
        }
    }

    public void DownSecond()
    {
        nameNumber = nameNumber - 100;
        midNumber = midNumber - 100;
        if (midNumber < 100)
        {
            nameNumber = nameNumber + 2600;
            midNumber = midNumber + 2600;
        }
    }

    public void UpThird()
    {
        nameNumber = nameNumber + 1;
        smallNumber = smallNumber + 1;
        if (smallNumber >= 27)
        {
            nameNumber = nameNumber - 25;
            smallNumber = smallNumber - 25;
        }
    }

    public void DownThird()
    {
        nameNumber = nameNumber - 1;
        smallNumber = smallNumber + 1;
        if (smallNumber < 1)
        {
            nameNumber = nameNumber + 26;
            smallNumber = smallNumber + 26;
        }
    }

    // Update is called once per frame
    void Update()
    {
        yourName = GetNameFromNumber(nameNumber); // O_O
        text.text = yourName.ToString();
    }

    string GetNameFromNumber(int number) // It works!
    {
        // Extract the three parts of the nameNumber: big, mid, small
        int bigPart = (number / 10000) % 100;
        int midPart = (number / 100) % 100;
        int smallPart = number % 100;

        // Convert each part to a letter (1 -> A, 26 -> Z)
        char bigChar = (char)('A' + (bigPart - 1));
        char midChar = (char)('A' + (midPart - 1));
        char smallChar = (char)('A' + (smallPart - 1));

        // Combine the characters into a string
        return $"{bigChar}{midChar}{smallChar}";
    }
}
