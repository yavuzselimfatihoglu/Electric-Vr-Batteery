using UnityEngine;
using TMPro;
public class HVManager : MonoBehaviour
{
    public static HVManager Instance;
    [Header("Aşama 1: Kalibrasyon Yuvaları")]
    public SnapTarget calibPlus;  
    public SnapTarget calibMinus; 
    [Header("Aşama 2: Ara Konnektör Yuvaları")]
    public SnapTarget testPlus;   
    public SnapTarget testMinus;  
    [Header("Referanslar")]
    public AraKonnektorSistemi araKonnektor;
    public TextMeshProUGUI durumText;
    private void Awake() => Instance = this;
    public void BaglantiDurumunuGuncelle()
    {        
        if (calibPlus != null && calibMinus != null && calibPlus.isConnected && calibMinus.isConnected)
        {
            PinKimligi p1 = calibPlus.GetSnappedPinKimligi();
            PinKimligi p2 = calibMinus.GetSnappedPinKimligi();
            if (p1 != null && p2 != null && p1.grupAdi == "HV_Test_Unitesi" && p2.grupAdi == "HV_Test_Unitesi")
            {
                durumText.text = "12V";
                durumText.color = Color.red;
                return;
            }
        }
    if (testPlus != null && testMinus != null && testPlus.isConnected && testMinus.isConnected)
    {
        string grupA = testPlus.GetComponent<PinKimligi>().grupAdi;
        string grupE = testMinus.GetComponent<PinKimligi>().grupAdi;
        if ((grupA == "hv+" && grupE == "hv-") || (grupA == "hv-" && grupE == "hv+"))
        {           
            if (araKonnektor != null && araKonnektor.bataryayaBagli)
            {
                durumText.text = "12.4V";
                durumText.color = Color.green;
                Debug.Log("<color=cyan>EKRAN GÜNCELLENDİ: 12.4V</color>");
            }
            else
            {              
                durumText.text = "0.0V"; 
                durumText.color = Color.white;
                Debug.Log("<color=yellow>HATA: Pinler takılı ama batarya onayı alınamadı!</color>");
            }
            return;
        }
    }
    durumText.text = "Bağlantı Bekleniyor...";
        durumText.color = Color.white;
    }
}