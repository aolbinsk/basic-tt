using UnityEngine;

/// <summary>
/// Handles bounce physics for the table tennis ball, including collision rollback for accurate interactions.
/// </summary>
public class TableTennisBounceHandler : MonoBehaviour
{
    private Rigidbody ballRigidbody;
    private const float MIN_BOUNCE_VELOCITY = 0.1f;
    private Vector3 previousPosition;
    private SphereCollider _sphereCollider;

    private void Start()
    {
        _sphereCollider = GetComponent<SphereCollider>();
    }

    private void Awake()
    {
        ballRigidbody = GetComponent<Rigidbody>();
        if (ballRigidbody == null)
        {
            Debug.LogError("No Rigidbody found on ball!");
        }
        else
        {
            // Set collision detection mode to Continuous Dynamic
            ballRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        previousPosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Perform collision detection along the path from previousPosition to current position
        Vector3 movement = transform.position - previousPosition;
        float distance = movement.magnitude;

        if (distance > 0f)
        {
            RaycastHit hit;
            if (Physics.SphereCast(previousPosition, _sphereCollider.radius, movement.normalized, out hit, distance))
            {
                // Collision occurred between previous position and current position
                HandleCollision(hit);
            }
        }

        previousPosition = transform.position;
    }

    private void HandleCollision(RaycastHit hit)
    {
        // Calculate collision restitution
        float bounceRestitution = GetBounceRestitution(hit.collider.gameObject);

        // Calculate collision point
        Vector3 collisionPoint = hit.point;

        // Move ball to collision point
        transform.position = collisionPoint;

        // Calculate bounce direction
        Vector3 normal = hit.normal;
        Vector3 incomingVelocity = ballRigidbody.linearVelocity;
        Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, normal);

        // Apply bounce force
        ballRigidbody.linearVelocity = reflectedVelocity * bounceRestitution;
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
            return TableTennisPhysicsConfig.instance.tableBounceRestitution;
        }

        if (hitObject.CompareTag("Paddle"))
        {
            return TableTennisPhysicsConfig.instance.paddleBounceRestitution;
        }

        if (hitObject.CompareTag("Net"))
        {
            return TableTennisPhysicsConfig.instance.netBounceRestitution;
        }

        return 0.5f; // Default bounce for other objects
    }
}