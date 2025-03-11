using System.Collections.Generic;
using UnityEngine;

public class cloudScript : MonoBehaviour
{
    public static cloudScript Instance; // Cached instance

    public GameObject AirElementCloneTemplate;
    int dim = 6;
    [SerializeField]
    float cubeSize = 10f; // Container size (10x10x10 cube)
    Vector3 boundsMin, boundsMax;

    internal Vector3[] allPos, velocities, accelerations;
    // internal airElementScript[] airElementScripts;

    public spatialHash spatialHashInstance;
    [SerializeField]
    public float spatialCellSize = 3f;

    // Update spatial hash every updateHashInterval seconds.
    private float updateHashInterval = 0.3f;
    private float updateHashTimer = 0f;

    private float smoothingDenom;
    private Vector3[] randomJitterCache;
    private int randomIndex = 0;
    private const int randomCacheSize = 100;

    // Smoothing & Pressure settings
    [SerializeField]
    private const float smoothingRadius = 1.5f;
    [SerializeField]
    private const float collisionRadius = 0.2f;
    [SerializeField]
    private const float edgeRepellingForceStrength = 0.1f;
    [SerializeField]
    private const float repulsionStrength = 0.2f;
    [SerializeField]
    private const float intermolecularDistance = 0.5f;
    [SerializeField]
    private const float pressureMultiplier = 5f;
    [SerializeField]
    private const float targetDensity = 0.5f;
    internal const float mass = 1f;
    private const float minDensity = 0.001f;

    // Cached density values for each particle.
    internal float[] densities;

    private List<Vector3> queryResult = new List<Vector3>();
    internal float density = 0f; // Global density (optional)

    // Damping and velocity limit
    private float dampingFactor = 0.98f;
    private const float maxVelocity = 50f;

    // Density update timer
    private float densityUpdateTimer = 0f;
    private const float densityUpdateInterval = 0.1f;

    // (Aerodynamic forces removed)
    // internal float dragCoefficient = 0.47f;
    // internal float liftCoefficient = 0.2f;
    // internal float fluidDensity = 1.225f;
    // internal float crossSectionalArea = 1.0f;
    // internal float forceStrength = 5f;

    void Awake()
    {
        Instance = this;
        smoothingDenom = Mathf.PI * Mathf.Pow(smoothingRadius, 8);
        randomJitterCache = new Vector3[randomCacheSize];
        for (int i = 0; i < randomCacheSize; i++)
        {
            randomJitterCache[i] = Random.insideUnitSphere * 0.05f;
        }
    }

    void Start()
    {
        // Set container bounds based solely on cubeSize.
        boundsMin = new Vector3(-cubeSize / 2, -cubeSize / 2, -cubeSize / 2);
        boundsMax = new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);

        int numParticles = dim * dim * dim;
        allPos = new Vector3[numParticles];
        velocities = new Vector3[numParticles];
        accelerations = new Vector3[numParticles];
        densities = new float[numParticles];  // Allocate density array

        GenerateCloud();

