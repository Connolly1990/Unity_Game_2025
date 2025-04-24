using UnityEngine;

public class BladeScript : MonoBehaviour
{
    public float slicingSpeedThreshold = 1.0f; // Minimum speed for slicing
    private Vector3 previousPosition;
    public Vector3 bladeDirection { get; private set; }
    public GameObject scoreHold; //Connect to score object
    public ScoreHolder script; //Access the score variables

    private void OnTriggerEnter(Collider other)//NOT WORKING YET
    {
        // Check if the object is tagged as breakable
        if (other.CompareTag("Breakable"))
        {
            script = scoreHold.GetComponent<ScoreHolder>(); //Access all variables in Score Holder
            script.comboMeter = script.comboMeter + 1; //Combo goes up when you hit something. Doesn't go down when you miss a hit yet
            script.scoreNumber = script.scoreNumber + 1 * script.comboMulti; //Increases score by 1, multiplied by current combo multiplier
        }
    }

    void Update()
    {
        // Calculate blade direction and update position
        bladeDirection = (transform.position - previousPosition).normalized;
        previousPosition = transform.position;
    }

    public bool IsSlicing()
    {
        float speed = (transform.position - previousPosition).magnitude / Time.deltaTime;
        return speed > slicingSpeedThreshold;
    }
}
