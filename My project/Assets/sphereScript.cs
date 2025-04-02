using UnityEngine;

public class sphereScript : MonoBehaviour
{
    public float Radius
    {
        get { return transform.localScale.x / 2; }
        internal set
        { transform.localScale = (value / 2f) * Vector3.one; }
    }
    private Vector3 offset;
    private Camera cam;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
    }
    void OnMouseDown()
    {
        // Calculate the offset between the sphere's position and the mouse's world position.
        offset = transform.position - GetMouseWorldPos();
    }
    void OnMouseDrag()
    {
        // Update the sphere's position based on the mouse position plus the initial offset.
        transform.position = GetMouseWorldPos() + offset;
    }

    private Vector3 GetMouseWorldPos()
    {
        // Capture the current mouse position in screen space.
        Vector3 mousePoint = Input.mousePosition;
        // Set the Z coordinate so that we get the correct world position relative to the camera's distance.
        mousePoint.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        // Convert and return the position.
        return cam.ScreenToWorldPoint(mousePoint);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
