using System.Collections.Generic;
using UnityEngine;

public class cloudScript : MonoBehaviour
{
    public static cloudScript Instance; // Cached instance

    public GameObject AirElementCloneTemplate;
    int dim = 4;
    [SerializeField]
    float cubeSize = 6f; // Container size
    Vector3 boundsMin, boundsMax;

    internal Vector3[] allPos, velocities, accelerations;
    //internal airElementScript[] airElementScripts;

    public spatialHash spatialHashInstance;
    [SerializeField]
    public float spatialCellSize = 3f;

    // Update the spatial hash every 0.3 seconds
    private float updateHashInterval = 0.3f;
    private float updateHashTimer = 0f;

    private Camera mainCamera;
    private float smoothingDenom;
    private Vector3[] randomJitterCache;
    private int randomIndex = 0;
    private const int randomCacheSize = 100;

    private const float smoothingRadius = 1f;
    private const float collisionRadius = 0.5f;
    private const float edgeRepellingForceStrength = 0.1f; // gentle edge force
    private const float attractionStrength = 0f;
    private const float repulsionStrength = 0.1f; // softer repulsion
    private const float intermolecularDistance = 2.0f;
    private const float pressureMultiplier = 0.7f;
    private const float targetDensity = 0.5f;
    internal const float mass = 1f;
    private const float minDensity = 0.001f;
    private List<Vector3> queryResult = new List<Vector3>();
    internal float density = 0.5f;

    // Use a damping factor for smooth decay (0.99)
    private float dampingFactor = 0.99f;

    private float densityUpdateTimer = 0f;
    private const float densityUpdateInterval = 0.1f;

    internal float dragCoefficient = 0.47f;
    internal float liftCoefficient = 0.2f;
    internal float fluidDensity = 1.225f;
    internal float crossSectionalArea = 1.0f;
    internal float forceStrength = 5f;

    private const float maxVelocity = 50f;

    // Diffusion coefficient for the density gradient force
    private const float diffusionCoefficient = 0.1f;

