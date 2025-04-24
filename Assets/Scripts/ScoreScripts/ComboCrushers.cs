using UnityEngine;

public class ComboCrushers : MonoBehaviour //Script for when a box is missed, to reset combo
{
    public GameObject scoreHold; //Connect to score object
    public ScoreHolder script; //Access the score variables
    


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is either kind of box
        if (other.CompareTag("Sliceable"))
        {
            Destroy(other.gameObject); //Destroy box
            script = scoreHold.GetComponent<ScoreHolder>(); //Access all variables in Score Holder
            script.comboMeter = script.comboMeter = 0; //Combo reset
        }
        if (other.CompareTag("Breakable"))
        {
            Destroy(other.gameObject); //Destroy box
            script = scoreHold.GetComponent<ScoreHolder>(); //Access all variables in Score Holder
            script.comboMeter = script.comboMeter = 0; //Combo reset
        }
    }
}
