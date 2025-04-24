using UnityEngine;
using UnityEngine.Events;

public class TriggerEvents : MonoBehaviour
{
    [SerializeField] string useTag;
    [SerializeField] UnityEvent triggerEnter;
    [SerializeField] UnityEvent triggerStay;
    [SerializeField] UnityEvent triggerExit;



    private void OnTriggerEnter(Collider other)
    {
        if(useTag != "" && other.CompareTag(useTag))
        {
            triggerEnter.Invoke();
        }
        else if(useTag == "")
        {
            triggerEnter.Invoke();
        }
    }
}
