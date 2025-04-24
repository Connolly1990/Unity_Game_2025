using UnityEngine;

public class SliceableScript : MonoBehaviour
{
    public GameObject shatteredPiecePrefab; // Prefab for the shattered pieces
    public int numberOfPieces = 9; // Number of shattered pieces to create
    public float explosionForce = 2.0f; // Force applied to each piece
    public float explosionRadius = 1.0f; // Radius of the explosion force
    public float pieceLifetime = 3.0f; // Time after which pieces disappear
    private bool hasBeenShattered = false; // Flag to ensure it shatters only once

    private void OnTriggerEnter(Collider other)
    {
        // Only shatter if tagged as Breakable and hasn't been shattered yet
        if (other.CompareTag("Blade") && !hasBeenShattered)
        {
            ShatterObject();
            hasBeenShattered = true; // Prevent future shattering
        }
    }

    private void ShatterObject()
    {
        // Get the current position of the object
        Vector3 position = transform.position;

        // Create the shattered pieces
        for (int i = 0; i < numberOfPieces; i++)
        {
            // Random position around the object
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f),
                Random.Range(-0.5f, 0.5f)
            );

            // Instantiate a shattered piece
            GameObject piece = Instantiate(shatteredPiecePrefab, position + randomOffset, Random.rotation);

            // Add physics to the piece
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, position, explosionRadius);
            }

            // Disable the colliders on the shattered piece
            Collider[] colliders = piece.GetComponents<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = false;
            }

            // Schedule the piece to be destroyed after a few seconds
            Destroy(piece, pieceLifetime);
        }

        // Destroy the original object
        Destroy(gameObject);
    }
}
