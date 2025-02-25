using UnityEngine;

public class airElementScript : MonoBehaviour
{
    internal float density = 0.5f;
    internal Vector3 velocity, acceleration;

    // We increase collision & repulsion to avoid clumping
    private const float smoothingRadius = 3f;
    private const float collisionRadius = 0.5f;
    private const float edgeRepellingForceStrength = 0.1f;
    private const float attractionStrength = 0f;  // No attraction
    private const float repulsionStrength = 0.3f; // Even stronger repulsion
    private const float intermolecularDistance = 2.0f;
    private const float pressureMultiplier = 1f;
    private const float targetDensity = 0.5f;
    internal const float mass = 1f;

    // Aerodynamics
    internal float dragCoefficient = 0.47f;
    internal float liftCoefficient = 0.2f;
    internal float fluidDensity = 1.225f;
    internal float crossSectionalArea = 1.0f;
    internal float forceStrength = 5f;

    private const float minDensity = 0.001f;
    private const float maxVelocity = 50f;
    private Vector3 boundsMin, boundsMax;

    // NEW: damping factor for natural slowing when undisturbed
    [SerializeField] private float dampingFactor = 0.98f;

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
            float cubeSize = 5.0f;
            boundsMin = centerPoint - new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
            boundsMax = centerPoint + new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
        }
    }

    Vector3 CalculatePressureForce()
    {
        Vector3 pressureForce = Vector3.zero;
        Collider[] colliders = Physics.OverlapSphere(transform.position, smoothingRadius);
        foreach (Collider c in colliders)
        {
            if (c.gameObject == gameObject) continue;
            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist > 0)
            {
                Vector3 dir = (c.transform.position - transform.position).normalized;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);
                float otherDensity = Mathf.Max(DensityAt(c.transform.position), minDensity);
                float sharedPressure = (ConvertDensityToPressure(otherDensity) + ConvertDensityToPressure(density)) / 2f;
                pressureForce += sharedPressure * dir * slope * mass / otherDensity;
            }
        }
        return pressureForce;
    }

    float ConvertDensityToPressure(float d)
    {
        float densityError = d - targetDensity;
        return densityError * pressureMultiplier;
    }

    float SmoothingKernelDerivative(float dist, float radius)
    {
        if (dist >= radius) return 0f;
        float f = radius * radius - dist * dist;
        return -24f * dist * f * f / (Mathf.PI * Mathf.Pow(radius, 8));
    }

    void ResolveCollisions()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, collisionRadius);
        foreach (Collider c in colliders)
        {
            if (c.gameObject == gameObject) continue;
            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist < collisionRadius && dist > 0)
            {
                Vector3 dir = (transform.position - c.transform.position).normalized;
                float overlap = collisionRadius - dist;
                transform.position += dir * overlap * 0.7f;
                velocity = Vector3.Lerp(velocity, velocity + dir * overlap * 3f, Time.deltaTime * 3f);
                // random jitter
                velocity += Random.insideUnitSphere * 0.05f;
            }
        }
    }

    void ApplyIntermolecularForces()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, smoothingRadius);
        foreach (Collider c in colliders)
        {
            if (c.gameObject == gameObject) continue;
            Vector3 dir = c.transform.position - transform.position;
            float dist = dir.magnitude;
            if (dist < intermolecularDistance && dist > 0)
            {
                // repulsion only
                Vector3 repulsionForce = -dir.normalized * repulsionStrength / (dist * dist);
                velocity += repulsionForce * Time.deltaTime;
            }
        }
    }

    float DensityAt(Vector3 point)
    {
        Collider[] colliders = Physics.OverlapSphere(point, smoothingRadius);
        float densitySum = 0f;
        foreach (Collider c in colliders)
        {
            float dist = Vector3.Distance(c.transform.position, point);
            float influence = SmoothingKernel(smoothingRadius, dist);
            densitySum += influence;
        }
        return Mathf.Max(densitySum, minDensity);
    }

    float SmoothingKernel(float radius, float dist)
    {
        float val = Mathf.Max(0f, radius * radius - dist * dist);
        return val * val * val / (Mathf.PI * Mathf.Pow(radius, 8));
    }

    internal void Update()
    {
        // 1) Pressure
        acceleration = CalculatePressureForce();

        // 2) Drag
        Vector3 dragForce = -velocity.normalized * 0.5f * dragCoefficient * fluidDensity *
                            (velocity.magnitude * velocity.magnitude) * crossSectionalArea;
        acceleration += dragForce / mass;

        // 3) Lift
        Vector3 liftDir = Vector3.Cross(velocity, Vector3.up).normalized;
        Vector3 liftForce = liftDir * 0.5f * liftCoefficient * fluidDensity *
                            (velocity.magnitude * velocity.magnitude) * crossSectionalArea;
        acceleration += liftForce / mass;

        // 4) Swirl from vortex centers
        ApplyVortexCenters();

        // 5) Update velocity & position
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        // NEW: Apply damping to simulate random slowing
        velocity *= dampingFactor;

        // 6) Safety checks
        if (IsInvalid(velocity))
        {
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
        }
        if (velocity.magnitude > maxVelocity)
        {
            velocity = velocity.normalized * maxVelocity;
        }
        if (IsInvalid(transform.position))
        {
            transform.position = Vector3.zero;
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
        }

        density = DensityAt(transform.position);

        // 7) Collisions, repulsion, etc.
        ResolveCollisions();
        KeepParticleWithinScreenView();
        ApplyEdgeRepellingForce();
        ApplyIntermolecularForces();
    }

    bool IsInvalid(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
               float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
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

    void ApplyVortexCenters()
    {
        if (controlableElement.Instance == null) return;
        foreach (VortexCenter vortex in controlableElement.Instance.vortexCenters)
        {
            Vector3 toCenter = transform.position - vortex.position;
            float dist = toCenter.magnitude;
            if (dist < vortex.radius)
            {
                // swirl around forward axis (for XY swirl)
                Vector3 swirlAxis = Vector3.forward;
                Vector3 swirlDir = Vector3.Cross(toCenter, swirlAxis).normalized;
                float closeness = 1f - (dist / vortex.radius);
                float swirlMag = vortex.swirlStrength * closeness;
                Vector3 swirlForce = swirlDir * swirlMag;
                acceleration += swirlForce / mass;
            }
        }
    }

}
