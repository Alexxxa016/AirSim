using UnityEngine;

public class airElementScript : MonoBehaviour
{
    internal float density = 0.5f;
    internal Vector3 velocity, acceleration;
    float smoothingRadius = 3;
    internal float mass = 1;
    float targetDensity = 0.5f;
    Vector3 boundsMin, boundsMax;
    float pressureMultiplier = 1f;
    float collisionRadius = 0.2f;
    float edgeRepellingForceStrength = 0.1f;  
    float attractionStrength = 0.01f;          
    float repulsionStrength = 0.05f;           
    float intermolecularDistance = 1.0f;       // Distance at which intermolecular forces

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

    Vector3 CalculatePressureForce()
    {
        Vector3 pressureForce = Vector3.zero;
        Collider[] allAirElements = Physics.OverlapSphere(transform.position, smoothingRadius);

        foreach (Collider c in allAirElements)
        {
            if (c.gameObject == gameObject) continue;

            float dist = (c.transform.position - transform.position).magnitude;
            if (dist > 0)
            {
                Vector3 dir = (c.transform.position - transform.position).normalized;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);
                float density = DensityAt(c.transform.position);

                float sharedPressure = CalculateSharedPressure(density, this.density);
                pressureForce += sharedPressure * dir * slope * mass / density;
            }
        }
        return pressureForce;
    }

    float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        return densityError * pressureMultiplier;
    }

    float SmoothingKernelDerivative(float dist, float radius)
    {
        if (dist >= radius) return 0;
        float f = radius * radius - dist * dist;
        return -24 * dist * f * f / (Mathf.PI * Mathf.Pow(radius, 8));
    }

    void ResolveCollisions()
    {
        Collider[] allAirElements = Physics.OverlapSphere(transform.position, collisionRadius);
        foreach (Collider c in allAirElements)
        {
            if (c.gameObject == gameObject) continue;

            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist < collisionRadius)
            {
                Vector3 dir = (transform.position - c.transform.position).normalized;
                float overlap = collisionRadius - dist;
                transform.position += dir * overlap * 0.5f;
                airElementScript otherParticle = c.GetComponent<airElementScript>();
                if (otherParticle != null)
                {
                    otherParticle.velocity -= dir * overlap * 0.5f;
                }
            }
        }
    }

    internal void Update()
    {
        acceleration = CalculatePressureForce();
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        density = DensityAt(transform.position);
        ResolveCollisions();

        // Ensure the particle stays within the screen view
        KeepParticleWithinScreenView();
        ApplyEdgeRepellingForce();
        ApplyIntermolecularForces();
    }

    void KeepParticleWithinScreenView()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 screenMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, screenDistance));
            Vector3 screenMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, screenDistance));

            transform.position = new Vector3(
                Mathf.Clamp(transform.position.x, screenMin.x, screenMax.x),
                Mathf.Clamp(transform.position.y, screenMin.y, screenMax.y),
                Mathf.Clamp(transform.position.z, boundsMin.z, boundsMax.z) 
            );
        }
    }

    void ApplyEdgeRepellingForce()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 screenMin = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, screenDistance));
            Vector3 screenMax = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, screenDistance));

            // Apply repelling force based on proximity to screen edges
            if (transform.position.x < screenMin.x + 0.1f)
                velocity.x += edgeRepellingForceStrength;
            else if (transform.position.x > screenMax.x - 0.1f)
                velocity.x -= edgeRepellingForceStrength;

            if (transform.position.y < screenMin.y + 0.1f)
                velocity.y += edgeRepellingForceStrength;
            else if (transform.position.y > screenMax.y - 0.1f)
                velocity.y -= edgeRepellingForceStrength;
        }
    }

    void ApplyIntermolecularForces()
    {
        Collider[] allAirElements = Physics.OverlapSphere(transform.position, smoothingRadius);
        foreach (Collider col in allAirElements)
        {
            if (col.gameObject == gameObject) continue;

            Vector3 direction = col.transform.position - transform.position;
            float distance = direction.magnitude;

            if (distance < intermolecularDistance)
            {
                // Attraction force
                Vector3 attractionForce = direction.normalized * attractionStrength / (distance * distance);
                velocity += attractionForce * Time.deltaTime;

                // Repulsion force
                Vector3 repulsionForce = -direction.normalized * repulsionStrength / (distance * distance);
                velocity += repulsionForce * Time.deltaTime;
            }
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

    float SmoothingKernel(float smoothingRadius, float dist)
    {
        float val = Mathf.Max(0, smoothingRadius * smoothingRadius - dist * dist);
        return val * val * val / (Mathf.PI * Mathf.Pow(smoothingRadius, 8));
    }
}
