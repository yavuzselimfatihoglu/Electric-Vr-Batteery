using UnityEngine;
using TMPro; 
public class FisKontrol : MonoBehaviour
{
    [Header("Hedef Pozisyonlar")]
    public Transform hedefKonum; 
    public float hareketHizi = 5f;
    [Header("UI Ayarları")]
    public TextMeshProUGUI gerilimYazisi;
    public string mesaj = "Gerilim Yok";
    private bool fiisCekildi = false;
    public void FisCekildi()
    {
        fiisCekildi = true;        
        if (gerilimYazisi != null)
        {
            gerilimYazisi.text = mesaj;
            gerilimYazisi.color = Color.red; 
        }
    }
    void Update()
    {
        if (fiisCekildi)
        {
            transform.position = Vector3.Lerp(transform.position, hedefKonum.position, Time.deltaTime * hareketHizi);
            transform.rotation = Quaternion.Lerp(transform.rotation, hedefKonum.rotation, Time.deltaTime * hareketHizi);
            if (Vector3.Distance(transform.position, hedefKonum.position) < 0.01f)
            {
                fiisCekildi = false;
            }
        }
    }
}