using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
public class AraKonnektorSistemi : MonoBehaviour
{
    private XRGrabInteractable _cihazGrab;
    public SnapTarget[] pinYuvalari; 
    [Header("Kalibrasyon Ayarları")]
    public string kalibrasyonGrupAdi = "KalibrasyonKonnektor";
    public bool kalibrasyonTamamlandi = false;
    [Header("Bağlantı Durumu")]
    public bool bataryayaBagli = false;
    private AvometreSistemi _avo;
    private void Awake()
    {
        _cihazGrab = GetComponent<XRGrabInteractable>();
        _avo = Object.FindAnyObjectByType<AvometreSistemi>();
    }
    private void Update()
    {
        bool herhangiBirPinTakiliMi = false;
        int kalibrasyonPinSayisi = 0;
        foreach (var yuva in pinYuvalari)
        {
            if (yuva.isConnected)
            {
                herhangiBirPinTakiliMi = true;
                PinKimligi pin = yuva.snappableObject != null ? yuva.snappableObject.GetComponent<PinKimligi>() : null;
                if (!kalibrasyonTamamlandi && pin != null && pin.grupAdi == kalibrasyonGrupAdi)
                {
                    kalibrasyonPinSayisi++;
                }
            }
        }
        _cihazGrab.enabled = !herhangiBirPinTakiliMi;
        if (!kalibrasyonTamamlandi && kalibrasyonPinSayisi == 2)
        {
            KonnektorKalibreEt();
        }
    }
    private void KonnektorKalibreEt()
    {
        kalibrasyonTamamlandi = true;
        if (_avo != null)
        {
            _avo.anaEkranText.text = "0.000";
            _avo.anaEkranText.color = Color.green;
            _avo.Invoke("HazirModunaGec", 2.0f);
        }
        Debug.Log("Ara Konnektör Kalibrasyonu Tamamlandı.");
    }
public void BataryayaTakildi() 
{ 
    GameObject yuvaObj = GameObject.Find("Batarya_cikis");
    if (yuvaObj == null) return;
    SnapTarget bataryaYuvasi = yuvaObj.GetComponent<SnapTarget>();
    PinKimligi takilanPin = bataryaYuvasi.GetSnappedPinKimligi();
    if (takilanPin != null)
    {
        if (takilanPin.grupAdi == "Batarya_Cikis" || takilanPin.gameObject.name.Contains("obje1"))
        {
            bataryayaBagli = true; 
            Debug.Log("<color=green>Onaylandı: Batarya bağlantısı sağlandı (Grup: Batarya_Cikis)</color>");
        }
    }
    if(HVManager.Instance != null) HVManager.Instance.BaglantiDurumunuGuncelle();
}
  public void BataryadanCikarildi() 
{ 
    bataryayaBagli = false; 
    Debug.Log("<color=red>Ara Konnektör Bataryadan AYRILDI!</color>");
    if(HVManager.Instance != null) 
    {
        HVManager.Instance.BaglantiDurumunuGuncelle();
    }
}
}