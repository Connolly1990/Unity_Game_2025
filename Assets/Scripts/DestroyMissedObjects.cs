using UnityEngine;

public class DestroyMissedObjects : MonoBehaviour
{
    public GameObject scoreHold; //Connect to score object
    public ScoreHolder script; //Access the score variables
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object has the tag "Breakable" or "Sliceable"
        if (other.CompareTag("Breakable") || other.CompareTag("Sliceable"))
        {
            // Destroy the object
            Destroy(other.gameObject);
            script = scoreHold.GetComponent<ScoreHolder>(); //Access all variables in Score Holder
            script.comboMeter = script.comboMeter = 0; //Combo reset
        }
    }
}
