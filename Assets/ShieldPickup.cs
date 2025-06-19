using UnityEngine;

public class ShieldPickup : MonoBehaviour
{
    public int shieldHits = 2;

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.ActivateShield(shieldHits);
            Destroy(gameObject); // Remove shield pickup from scene
        }
    }
}
