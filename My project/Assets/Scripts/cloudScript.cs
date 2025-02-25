using System.Collections.Generic;
using UnityEngine;

public class cloudScript : MonoBehaviour
{
    public GameObject AirElementCloneTemplate;
    int dim = 6;
    float cubeSize = 2.5f;
    Vector3 boundsMin, boundsMax;
    List<GameObject> airElements = new List<GameObject>();

    void Start()
    {
        GenerateCloud();
        CalculateBounds();
    }

    void GenerateCloud()
    {
        boundsMin = new Vector3(-cubeSize / 2, -cubeSize / 2, -cubeSize / 2);
        boundsMax = new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);

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
                    GameObject a = Instantiate(AirElementCloneTemplate, pos, Quaternion.identity);
                    airElements.Add(a);
                }
            }
        }
    }

    void CalculateBounds()
    {
        Camera cam = Camera.main;
        if (cam)
        {
            float d = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 c = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, d));
            float s = cubeSize;
            boundsMin = c - new Vector3(s / 2, s / 2, s / 2);
            boundsMax = c + new Vector3(s / 2, s / 2, s / 2);
        }
    }
}
