using UnityEngine;

public class sphereScript : MonoBehaviour
{
    public float Radius
    {
        get { return transform.localScale.x / 2; }
        internal set { transform.localScale = (value / 2f) * Vector3.one; }
    }

    // Public property for current velocity.
    public Vector3 CurrentVelocity { get; private set; }
    private Vector3 lastPosition;
    private Camera cam;
    private Vector3 offset;

    void Start()
    {
        cam = Camera.main;
        lastPosition = transform.position;
    }

    void Update()
    {
        // Update velocity based on change in position.
        CurrentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        lastPosition = transform.position;
    }

    void OnMouseDown()
    {
        // Calculate the offset between the sphere's position and the mouse's world position.
        offset = transform.position - GetMouseWorldPos();
    }

    void OnMouseDrag()
    {
        // Update the sphere's position based on the mouse position plus the offset.
        transform.position = GetMouseWorldPos() + offset;
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        // Set the Z coordinate so that we get the correct world position relative to the camera.
        mousePoint.z = Mathf.Abs(cam.transform.position.z - transform.position.z);
        return cam.ScreenToWorldPoint(mousePoint);
    }
}
