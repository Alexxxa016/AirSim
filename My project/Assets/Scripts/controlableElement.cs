using UnityEngine;

public class controlableElement : airElementScript
{
    float forceRadius = 3f;
    float forceStrength = 5f;

    void Start()
    {
        base.Start();
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = Color.red;
    }

    internal void Update()
    {
        HandleMovement();
        ApplyForceToNearbyParticles();
        base.Update();
    }

    void HandleMovement()
    {
        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.UpArrow)) direction += Vector3.up;
        if (Input.GetKey(KeyCode.DownArrow)) direction += Vector3.down;
        if (Input.GetKey(KeyCode.LeftArrow)) direction += Vector3.left;
        if (Input.GetKey(KeyCode.RightArrow)) direction += Vector3.right;

        transform.position += direction * Time.deltaTime * 5;
    }

    void ApplyForceToNearbyParticles()
    {
        Collider[] nearbyParticles = Physics.OverlapSphere(transform.position, forceRadius);
        foreach (Collider col in nearbyParticles)
        {
            airElementScript particle = col.GetComponent<airElementScript>();
            if (particle != null && particle != this)
            {
                Vector3 forceDir = (particle.transform.position - transform.position).normalized;
                particle.velocity += forceDir * forceStrength * Time.deltaTime;
            }
        }
    }
}
