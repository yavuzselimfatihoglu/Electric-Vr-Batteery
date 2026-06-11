using UnityEngine;
using TMPro;
public class AvometreSistemi : MonoBehaviour
{
    public TextMeshProUGUI anaEkranText; 
    private AraKonnektorSistemi _araKonnektor;
    private PinKimligi prob1Pin;
    private PinKimligi prob2Pin;
    public bool kalibrasyonTamamlandi = false;
    public int tamamlananTestSayisi = 0;
private void Awake()
    {
        _araKonnektor = Object.FindAnyObjectByType<AraKonnektorSistemi>();
    }
public void BaglantiGuncelle(int probNo, PinKimligi pin)
{    
    if (pin.grupAdi == "Batarya_Cikis")
    {
        KontrolEt();
        return;
    }
    if (prob1Pin == pin || prob2Pin == pin) return;
    if (prob1Pin == null) prob1Pin = pin;
    else if (prob2Pin == null) prob2Pin = pin;
    else
    {
        if (probNo == 1) prob1Pin = pin;
        else prob2Pin = pin;
    }
    Debug.Log($"<color=cyan><b>[TAKILDI]</b></color> {pin.gameObject.name} | Kanal 1: {(prob1Pin != null ? prob1Pin.name : "Boş")} | Kanal 2: {(prob2Pin != null ? prob2Pin.name : "Boş")}");
    KontrolEt();
}
public void BaglantiKopart(int probNo)
{
    if (probNo == 1)
    {
        if (prob1Pin != null) Debug.Log($"<color=red><b>[TEMİZLENDİ]</b></color> Kanal 1 boşaltıldı: {prob1Pin.name}");
        prob1Pin = null;
    }
    else if (probNo == 2)
    {
        if (prob2Pin != null) Debug.Log($"<color=red><b>[TEMİZLENDİ]</b></color> Kanal 2 boşaltıldı: {prob2Pin.name}");
        prob2Pin = null;
    }
    KontrolEt();
}
    private void KontrolEt()
    {
        if (prob1Pin == null || prob2Pin == null)
        {
            if (anaEkranText != null)
            {
                anaEkranText.text = kalibrasyonTamamlandi ? "O.L" : "---";
                anaEkranText.color = Color.white;
            }
            return;
        }
        if (_araKonnektor != null && _araKonnektor.bataryayaBagli)
        {       
            if (CheckPinPair("hv+", "sasiKontrol"))
            {
                anaEkranText.text = "50M ohm"; 
                 anaEkranText.color = Color.green;
                 return;
            }
            else if (CheckPinPair("hv-", "sasiKontrol"))
            {
                anaEkranText.text = "60M ohm"; 
            anaEkranText.color = Color.green;
                return;
            }          
        }
       if (CheckPinPair("KalibrasyonAvo", "KalibrasyonAvo"))
    {
        if (!kalibrasyonTamamlandi) KalibrasyonuGerceklestir();
        else { anaEkranText.text = "0.000"; anaEkranText.color = Color.green; }
        return;
    }
    if (prob1Pin.grupAdi == prob2Pin.grupAdi)
    {
        anaEkranText.text = "EVET"; 
        anaEkranText.color = Color.green;
    }
    else
    {
        anaEkranText.text = "HAYIR";
        anaEkranText.color = Color.red;
    }
    }
    private bool CheckPinPair(string grupA, string grupB)
    {
        return (prob1Pin.grupAdi == grupA && prob2Pin.grupAdi == grupB) ||
               (prob1Pin.grupAdi == grupB && prob2Pin.grupAdi == grupA);
    }
    private void KalibrasyonuGerceklestir()
    {
        kalibrasyonTamamlandi = true;
        anaEkranText.text = "0.000";
        anaEkranText.color = Color.green;
        Debug.Log("<color=yellow><b>[SİSTEM]</b></color> Cihaz kalibre edildi. Artık ölçüme hazırsınız.");        
        Invoke("HazirModunaGec", 2.0f);
    }
    private void HazirModunaGec()
    {      
        if (prob1Pin != null && prob2Pin != null && prob1Pin.grupAdi == "KalibrasyonAvo")
        {
            anaEkranText.text = "0.000";
            anaEkranText.color = Color.green;
        }
        else
        {
            anaEkranText.text = "O.L";
            anaEkranText.color = Color.white;
        }
    }
}