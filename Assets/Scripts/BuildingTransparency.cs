using UnityEngine;
using System.Collections.Generic;

public class BuildingTransparency : MonoBehaviour
{
    public Transform player;  // The camera or the player to detect
    public Material transparentMaterial;  // Transparent material to apply
    public float checkRadius = 5f;  // Radius around the player to check for nearby buildings
    public LayerMask buildingLayer;  // Layer mask to filter buildings

    // Store the original materials of buildings
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    private void LateUpdate()
    {
        // Get all buildings within the check radius
        Collider[] nearbyBuildings = Physics.OverlapSphere(player.position, checkRadius, buildingLayer);

        // First, restore the materials of any building that is out of range
        foreach (var item in originalMaterials)
        {
            if (item.Key != null)
            {
                // Restore original material
                item.Key.materials = item.Value;
            }
        }

        // Clear the originalMaterials dictionary to only track new ones
        originalMaterials.Clear();

        // Now check for nearby buildings and make them transparent
        foreach (var building in nearbyBuildings)
        {
            Renderer rend = building.GetComponent<Renderer>();
            if (rend != null)
            {
                // If we haven't already set the material to transparent, store original and change material
                if (!originalMaterials.ContainsKey(rend))
                {
                    originalMaterials.Add(rend, rend.materials);
                    ChangeMaterialToTransparent(rend);
                }
            }
        }
    }

    // Method to change material to transparent
    private void ChangeMaterialToTransparent(Renderer rend)
    {
        if (rend != null)
        {
            Material[] materials = new Material[rend.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = transparentMaterial;
            }
            rend.materials = materials;
        }
    }
}
