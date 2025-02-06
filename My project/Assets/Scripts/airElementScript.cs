using System.Collections.Generic;
using UnityEngine;

public class airElementScript : MonoBehaviour
{
    float density = 0.5f;
    Vector3 velocity, acceleration;
    float smoothingRadius = 3;
    float mass = 1;
    float targetDensity = 0.5f;
    Vector3 boundsMin, boundsMax;
    float pressureMultiplier = 0.5f;
    float collisionRadius = 0.1f; // Minimum distance to maintain between particles

    // Start is called before the first frame update
    internal void Start()
    {

        CalculateBounds();
    }

    void CalculateBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 centerPoint = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, screenDistance));
            float cubeSize = 10.0f;
            boundsMin = centerPoint - new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
            boundsMax = centerPoint + new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
        }
    }

    float DensityAt(Vector3 point)
    {
        Collider[] allAirElements = Physics.OverlapSphere(point, smoothingRadius);
        float density = 0;
        foreach (Collider col in allAirElements)
        {
            float dist = Vector3.Distance(col.transform.position, point);
            float influence = SmoothingKernel(smoothingRadius, dist);
            density += influence;
        }
        return density;
    }

    private float SmoothingKernel(float smoothingRadius, float dist)
    {
        float volume = Mathf.PI * Mathf.Pow(smoothingRadius, 8) / 4;
        float val1 = Mathf.Max(0, smoothingRadius * smoothingRadius - dist * dist);
        return val1 * val1 * val1 / volume;
    }

    static float SmoothingKernelDerivative(float dist, float radius)
    {
        if (dist >= radius) return 0;
        float f = radius * radius - dist * dist;
        float scale = -24 / (Mathf.PI * Mathf.Pow(radius, 8));
        return scale * dist * f * f;
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }

    float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    Vector3 CalculatePressureForce()
    {
        Vector3 pressureForce = Vector3.zero;
        Collider[] allAirElements = Physics.OverlapSphere(transform.position, smoothingRadius);

        foreach (Collider c in allAirElements)
        {
            if (c.gameObject == gameObject) continue; // Skip self-collision

            float dist = (c.transform.position - transform.position).magnitude;
            if (dist > 0)
            {
                Vector3 dir = (c.transform.position - transform.position) / dist;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);
                float density = DensityAt(c.transform.position);

                float sharedPressure = CalculateSharedPressure(density, this.density);
                pressureForce += sharedPressure * dir * slope * mass / density;
            }
        }
        return pressureForce;
    }

    void ResolveCollisions()
    {
        Collider[] allAirElements = Physics.OverlapSphere(transform.position, collisionRadius);

        foreach (Collider c in allAirElements)
        {
            if (c.gameObject == gameObject) continue; // Skip self-collision

            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist < collisionRadius)
            {
                Vector3 dir = (transform.position - c.transform.position).normalized;
                float overlap = collisionRadius - dist;
                transform.position += dir * overlap * 0.5f;
                c.transform.position -= dir * overlap * 0.5f;

                // Adjust velocities to prevent further overlap
                velocity -= dir * overlap * 0.5f;
                airElementScript otherParticle = c.GetComponent<airElementScript>();
                if (otherParticle != null)
                {
                    otherParticle.velocity += dir * overlap * 0.5f;
                }
            }
        }
    }

    // Update is called once per frame
    internal void Update()
    {
        acceleration = Vector3.zero;
        Vector3 pressureForce = CalculatePressureForce();
        Vector3 pressureAcceleration;
        if (density == 0)
        {
            pressureAcceleration = Vector3.zero;
        }
        else
        {
            pressureAcceleration = pressureForce / density;
        }

        acceleration += pressureAcceleration;
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        density = DensityAt(transform.position);

        // Resolve collisions
        ResolveCollisions();

        // Non-bouncing boundary collision detection
        if (transform.position.x < boundsMin.x)
        {
            transform.position = new Vector3(boundsMin.x, transform.position.y, transform.position.z);
            velocity.x = Mathf.Abs(velocity.x); // Apply a gentle force to move it inside
        }
        if (transform.position.x > boundsMax.x)
        {
            transform.position = new Vector3(boundsMax.x, transform.position.y, transform.position.z);
            velocity.x = -Mathf.Abs(velocity.x); // Apply a gentle force to move it inside
        }
        if (transform.position.y < boundsMin.y)
        {
            transform.position = new Vector3(transform.position.x, boundsMin.y, transform.position.z);
            velocity.y = Mathf.Abs(velocity.y); // Apply a gentle force to move it inside
        }
        if (transform.position.y > boundsMax.y)
        {
            transform.position = new Vector3(transform.position.x, boundsMax.y, transform.position.z);
            velocity.y = -Mathf.Abs(velocity.y); // Apply a gentle force to move it inside
        }
        if (transform.position.z < boundsMin.z)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, boundsMin.z);
            velocity.z = Mathf.Abs(velocity.z); // Apply a gentle force to move it inside
        }
        if (transform.position.z > boundsMax.z)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, boundsMax.z);
            velocity.z = -Mathf.Abs(velocity.z); // Apply a gentle force to move it inside
        }
    }
}
