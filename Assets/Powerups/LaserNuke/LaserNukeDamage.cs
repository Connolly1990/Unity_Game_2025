using UnityEngine;
using System.Reflection;

public class LaserNukeDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damage = 50;
    public float explosionRadius = 5f;
    // Removed destroyOnImpact - nuke always penetrates

    [Header("Visual Effects")]
    public GameObject impactEffect;
    public GameObject areaEffect;

    [Header("Audio Settings")]
    public AudioClip impactSound;
    public float soundVolume = 1f;

    [Header("Penetration Settings")]
    public float damageInterval = 0.1f; // How often to damage the same target
    private System.Collections.Generic.Dictionary<GameObject, float> lastDamageTime = new System.Collections.Generic.Dictionary<GameObject, float>();

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("Wall") || other.CompareTag("Obstacle"))
        {
            // Create explosion effect at impact point but don't destroy the nuke
            CreateExplosionEffects();

            // Apply area damage
            Explode();

            // DO NOT destroy the nuke - let it continue through everything
        }
    }

    private void CreateExplosionEffects()
    {
        // Visual effects
        if (impactEffect != null)
        {
            GameObject effect = Instantiate(impactEffect, transform.position, transform.rotation);
            Destroy(effect, 2f);
        }

        if (areaEffect != null)
        {
            GameObject effect = Instantiate(areaEffect, transform.position, transform.rotation);
            effect.transform.localScale = Vector3.one * explosionRadius * 2;
            Destroy(effect, 2f);
        }

        // Audio
        if (impactSound != null)
        {
            AudioSource.PlayClipAtPoint(impactSound, transform.position, soundVolume);
        }
    }

    private void Explode()
    {
        // Area of effect damage
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Enemy"))
            {
                // Check if enough time has passed since last damage to this target
                if (CanDamageTarget(hitCollider.gameObject))
                {
                    ApplyDamage(hitCollider.gameObject);
                    lastDamageTime[hitCollider.gameObject] = Time.time;
                }
            }
        }
    }

    private bool CanDamageTarget(GameObject target)
    {
        if (!lastDamageTime.ContainsKey(target))
        {
            return true; // First time hitting this target
        }

        return Time.time - lastDamageTime[target] >= damageInterval;
    }

    private void ApplyDamage(GameObject target)
    {
        // First try the SuicideBomber's TakeDamage method (from your example)
        var suicideBomber = target.GetComponent<SuicideBomber>();
        if (suicideBomber != null)
        {
            suicideBomber.TakeDamage(damage);
            return;
        }

        // Then try PlayerHealth if it's a player (though nuke shouldn't damage player)
        var playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            return;
        }

        // Fallback to reflection-based damage for other enemies
        var healthComponent = target.GetComponent<MonoBehaviour>();
        if (healthComponent != null)
        {
            // Try common damage methods
            var takeDamageMethod = healthComponent.GetType().GetMethod("TakeDamage");
            if (takeDamageMethod != null)
            {
                takeDamageMethod.Invoke(healthComponent, new object[] { damage });
                return;
            }

            // Try direct health field/property access
            var healthField = healthComponent.GetType().GetField("health");
            var healthProperty = healthComponent.GetType().GetProperty("health");

            if (healthField != null && healthField.FieldType == typeof(int))
            {
                int currentHealth = (int)healthField.GetValue(healthComponent);
                healthField.SetValue(healthComponent, currentHealth - damage);
                return;
            }
            else if (healthProperty != null && healthProperty.PropertyType == typeof(int))
            {
                int currentHealth = (int)healthProperty.GetValue(healthComponent);
                healthProperty.SetValue(healthComponent, currentHealth - damage);
                return;
            }
        }

        // Final fallback - just destroy
        Destroy(target);
    }

    private void OnDestroy()
    {
        // Clean up the damage tracking dictionary
        if (lastDamageTime != null)
        {
            lastDamageTime.Clear();
        }
    }
}