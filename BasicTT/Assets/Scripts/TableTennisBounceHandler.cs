using UnityEngine;

/// <summary>
/// Handles bounce physics for the table tennis ball
/// </summary>
public class TableTennisBounceHandler : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    private const float MIN_BOUNCE_VELOCITY = 0.1f;

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        if (ballRigidbody == null)
        {
            Debug.LogError("No Rigidbody found on ball!");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < MIN_BOUNCE_VELOCITY)
            return;

        // Get bounce properties based on what was hit
        float bounceRestitution = GetBounceRestitution(collision.gameObject);
        
        // Calculate bounce direction
        Vector3 normal = collision.contacts[0].normal;
        Vector3 incomingVelocity = collision.relativeVelocity;
        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);
        
        // Apply bounce force
        ballRigidbody.linearVelocity = reflectedVelocity * bounceRestitution;
    }

    private float GetBounceRestitution(GameObject hitObject)
    {
        // Check what type of object was hit and return appropriate bounce factor
        if (hitObject.CompareTag("Table"))
        {
            return TableTennisPhysicsConfig.Instance.tableBounceRestitution;
        }
        else if (hitObject.CompareTag("Paddle"))
        {
            return TableTennisPhysicsConfig.Instance.paddleBounceRestitution;
        }
        
        return 0.5f; // Default bounce for other objects
    }
}