using System.Collections.Generic;
using UnityEngine;

public class controlableElement : airElementScript
{
    // Static reference so particles can find your vortex centers
    public static controlableElement Instance;

    [Header("Movement & Vacuum")]
    public float moveSpeed = 5f;
    public float vacuumRadius = 6f;     // How far behind it pulls
    public float vacuumStrength = 0.8f;   // How strong the pull is
    private float vacuumFalloff = 0.8f;   // How quickly it fades with distance

    [Header("Vortex Centers (Optional)")]
    public List<VortexCenter> vortexCenters = new List<VortexCenter>();
    public float vortexSpawnInterval = 0.3f;  // Spawn vortex centers a bit more frequently
    public float baseSwirlStrength = 8f;      // Slightly stronger swirl
    public float vortexLifetime = 3f;
    public float vortexRadius = 2f;
    private float vortexSpawnTimer;
    private int swirlSign = 1;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        base.Start();
        // Make it visibly red
        Renderer rend = GetComponent<Renderer>();
        if (rend) rend.material.color = Color.red;
    }

    internal void Update()
    {
        // 1) Move with arrow keys
        HandleMovement();

        // 2) Vacuum effect behind the object (low-pressure zone) with drag-along effect
        ApplyVacuumEffect();

        // 3) (Optional) aerodynamic forces
        ApplyAerodynamicForces();

        // 4) Base update (pressure, collisions, etc.)
        base.Update();

        // 5) Spawn vortex centers for swirling effect
        vortexSpawnTimer += Time.deltaTime;
        if (vortexSpawnTimer >= vortexSpawnInterval)
        {
            vortexSpawnTimer = 0f;
            SpawnVortexCenter();
        }

        // 6) Decay vortex centers
        for (int i = vortexCenters.Count - 1; i >= 0; i--)
        {
            vortexCenters[i].lifetime -= Time.deltaTime;
            if (vortexCenters[i].lifetime <= 0f)
                vortexCenters.RemoveAt(i);
        }
    }

    void HandleMovement()
    {
        Vector3 input = Vector3.zero;
        if (Input.GetKey(KeyCode.UpArrow)) input += Vector3.up;
        if (Input.GetKey(KeyCode.DownArrow)) input += Vector3.down;
        if (Input.GetKey(KeyCode.LeftArrow)) input += Vector3.left;
        if (Input.GetKey(KeyCode.RightArrow)) input += Vector3.right;

        velocity = Vector3.Lerp(velocity, input * moveSpeed, Time.deltaTime * 3f);
        transform.position += velocity * Time.deltaTime;
    }

    void ApplyVacuumEffect()
    {
        // Define a vacuum zone behind the object.
        Collider[] colliders = Physics.OverlapSphere(transform.position, vacuumRadius * 2f);
        // The vacuum target is behind the object.
        Vector3 vacuumTarget = transform.position - velocity.normalized * 1.0f;

        foreach (Collider c in colliders)
        {
            airElementScript particle = c.GetComponent<airElementScript>();
            if (particle != null && particle != this)
            {
                Vector3 relativePos = particle.transform.position - transform.position;
                float dist = relativePos.magnitude;

                // Only affect particles that are behind the object.
                if (Vector3.Dot(relativePos, -velocity.normalized) > 0)
                {
                    // Existing pull effect.
                    float falloff = Mathf.Lerp(vacuumStrength, 0f, dist / (vacuumRadius * vacuumFalloff));
                    Vector3 pullDir = (vacuumTarget - particle.transform.position).normalized;
                    Vector3 pullForce = pullDir * falloff;
                    particle.velocity = Vector3.Lerp(
                        particle.velocity,
                        particle.velocity + pullForce,
                        Time.deltaTime * 8f
                    );
                }

                // NEW: If a particle is close enough to the controllable,
                // gently drag it along by interpolating its velocity toward the controllable's velocity.
                if (dist < vacuumRadius * 0.5f)
                {
                    particle.velocity = Vector3.Lerp(
                        particle.velocity,
                        velocity,
                        Time.deltaTime * 4f // Adjust this factor to change the dragging strength.
                    );
                }
            }
        }
    }

    void ApplyAerodynamicForces()
    {
        // Basic drag
        Vector3 dragForce = -velocity.normalized * 0.5f * dragCoefficient * fluidDensity * velocity.sqrMagnitude * crossSectionalArea;
        acceleration += dragForce / mass;

        // Basic lift
        Vector3 flowDir = velocity.normalized;
        Vector3 liftDir = Vector3.Cross(flowDir, Vector3.forward).normalized; // 3D cross product (swirl around z)
        Vector3 liftForce = liftDir * 0.5f * liftCoefficient * fluidDensity * velocity.sqrMagnitude * crossSectionalArea;
        acceleration += liftForce / mass;
    }

    void SpawnVortexCenter()
    {
        // Spawn behind the object
        Vector3 spawnPos = transform.position - velocity.normalized * 1f;
        float swirl = baseSwirlStrength * swirlSign;
        swirlSign *= -1;
        VortexCenter v = new VortexCenter(spawnPos, swirl, vortexRadius, vortexLifetime);
        vortexCenters.Add(v);
    }
}

public class VortexCenter
{
    public Vector3 position;
    public float swirlStrength;
    public float radius;
    public float lifetime;

    public VortexCenter(Vector3 pos, float swirl, float rad, float life)
    {
        position = pos;
        swirlStrength = swirl;
        radius = rad;
        lifetime = life;
    }
}
