using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    public Transform CameraPlace;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = CameraPlace.position;
    }
}