    void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
        smoothingDenom = Mathf.PI * Mathf.Pow(smoothingRadius, 8);
        randomJitterCache = new Vector3[randomCacheSize];
        for (int i = 0; i < randomCacheSize; i++)
        {
            randomJitterCache[i] = Random.insideUnitSphere * 0.01f;
        }
    }

    void Start()
    {
        // Set container bounds BEFORE generating particles.
        boundsMin = new Vector3(-cubeSize / 2, -cubeSize / 2, -cubeSize / 2);
        boundsMax = new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);

        allPos = new Vector3[dim * dim * dim];
        velocities = new Vector3[dim * dim * dim];
        accelerations = new Vector3[dim * dim * dim];

        GenerateCloud();

        spatialHashInstance = new spatialHash(spatialCellSize);
        spatialHashInstance.Insert(allPos);
    }

    void Update()
    {
        updateHashTimer += Time.deltaTime;
        if (updateHashTimer >= updateHashInterval)
        {
            spatialHashInstance.UpdateHash(allPos);
            updateHashTimer = 0f;
        }

        for (int i = 0; i < allPos.Length; i++)
        {
            // Compute forces (pressure, repulsion, and diffusion).
            Vector3 combinedSmoothingForce = ComputeSmoothingForces(i);
            accelerations[i] = combinedSmoothingForce;

            // Calculate drag and lift.
            Vector3 dragForce = -velocities[i].normalized * 0.5f * dragCoefficient * fluidDensity *
                                (velocities[i].magnitude * velocities[i].magnitude) * crossSectionalArea;
            accelerations[i] += dragForce / mass;

            Vector3 liftDir = Vector3.Cross(velocities[i], Vector3.up).normalized;
            Vector3 liftForce = liftDir * 0.5f * liftCoefficient * fluidDensity *
                                (velocities[i].magnitude * velocities[i].magnitude) * crossSectionalArea;
            accelerations[i] += liftForce / mass;

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
            if (IsInvalid(transform.position))
            {
                allPos[i] = Vector3.zero;
                velocities[i] = Vector3.zero;
                accelerations[i] = Vector3.zero;
            }

            densityUpdateTimer += Time.deltaTime;
            if (densityUpdateTimer >= densityUpdateInterval)
            {
                // (Optional) Update a global density if needed.
                density = DensityAt(transform.position);
                densityUpdateTimer = 0f;
            }

            ResolveCollisions(i);
            KeepParticleWithinContainer(i);
            ApplyEdgeRepellingForce(i); // Optionally disable if too forceful.
        }
    }

    void GenerateCloud()
    {
        int index = 0;
        // Generate particles randomly within container bounds.
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

    internal Vector3[] getpositions()
    {
        return allPos;
    }

    internal void myNewPositionIs(Vector3 position, int myIndex)
    {
        allPos[myIndex] = position;
    }

    Vector3 GetRandomJitter()
    {
        if (randomJitterCache == null || randomJitterCache.Length == 0)
        {
            randomJitterCache = new Vector3[randomCacheSize];
            for (int i = 0; i < randomCacheSize; i++)
                randomJitterCache[i] = Random.insideUnitSphere * 0.01f;
            randomIndex = 0;
        }
        Vector3 jitter = randomJitterCache[randomIndex];
        randomIndex = (randomIndex + 1) % randomCacheSize;
        return jitter;
    }

    // Apply a gentle repelling force near container walls.
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

    // Clamp particles within the fixed container.
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

    // Softer collision resolution.
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
                float overlap = (collisionRadius - dist) * 0.4f; // softer correction
                allPos[index] += dir * overlap;
                float dot = Vector3.Dot(velocities[index], dir);
                if (dot < 0)
                {
                    velocities[index] -= dir * dot * 0.5f;
                }
            }
        }
    }

    float SmoothingKernelDerivative(float dist, float radius)
    {
        if (dist >= radius) return 0f;
        float f = radius * radius - dist * dist;
        return -24f * dist * f * f / smoothingDenom;
    }

    float ConvertDensityToPressure(float d)
    {
        float densityError = d - targetDensity;
        return densityError * pressureMultiplier;
    }

    // Compute forces based on pressure, repulsion, and diffusion.
    Vector3 ComputeSmoothingForces(int index)
    {
        List<Vector3> neighbors = new List<Vector3>();
        spatialHashInstance.Query(allPos[index], smoothingRadius, neighbors);

        Vector3 pressureForce = Vector3.zero;
        Vector3 intermolecularForce = Vector3.zero;
        float localDensity = DensityAt(allPos[index]);
        Vector3 densityGradient = Vector3.zero;

        foreach (var neighbor in neighbors)
        {
            float dist = Vector3.Distance(neighbor, allPos[index]);
            if (dist > 0)
            {
                Vector3 dir = (neighbor - allPos[index]).normalized;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);
                float otherDensity = Mathf.Max(DensityAt(neighbor), minDensity);
                float sharedPressure = (ConvertDensityToPressure(otherDensity) + ConvertDensityToPressure(localDensity)) / 2f;
                pressureForce += sharedPressure * dir * slope * mass / otherDensity;
                if (dist < intermolecularDistance)
                {
                    intermolecularForce += -dir * repulsionStrength / (dist * dist);
                }
                float densityDifference = localDensity - DensityAt(neighbor);
                densityGradient += dir * densityDifference;
            }
        }
        Vector3 diffusionForce = diffusionCoefficient * densityGradient;
        return pressureForce + intermolecularForce + diffusionForce;
    }

    float SmoothingKernel(float radius, float dist)
    {
        float val = Mathf.Max(0f, radius * radius - dist * dist);
        return Mathf.Pow(val, 3) / smoothingDenom;
    }

    float DensityAt(Vector3 point)
    {
        queryResult.Clear();
        spatialHashInstance.Query(point, smoothingRadius, queryResult);
        float densitySum = 0f;
        for (int i = 0; i < queryResult.Count; i++)
        {
            float dist = Vector3.Distance(queryResult[i], point);
            float influence = SmoothingKernel(smoothingRadius, dist);
            densitySum += influence;
        }
        return Mathf.Max(densitySum, minDensity);
    }
}
