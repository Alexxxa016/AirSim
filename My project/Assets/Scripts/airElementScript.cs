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


    // Start is called before the first frame update
    void Start()
    {

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

    //float CalculateProperty(Vector3 samplePoint)
    //{
    //    float property = 0;
    //    Collider[] allAirElements = Physics.OverlapSphere(samplePoint, smoothingRadius);

    //    foreach (Collider c in allAirElements)
    //    {
    //        float dist = (c.transform.position - samplePoint).magnitude;
    //        float influence = SmoothingKernel(dist, smoothingRadius);
    //        float density = DensityAt(c.transform.position);
    //        property += particleProperties[i] * influence * mass / density * influence;
    //    }
    //    return property;
    //}



    /// <summary>
    /// 
    /// </summary>
    /// <param name="samplePoint"></param>
    /// <returns></returns>
    //Vector3 CalculatePropertyGradient(Vector3 samplePoint)
    //{
    //    Vector3 propertyGradient = Vector3.zero;

    //    for (int i = 0; i < numParticles; i++)
    //    {
    //        float dist = (positions[i] - samplePoint).magnitude;
    //        Vector3 dir = (positions[i] - samplePoint) / dist;
    //        float slope = SmoothingKernelDerivative(dist, smoothingRadius);
    //        float density = densities[i];
    //        propertyGradient += -particleProperties[i] * dir * slope * mass / density;
    //    }
    //    return propertyGradient;
    //}

    float[] densities;
    private float[] particleProperties;
    private float pressureMultiplier = 1;

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
        print(density);

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
    }
}
