using UnityEngine;
using static controlableElement;

public class airElementScript : MonoBehaviour
{
    internal float density = 0.5f;
    internal Vector3 velocity, acceleration;

    // We increase collision & repulsion to avoid clumping
    private const float smoothingRadius = 3f;
    private const float collisionRadius = 0.5f;
    private const float edgeRepellingForceStrength = 0.1f;
    private const float attractionStrength = 0f;  // No attraction
    private const float repulsionStrength = 0.3f;   // Even stronger repulsion
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

    internal bool isControllable = false;
    int myIndex;
    private cloudScript theCloud;

    // Damping factor for natural slowing when undisturbed
    [SerializeField] private float dampingFactor = 0.98f;

    // Preallocated buffers for non-allocating overlap sphere queries:
    private Collider[] overlapBufferSmall = new Collider[32];   // for collision queries
    private Collider[] overlapBufferLarge = new Collider[64];     // for smoothing/density queries

    // Cached main camera reference
    private Camera mainCamera;

    // Precomputed constant denominator for smoothing kernels using smoothingRadius
    private float smoothingDenom;

    // Random jitter caching to reduce frequent Random.insideUnitSphere calls
    private Vector3[] randomJitterCache;
    private int randomIndex = 0;
    private const int randomCacheSize = 100;

    // Density update frequency
    private float densityUpdateTimer = 0f;
    private const float densityUpdateInterval = 0.05f; // seconds

    internal void Awake()
    {
        mainCamera = Camera.main;
        smoothingDenom = Mathf.PI * Mathf.Pow(smoothingRadius, 8);
        randomJitterCache = new Vector3[randomCacheSize];
        for (int i = 0; i < randomCacheSize; i++)
        {
            randomJitterCache[i] = Random.insideUnitSphere * 0.05f;
        }
    }

    internal void Start()
    {
        CalculateBounds();
      
    }

    void CalculateBounds()
    {
        if (mainCamera != null)
        {
            float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 centerPoint = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, screenDistance));
            float cubeSize = 5.0f;
            boundsMin = centerPoint - new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
            boundsMax = centerPoint + new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
        }
    }

    // Combined computation for pressure force and intermolecular (repulsion) force.
    Vector3 ComputeSmoothingForces(int count)
    {
        Vector3 pressureForce = Vector3.zero;
        Vector3 intermolecularForce = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            Collider c = overlapBufferLarge[i];
            if (c.gameObject == gameObject) continue;
            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist > 0)
            {
                Vector3 dir = (c.transform.position - transform.position).normalized;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);
                float otherDensity = Mathf.Max(DensityAt(c.transform.position), minDensity);
                float sharedPressure = (ConvertDensityToPressure(otherDensity) + ConvertDensityToPressure(density)) / 2f;
                pressureForce += sharedPressure * dir * slope * mass / otherDensity;

                if (dist < intermolecularDistance)
                {
                    Vector3 repulsionForce = -dir * repulsionStrength / (dist * dist);
                    intermolecularForce += repulsionForce;
                }
            }
        }
        return pressureForce + intermolecularForce;
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
        return -24f * dist * f * f / smoothingDenom;
    }

    void ResolveCollisions()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, collisionRadius, overlapBufferSmall);
        for (int i = 0; i < count; i++)
        {
            Collider c = overlapBufferSmall[i];
            if (c.gameObject == gameObject) continue;
            float dist = Vector3.Distance(c.transform.position, transform.position);
            if (dist < collisionRadius && dist > 0)
            {
                Vector3 dir = (transform.position - c.transform.position).normalized;
                float overlap = collisionRadius - dist;
                transform.position += dir * overlap * 0.7f;
                velocity = Vector3.Lerp(velocity, velocity + dir * overlap * 3f, Time.deltaTime * 3f);
                velocity += GetRandomJitter();
            }
        }
    }

    float DensityAt(Vector3 point)
    {
        float densitySum = 0f;
        int count = Physics.OverlapSphereNonAlloc(point, smoothingRadius, overlapBufferLarge);
        for (int i = 0; i < count; i++)
        {
            Collider c = overlapBufferLarge[i];
            float dist = Vector3.Distance(c.transform.position, point);
            float influence = SmoothingKernel(smoothingRadius, dist);
            densitySum += influence;
        }
        return Mathf.Max(densitySum, minDensity);
    }

    float SmoothingKernel(float radius, float dist)
    {
        float val = Mathf.Max(0f, radius * radius - dist * dist);
        return Mathf.Pow(val, 3) / smoothingDenom;
    }

    internal void Update()
    {
        int smoothingCount = Physics.OverlapSphereNonAlloc(transform.position, smoothingRadius, overlapBufferLarge);
        Vector3 combinedSmoothingForce = ComputeSmoothingForces(smoothingCount);
        acceleration = combinedSmoothingForce;

        Vector3 dragForce = -velocity.normalized * 0.5f * dragCoefficient * fluidDensity *
                            (velocity.magnitude * velocity.magnitude) * crossSectionalArea;
        acceleration += dragForce / mass;

        Vector3 liftDir = Vector3.Cross(velocity, Vector3.up).normalized;
        Vector3 liftForce = liftDir * 0.5f * liftCoefficient * fluidDensity *
                            (velocity.magnitude * velocity.magnitude) * crossSectionalArea;
        acceleration += liftForce / mass;

        ApplyVortexCenters();

        velocity += acceleration * Time.deltaTime;
        Vector3 candidatePosition = transform.position + velocity * Time.deltaTime;

        if (!IsInvalid(candidatePosition))
        {
            transform.position = candidatePosition;
        }
        else
        {
            transform.position = Vector3.zero;
            velocity = Vector3.zero;
            acceleration = Vector3.zero;
        }

        velocity *= dampingFactor;

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

        densityUpdateTimer += Time.deltaTime;
        if (densityUpdateTimer >= densityUpdateInterval)
        {
            density = DensityAt(transform.position);
            densityUpdateTimer = 0f;
        }

        ResolveCollisions();
        KeepParticleWithinScreenView();
        ApplyEdgeRepellingForce();

        if (!isControllable)
            theCloud.myNewPositionIs(transform.position, myIndex);
    }

    bool IsInvalid(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
               float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
    }

    void KeepParticleWithinScreenView()
    {
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
                Vector3 swirlAxis = Vector3.forward;
                Vector3 swirlDir = Vector3.Cross(toCenter, swirlAxis).normalized;
                float closeness = 1f - (dist / vortex.radius);
                float swirlMag = vortex.swirlStrength * closeness;
                Vector3 swirlForce = swirlDir * swirlMag;
                acceleration += swirlForce / mass;
            }
        }
    }

    internal void yourPositionIs(int index, cloudScript cloudScript)
    {
        myIndex = index;
        theCloud = cloudScript;
    }

    Vector3 GetRandomJitter()
    {
        if (randomJitterCache == null || randomJitterCache.Length == 0)
        {
            randomJitterCache = new Vector3[randomCacheSize];
            for (int i = 0; i < randomCacheSize; i++)
            {
                randomJitterCache[i] = Random.insideUnitSphere * 0.05f;
            }
            randomIndex = 0;
        }
        Vector3 jitter = randomJitterCache[randomIndex];
        randomIndex = (randomIndex + 1) % randomCacheSize;
        return jitter;
    }
}
