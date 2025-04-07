using UnityEngine;

public class CameraScript : MonoBehaviour
{

    [Header("CameraSettings")]
    public float SensX;
    public float SensY;

    public Transform Orintation;

    float xRotation;
    float yRotation;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * SensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * SensY;

        yRotation += mouseX;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        Orintation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
}
