using UnityEngine;
public class LookAtPlayer : MonoBehaviour
{
    private Transform mainCamera;
    void Start() { mainCamera = Camera.main.transform; }
    void Update()
    {        
        transform.LookAt(transform.position + mainCamera.rotation * Vector3.forward, mainCamera.rotation * Vector3.up);
    }
}