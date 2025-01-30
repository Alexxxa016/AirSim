using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using UnityEngine;

public class airElementScript : MonoBehaviour
{
    float density = 1;
    Vector3 velocity, acceleration;
    float smoothingRadius = 3;
    float mass = 1;
    float targetDensity = 1;
    Vector3 initialPosition;
    Vector3 boundsMin;
    Vector3 boundsMax;
    //float maxSpeed = 0.5f;
    float[] densities;
    private float[] particleProperties;
    private float pressureMultiplier = 1;


    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position; // Save the initial position
        CalculateBounds(); // Calculate the screen boundaries

    }

    void CalculateBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Extend the distance from the camera to the boundary
            float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);

            // Get the frustum's horizontal and vertical size at the particle distance
            Vector3 centerPoint = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, screenDistance));

            // Set the size of the boundary cube (e.g., a 1-unit cube)
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

    Vector3 CalculatePressureForce(Vector3 samplePoint)
    {
        Vector3 pressureForce = Vector3.zero;
        Collider[] allAirElements = Physics.OverlapSphere(samplePoint, smoothingRadius);

        foreach (Collider c in allAirElements)
        {
            float dist = (c.transform.position - samplePoint).magnitude;
            if (dist > 0)
            {
                Vector3 dir = (c.transform.position - samplePoint) / dist;
                float slope = SmoothingKernelDerivative(dist, smoothingRadius);

                pressureForce += -ConvertDensityToPressure(density) * dir * slope * mass / density;
            }
        }
        return pressureForce;
    }

    // Update is called once per frame
    void Update()
    {

        //print(density);

        acceleration = Vector3.zero; // 9.8f * Vector3.down;
        Vector3 pressureForce = CalculatePressureForce(transform.position);
        Vector3 pressureAcceleration;
        if (density == 0)
            pressureAcceleration = Vector3.zero;
        else
            pressureAcceleration = pressureForce / density;

        acceleration += pressureAcceleration;
        //acceleration = 9.8f * Vector3.down;
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        density = DensityAt(transform.position);

        // Boundary collision detection
        if (transform.position.x < boundsMin.x || transform.position.x > boundsMax.x)
        {
            velocity.x = -velocity.x;
        }
        if (transform.position.y < boundsMin.y || transform.position.y > boundsMax.y)
        {
            velocity.y = -velocity.y;
        }
        if (transform.position.z < boundsMin.z || transform.position.z > boundsMax.z)
        {
            velocity.z = -velocity.z;
        }


    }
}
