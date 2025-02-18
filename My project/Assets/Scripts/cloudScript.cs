using System.Collections.Generic;
using UnityEngine;

public class cloudScript : MonoBehaviour
{
    public GameObject AirElementCloneTemplate;
    int dim = 6;
    float smoothingRadius = 3;
    Vector3 boundsMin, boundsMax;
    List<GameObject> airElements = new List<GameObject>();

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
                        Random.Range(boundsMin.x, boundsMax.x),
                        Random.Range(boundsMin.y, boundsMax.y),
                        Random.Range(boundsMin.z, boundsMax.z)
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
}
