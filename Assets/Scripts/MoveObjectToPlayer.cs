using UnityEngine;

public class MoveObjectStraight : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float maxFadeDistance = 3.0f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private Vector3 rotationSpeed = new Vector3(0f, 90f, 0f);

    private Material materialInstance; // Instance of the material to modify
    private Color originalColor;
    private Transform missCounter;
    private Transform visualRotationTransform;

    private void Start()
    {
        Vector3 originalScale = transform.localScale;

        GameObject visualObject = new GameObject("VisualRotation");
        visualRotationTransform = visualObject.transform;
        visualRotationTransform.SetParent(transform);
        visualRotationTransform.localPosition = Vector3.zero;
        visualRotationTransform.localRotation = Quaternion.identity;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        MeshRenderer newRenderer = visualObject.AddComponent<MeshRenderer>();
        MeshFilter newFilter = visualObject.AddComponent<MeshFilter>();

        // Create a material instance so we don't modify the original
        materialInstance = new Material(meshRenderer.material);

        // Set up the material for transparency
        SetupMaterialTransparency(materialInstance);

        newRenderer.material = materialInstance;
        newFilter.mesh = meshFilter.mesh;
        originalColor = materialInstance.color;

        visualRotationTransform.localScale = Vector3.one;
        meshRenderer.enabled = false;
        transform.localScale = originalScale;

        GameObject missCounterObj = GameObject.FindGameObjectWithTag("MissCounter");
        if (missCounterObj)
        {
            missCounter = missCounterObj.transform;
        }
        else
        {
            Debug.LogError("MissCounter object not found! Make sure it's tagged correctly.");
        }
    }

    private void SetupMaterialTransparency(Material material)
    {
        // Check if we're using URP or Standard RP
        if (material.shader.name.Contains("Universal Render Pipeline"))
        {
            // Setup for URP
            material.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            material.SetFloat("_Blend", 0f);   // 0 = Alpha, 1 = Premultiply
            material.SetFloat("_ZWrite", 0f);  // 0 = disable Z write
            material.renderQueue = 3000;       // Transparent queue
        }
        else
        {
            // Setup for Standard RP
            material.SetFloat("_Mode", 2f);    // 2 = Fade mode
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
        }
    }

    private void Update()
    {
        transform.position += -transform.forward * moveSpeed * Time.deltaTime;
        visualRotationTransform.Rotate(rotationSpeed * Time.deltaTime);

        if (missCounter)
        {
            float distance = Vector3.Distance(transform.position, missCounter.position);
            float alpha = Mathf.Clamp(distance / maxFadeDistance, minAlpha, 1f);

            // Update the material's alpha while preserving its original color
            Color newColor = originalColor;
            newColor.a = alpha;
            materialInstance.color = newColor;
        }
    }

    private void OnDestroy()
    {
        // Clean up the material instance
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }
}