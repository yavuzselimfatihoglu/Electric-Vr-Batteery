using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public class ReturnToOrigin : MonoBehaviour
{
    [Header("Ayarlar")]
    public float geriDonmeSuresi = 3f; 
    public float mesafeToleransi = 0.1f; 
    public float hizEsigi = 0.05f; 
    private Vector3 startPosition;
    private Quaternion startRotation;
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private float sayac = 0f;
    private bool eldeMi = false;
    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(x => eldeMi = true);
        grabInteractable.selectExited.AddListener(x => eldeMi = false);
    }
    void Update()
    {
        if (eldeMi || SnapTarget.IsSnapped(transform))
        {
            sayac = 0f;
            return;
        }
        float mesafe = Vector3.Distance(transform.position, startPosition);         
        bool hareketEdiyorMu = rb != null && rb.linearVelocity.magnitude > hizEsigi;
        if (mesafe > mesafeToleransi)
        {
            sayac += Time.deltaTime;
            if (sayac >= geriDonmeSuresi)
            {
                ResetToStart();
            }
        }
        else
        {            
            sayac = 0f;
            if (!hareketEdiyorMu && mesafe < 0.01f) 
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
    private void ResetToStart()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep(); 
        }
        sayac = 0f;
    }
}