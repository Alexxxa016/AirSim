using System.Collections.Generic;
using UnityEngine;

public class cloudScript : MonoBehaviour
{
    public GameObject AirElementCloneTemplate;
    int dim = 6;
    float smoothingRadius = 3;
    Vector3 boundsMin, boundsMax;

    // Store references to the instantiated air elements
    List<GameObject> airElements = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        GenerateCloud();
        CalculateBounds();
    }

    private void GenerateCloud()
    {
        float cubeSize = 10.0f;
        boundsMin = new Vector3(-cubeSize / 2, -cubeSize / 2, -cubeSize / 2);
        boundsMax = new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);

        for (int i = 0; i < dim; i++)
        {
            for (int j = 0; j < dim; j++)
            {
                for (int k = 0; k < dim; k++)
                {
                    Vector3 randomPosition = new Vector3(
                        UnityEngine.Random.Range(boundsMin.x, boundsMax.x),
                        UnityEngine.Random.Range(boundsMin.y, boundsMax.y),
                        UnityEngine.Random.Range(boundsMin.z, boundsMax.z)
                    );

                    GameObject airElement = Instantiate(AirElementCloneTemplate, randomPosition, Quaternion.identity);
                    airElements.Add(airElement);
                }
            }
        }
    }

    private void CalculateBounds()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            Vector3 centerPoint = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, screenDistance));
            float cubeSize = 10.0f;
            boundsMin = centerPoint - new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
            boundsMax = centerPoint + new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TestDensityAtRandomPoint();
        }
    }

    private void TestDensityAtRandomPoint()
    {
        float x = UnityEngine.Random.Range(1f, 10f);
        float y = UnityEngine.Random.Range(1f, 10f);
        float z = UnityEngine.Random.Range(1f, 10f);
        float density = DensityAt(new Vector3(x, y, z));
        Debug.Log("Point " + x + " , " + y + " , " + z + " has density " + density);
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
}
