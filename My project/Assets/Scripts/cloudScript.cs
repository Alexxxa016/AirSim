using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

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

    // Start is called before the first frame update
    void Start()
    {
        generateCloud();
    }

    private void generateCloud()
    {
        for (int i = 0; i < dim; i++)
            for (int j = 0; j < dim; j++)
                for (int k = 0; k < dim; k++)
                    Instantiate(AirElementCloneTemplate, new Vector3(i, j, k), Quaternion.identity);

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