        spatialHashInstance = new spatialHash(spatialCellSize);
        spatialHashInstance.Insert(allPos);
    }

    void Update()
    {
        // Update spatial hash periodically.
        updateHashTimer += Time.deltaTime;
        if (updateHashTimer >= updateHashInterval)
        {
            spatialHashInstance.UpdateHash(allPos);
            updateHashTimer = 0f;
        }

        // Cache density for each particle once per frame.
        for (int i = 0; i < allPos.Length; i++)
        {
            densities[i] = DensityAt(allPos[i]);
        }

        // Now update each particle using the cached density values.
        for (int i = 0; i < allPos.Length; i++)
        {
            Vector3 combinedForce = ComputeSmoothingForces(i);
            accelerations[i] = combinedForce;

            // Semi-implicit Euler integration.
            velocities[i] += accelerations[i] * Time.deltaTime;
            Vector3 candidatePosition = allPos[i] + velocities[i] * Time.deltaTime;
            if (!IsInvalid(candidatePosition))
                allPos[i] = candidatePosition;
            else
            {
                allPos[i] = Vector3.zero;
                velocities[i] = Vector3.zero;
                accelerations[i] = Vector3.zero;
            }

            velocities[i] *= dampingFactor;
            if (IsInvalid(velocities[i]))
            {
                velocities[i] = Vector3.zero;
                accelerations[i] = Vector3.zero;
            }
            if (velocities[i].magnitude > maxVelocity)
                velocities[i] = velocities[i].normalized * maxVelocity;

            densityUpdateTimer += Time.deltaTime;
            if (densityUpdateTimer >= densityUpdateInterval)
            {
                density = DensityAt(transform.position);
                densityUpdateTimer = 0f;
            }

            ResolveCollisions(i);
            KeepParticleWithinContainer(i);
            ApplyEdgeRepellingForce(i);
        }
    }

    void GenerateCloud()
    {
        int index = 0;
        for (int i = 0; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                for (int k = 0; k < dim; k++)
                {
                    Vector3 pos = new Vector3(
                        Random.Range(boundsMin.x, boundsMax.x),
                        Random.Range(boundsMin.y, boundsMax.y),
                        Random.Range(boundsMin.z, boundsMax.z)
                    );

                    allPos[index] = pos;
                    index++;
                }
            }
        }
    }

    // Constrain particles within container bounds.
    void KeepParticleWithinContainer(int index)
    {
        allPos[index] = new Vector3(
            Mathf.Clamp(allPos[index].x, boundsMin.x, boundsMax.x),
            Mathf.Clamp(allPos[index].y, boundsMin.y, boundsMax.y),
            Mathf.Clamp(allPos[index].z, boundsMin.z, boundsMax.z)
        );
    }

    bool IsInvalid(Vector3 v)
    {
        return float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z) ||
               float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z);
    }

    // Collision resolution: gently separate overlapping particles.
    void ResolveCollisions(int index)
    {
        List<Vector3> neighbors = new List<Vector3>();
        spatialHashInstance.Query(allPos[index], collisionRadius, neighbors);
        foreach (var neighbor in neighbors)
        {
            float dist = Vector3.Distance(neighbor, allPos[index]);
            if (dist < collisionRadius && dist > 0)
            {
                Vector3 dir = (allPos[index] - neighbor).normalized;
                float overlap = (collisionRadius - dist) * 0.7f;
                allPos[index] += dir * overlap;
                velocities[index] = Vector3.Lerp(velocities[index], velocities[index] + dir * overlap * 3f, Time.deltaTime * 3f);
                velocities[index] += GetRandomJitter();
            }
        }
    }

    // Smoothing kernel (poly6-like) for density estimation.
    float SmoothingKernel(float radius, float dist)
    {
        float val = Mathf.Max(0f, radius * radius - dist * dist);
        return Mathf.Pow(val, 3) / smoothingDenom;
    }

    // Smoothing kernel derivative for pressure force.
    float SmoothingKernelDerivative(float dist, float radius)
    {
        if (dist >= radius) return 0f;
        float f = radius * radius - dist * dist;
        return -24f * dist * f * f / smoothingDenom;
    }

    // Convert density to pressure.
    float ConvertDensityToPressure(float d)
    {
        float densityError = d - targetDensity;
        return densityError * pressureMultiplier;
    }

    // Compute the smoothing (pressure/diffusion) force using cached density values.
    Vector3 ComputeSmoothingForces(int index)
    {
        List<Vector3> neighbors = new List<Vector3>();
        spatialHashInstance.Query(allPos[index], smoothingRadius, neighbors);

        // If no neighbors, return a random jitter to avoid stagnation.
        if (neighbors.Count == 0)
            return GetRandomJitter();

        float localDensity = densities[index]; // use cached density
        float localPressure = ConvertDensityToPressure(localDensity);

        Vector3 pressureForce = Vector3.zero;
        Vector3 densityGradient = Vector3.zero;

        foreach (var neighbor in neighbors)
        {
            float dist = Vector3.Distance(neighbor, allPos[index]);
            if (dist > 0f && dist < smoothingRadius)
            {
                Vector3 dir = (allPos[index] - neighbor).normalized;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);
                // Try to find neighbor's density from cached values:
                int neighborIndex = System.Array.IndexOf(allPos, neighbor);
                float neighborDensity = (neighborIndex >= 0) ? densities[neighborIndex] : Mathf.Max(DensityAt(neighbor), minDensity);
                float neighborPressure = ConvertDensityToPressure(neighborDensity);
                float sharedPressure = (localPressure + neighborPressure) / 2f;
                // Negative force: pushes from high-pressure areas.
                pressureForce += -sharedPressure * dir * slope * mass / Mathf.Max(neighborDensity, minDensity);
                densityGradient += dir * (localDensity - neighborDensity);
            }
        }
        float diffusionCoefficient = 0.3f; // Adjust to increase/decrease diffusion strength.
        Vector3 diffusionForce = diffusionCoefficient * densityGradient;
        return pressureForce + diffusionForce;
    }

    // Calculate local density at a point.
    float DensityAt(Vector3 point)
    {
        queryResult.Clear();
        spatialHashInstance.Query(point, smoothingRadius, queryResult);
        float densitySum = 0f;
        for (int i = 0; i < queryResult.Count; i++)
        {
            float dist = Vector3.Distance(queryResult[i], point);
            float influence = SmoothingKernel(smoothingRadius, dist);
            densitySum += mass * influence;
        }
        return Mathf.Max(densitySum, minDensity);
    }

    // Apply edge repelling force based on container bounds.
    void ApplyEdgeRepellingForce(int index)
    {
        float threshold = 0.1f;
        if (allPos[index].x < boundsMin.x + threshold)
            velocities[index].x += edgeRepellingForceStrength;
        else if (allPos[index].x > boundsMax.x - threshold)
            velocities[index].x -= edgeRepellingForceStrength;
        if (allPos[index].y < boundsMin.y + threshold)
            velocities[index].y += edgeRepellingForceStrength;
        else if (allPos[index].y > boundsMax.y - threshold)
            velocities[index].y -= edgeRepellingForceStrength;
        if (allPos[index].z < boundsMin.z + threshold)
            velocities[index].z += edgeRepellingForceStrength;
        else if (allPos[index].z > boundsMax.z - threshold)
            velocities[index].z -= edgeRepellingForceStrength;
    }

    // Returns a small random jitter force.
    Vector3 GetRandomJitter()
    {
        if (randomJitterCache == null || randomJitterCache.Length == 0)
        {
            randomJitterCache = new Vector3[randomCacheSize];
            for (int i = 0; i < randomCacheSize; i++)
                randomJitterCache[i] = Random.insideUnitSphere * 0.05f;
            randomIndex = 0;
        }
        Vector3 jitter = randomJitterCache[randomIndex];
        randomIndex = (randomIndex + 1) % randomCacheSize;
        return jitter;
    }
}
