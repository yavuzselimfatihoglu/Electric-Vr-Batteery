using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class AutoConnectPlug : MonoBehaviour
{    
    public Transform targetSocket;  
    public UnityEvent onConnected;    
    public float flyDuration = 0.4f;
    private bool isConnected = false;
    private XRBaseInteractable interactable;
    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
    }
    public void SendToSocket()
    {
        if (!isConnected && targetSocket != null)
        {
            StartCoroutine(FlyToSocketCoroutine());
        }
    }
    private IEnumerator FlyToSocketCoroutine()
    {
        isConnected = true;
        if (interactable != null) 
        {
            interactable.enabled = false;
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true; 
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsedTime = 0f;
        while (elapsedTime < flyDuration)
        {           
            transform.position = Vector3.Lerp(startPos, targetSocket.position, elapsedTime / flyDuration);
            transform.rotation = Quaternion.Lerp(startRot, targetSocket.rotation, elapsedTime / flyDuration);            
            elapsedTime += Time.deltaTime;
            yield return null; 
        }       
        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        transform.SetParent(targetSocket); 
        onConnected.Invoke();
    }
}