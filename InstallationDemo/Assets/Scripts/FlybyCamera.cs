using UnityEngine;

public class FlybyCamera : MonoBehaviour
{

    public float movementSpeed = 10f;
    public float fastMovementSpeed = 50f;
    public float rotationSensitivity = 2f;

    private float yaw = 0f;
    private float pitch = 0f;

    void Start()
    {
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        if (Input.GetMouseButton(1))  // Right mouse button (0 = left, 1 = right, 2 = middle)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw += Input.GetAxis("Mouse X") * rotationSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * rotationSensitivity;
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);

            Vector3 direction = new Vector3();

            if (Input.GetKey(KeyCode.W)) direction += transform.forward;
            if (Input.GetKey(KeyCode.S)) direction -= transform.forward;
            if (Input.GetKey(KeyCode.A)) direction -= transform.right;
            if (Input.GetKey(KeyCode.D)) direction += transform.right;
            if (Input.GetKey(KeyCode.Q)) direction -= transform.up;
            if (Input.GetKey(KeyCode.E)) direction += transform.up;

            float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMovementSpeed : movementSpeed;
            transform.position += direction * currentSpeed * Time.deltaTime;
        } else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
