using UnityEngine;
using static controlableElement;
using System.Collections.Generic;

public class airElementScript : MonoBehaviour
{


    //internal bool isControllable = false;
   // int myIndex;
    //private cloudScript theCloud;

    [SerializeField] 



    // Increase density update interval to 0.1 seconds.



    internal void Awake()
    {

    }

    //internal void Start()
    //{
    //    CalculateBounds();
    //}

    //void CalculateBounds()
    //{
    //    if (mainCamera != null)
    //    {
    //        float screenDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
    //        Vector3 centerPoint = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, screenDistance));
    //        float cubeSize = 5.0f;
    //        boundsMin = centerPoint - new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
    //        boundsMax = centerPoint + new Vector3(cubeSize / 2, cubeSize / 2, cubeSize / 2);
    //    }
    //}

    // Density query using spatial hash.


    // Compute smoothing forces using spatial hash.





    // Collision resolution using spatial hash.


    internal void Update()
    {



        //ApplyVortexCenters();


    }





    //void ApplyVortexCenters()
    //{
    //    if (controlableElement.Instance == null) return;
    //    foreach (VortexCenter vortex in controlableElement.Instance.vortexCenters)
    //    {
    //        Vector3 toCenter = transform.position - vortex.position;
    //        float dist = toCenter.magnitude;
    //        if (dist < vortex.radius)
    //        {
    //            Vector3 swirlAxis = Vector3.forward;
    //            Vector3 swirlDir = Vector3.Cross(toCenter, swirlAxis).normalized;
    //            float closeness = 1f - (dist / vortex.radius);
    //            float swirlMag = vortex.swirlStrength * closeness;
    //            Vector3 swirlForce = swirlDir * swirlMag;
    //            acceleration += swirlForce / mass;
    //        }
    //    }
    //}

    //internal void yourPositionIs(int index, cloudScript cloudScript)
    //{
    //    myIndex = index;
    //    theCloud = cloudScript;
    //}


}
