using UnityEngine;
using EzySlice;

public class VRMeshSlicer : MonoBehaviour
{
    public Transform blade; // Reference to the VR controller's transform (blade)
    public Material crossSectionMaterial; // Material for the sliced surfaces
    public float destructionDelay = 5f; // Time (in seconds) before the sliced pieces are destroyed
    public GameObject scoreHold; //Connect to score object
    public ScoreHolder script; //Access the score variables

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object is tagged as sliceable
        if (other.CompareTag("Sliceable"))
        {
            SliceObject(other.gameObject);
        }
    }

    private void SliceObject(GameObject target)
    {
        // Perform slicing using EzySlice
        SlicedHull slicedObject = target.Slice(blade.position, blade.up, crossSectionMaterial);

        script = scoreHold.GetComponent<ScoreHolder>(); //Access all variables in Score Holder
        script.comboMeter = script.comboMeter + 1; //Combo goes up when you hit something. Doesn't go down when you miss a hit yet
        script.scoreNumber = script.scoreNumber + 1 * script.comboMulti; //Increases score by 1, multiplied by current combo multiplier

        if (slicedObject != null)
        {
            // Create the upper and lower hulls of the sliced object
            GameObject upperHull = slicedObject.CreateUpperHull(target, crossSectionMaterial);
            GameObject lowerHull = slicedObject.CreateLowerHull(target, crossSectionMaterial);

            // Add physics to the sliced parts
            AddPhysics(upperHull);
            AddPhysics(lowerHull);

            // Ignore collision with terrain for sliced parts
            IgnoreCollisionWithTerrain(upperHull);
            IgnoreCollisionWithTerrain(lowerHull);

            // Schedule destruction of sliced parts
            Destroy(upperHull, destructionDelay);
            Destroy(lowerHull, destructionDelay);

            // Destroy the original object after slicing
            Destroy(target);
        }
    }

    private void AddPhysics(GameObject obj)
    {
        // Add a Mesh Collider and Rigidbody to the sliced parts
        MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
        meshCollider.convex = true;
        obj.AddComponent<Rigidbody>();
    }

    private void IgnoreCollisionWithTerrain(GameObject obj)
    {
        // Ignore collision with all objects tagged as "Terrain"
        Collider objCollider = obj.GetComponent<Collider>();
        if (objCollider != null)
        {
            GameObject[] terrains = GameObject.FindGameObjectsWithTag("Terrain");
            foreach (GameObject terrain in terrains)
            {
                Collider terrainCollider = terrain.GetComponent<Collider>();
                if (terrainCollider != null)
                {
                    Physics.IgnoreCollision(objCollider, terrainCollider);
                }
            }
        }
    }
}
