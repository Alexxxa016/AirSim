using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class cloudScript : MonoBehaviour
{

    public GameObject AirElementCloneTemplate;
    Vector3[] positions;
    float[] particleProperties;
    int numParticles;
    Vector3 boundsSize;
    const float mass = 1;
    float targetDensity;
    float pressureMultiplier;
    int dim = 5;


    // Store the original positions of particles
    private Dictionary<GameObject, Vector3> particleOriginalPositions = new Dictionary<GameObject, Vector3>();


    // Start is called before the first frame update
    void Start()
    {
        generateCloud();
      
    }

    private void generateCloud()
    {
        // Define the bounds size as a 10-unit cube (adjust this value as needed)
        float cubeSize = 10.0f;

        Vector3 boundsMin = new Vector3(-cubeSize / 2, -cubeSize / 2, -cubeSize / 2);
        Vector3 boundsMax = new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);

        for (int i = 0; i < dim; i++)
            for (int j = 0; j < dim; j++)
                for (int k = 0; k < dim; k++)
                {
                    // Generate random position within the bounds
                    Vector3 randomPosition = new Vector3(
                        UnityEngine.Random.Range(boundsMin.x, boundsMax.x),
                        UnityEngine.Random.Range(boundsMin.y, boundsMax.y),
                        UnityEngine.Random.Range(boundsMin.z, boundsMax.z)
                    );

                    // Instantiate the particle at the random position
                    Instantiate(AirElementCloneTemplate, randomPosition, Quaternion.identity);
                }

        // No need to instantiate a single particle at the spawn point
        // The original position storage logic has been removed
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            test();
    }






    private void test()
    {
        float x = UnityEngine.Random.Range(1f, 10f);
        float y = UnityEngine.Random.Range(1f, 10f);
        float z = UnityEngine.Random.Range(1f, 10f);
        //  print("Point " + x.ToString() + " , " + y.ToString() + " , " + z.ToString() + " has density " + DensityAt(new Vector3(x, y, z)).ToString());
    }
}
