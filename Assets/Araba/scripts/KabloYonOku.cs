using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class KabloYonOku : MonoBehaviour
{
    [Header("UI Referansları")]
    public Canvas canvas;
    public RectTransform okTransform;
    public TextMeshProUGUI mesafeText;
    public TextMeshProUGUI ipucuText;
    [Header("Ayarlar")]
    public float guncellemeSikligi = 0.05f;
    [Header("3D Ok Ayarları")]
    public Transform ok3DTransform;
    public float okKameraUzakligi = 1.5f;
    public float okKameraYuksekligi = 0.5f;
    public float okDonusHizi = 10f;
    public bool kendiEtrafindaDon = true;
    public float kendiEtrafindaDonusHizi = 180f;
    public Vector3 modelRotasyonDuzeltme = new Vector3(90f, 0f, 90f);
    [Header("Mesafe Yazısı (3D)")]
    public TextMeshPro ok3DUzerindekiYazi;
    private KabloGrabHighlight _aktifKablo;
    private List<SnapTarget> _hedefler;
    private float _sonGuncellemeZamani;
    private void Start()
    {
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        if (ok3DTransform != null)
            ok3DTransform.gameObject.SetActive(false);
    }
    public void KabloTutuldu(KabloGrabHighlight kablo, List<SnapTarget> hedefler)
    {
        _aktifKablo = kablo;
        _hedefler = hedefler;
        if (canvas != null)
            canvas.gameObject.SetActive(true);
        if (okTransform != null)
            okTransform.gameObject.SetActive(true);
        if (ok3DTransform != null)
        {
            ok3DTransform.gameObject.SetActive(true);
            if (Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                ok3DTransform.position = cam.position + (cam.forward * okKameraUzakligi) + (cam.up * okKameraYuksekligi);
            }
        }
    }
    public void KabloBirakildi()
    {
        _aktifKablo = null;
        _hedefler = null;
        if (canvas != null)
            canvas.gameObject.SetActive(false);
        if (ok3DTransform != null)
            ok3DTransform.gameObject.SetActive(false);
    }
    private void Update()
    {
        if (_aktifKablo == null || _hedefler == null || _hedefler.Count == 0) return;
        bool uiGuncelle = (Time.time - _sonGuncellemeZamani >= guncellemeSikligi);
        if (uiGuncelle) _sonGuncellemeZamani = Time.time;
        SnapTarget enYakin = null;
        float minMesafe = float.MaxValue;
        foreach (var target in _hedefler)
        {
            if (target != null && !target.isConnected)
            {
                float mesafe = Vector3.Distance(_aktifKablo.transform.position, target.transform.position);
                if (mesafe < minMesafe)
                {
                    minMesafe = mesafe;
                    enYakin = target;
                }
            }
        }
        if (enYakin != null)
        {
            if (mesafeText != null)
                mesafeText.text = $"{minMesafe:F2} m";
            if (ipucuText != null)
                ipucuText.text = $"En yakın: {enYakin.gameObject.name}";
            if (okTransform != null && Camera.main != null)
            {
                Vector3 hedefEkranPos = Camera.main.WorldToScreenPoint(enYakin.transform.position);
                Vector3 ekranMerkezi = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
                bool arkada = hedefEkranPos.z < 0;
                hedefEkranPos.z = 0;
                Vector3 yon = (hedefEkranPos - ekranMerkezi).normalized;
                if (arkada) yon *= -1f;
                float aci = Mathf.Atan2(yon.y, yon.x) * Mathf.Rad2Deg;
                okTransform.rotation = Quaternion.Euler(0, 0, aci - 90f);
            }
            if (ok3DTransform != null && Camera.main != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 hedefPozisyon = cam.position + (cam.forward * okKameraUzakligi) + (cam.up * okKameraYuksekligi);
                if (Vector3.Distance(ok3DTransform.position, hedefPozisyon) > 2f)
                    ok3DTransform.position = hedefPozisyon;
                else
                    ok3DTransform.position = Vector3.Lerp(ok3DTransform.position, hedefPozisyon, Time.deltaTime * 15f);
                Vector3 hedefeDogru = (enYakin.transform.position - cam.position).normalized;
                if (hedefeDogru.sqrMagnitude > 0.001f)
                {
                    Quaternion hedefRotasyon = Quaternion.LookRotation(hedefeDogru);
                    hedefRotasyon *= Quaternion.Euler(modelRotasyonDuzeltme);
                    if (kendiEtrafindaDon)
                        hedefRotasyon *= Quaternion.Euler(0, 0, Time.time * kendiEtrafindaDonusHizi);
                    if (okDonusHizi > 0)
                        ok3DTransform.rotation = Quaternion.Slerp(ok3DTransform.rotation, hedefRotasyon, Time.deltaTime * okDonusHizi);
                    else
                        ok3DTransform.rotation = hedefRotasyon;
                }
                if (ok3DUzerindekiYazi != null)
                {
                    ok3DUzerindekiYazi.text = $"{minMesafe:F2}m";
                    ok3DUzerindekiYazi.transform.rotation = Quaternion.LookRotation(ok3DUzerindekiYazi.transform.position - cam.position);
                }
            }
        }
        else
        {
            if (ipucuText != null) ipucuText.text = "";
            if (mesafeText != null) mesafeText.text = "";
            if (okTransform != null) okTransform.gameObject.SetActive(false);
            if (ok3DTransform != null) ok3DTransform.gameObject.SetActive(false);
        }
    }
}