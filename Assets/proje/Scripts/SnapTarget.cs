using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
public class SnapTarget : MonoBehaviour
{
    [Header("Eventler")]
    public UnityEvent onObjectSnapped; 
    public UnityEvent onObjectUnsnapped;
    [Header("Avometre Ayarı")]
public int probNumarasi;
[Header("Snap Ayarları")]
public Vector3 snapLocalRotation = Vector3.zero; 
    public Transform snappableObject;
    public bool isConnected;
    public Vector3 snapLocalOffset = Vector3.zero;
    public float snapRange = 0.3f;
    [Header("UI Mesaj Ayarı")]
    public TMPro.TextMeshProUGUI mesajText;
    [Header("Magnet")]
    public bool magnetEffect = true;
    public float magnetDuration = 0.15f;
    [Header("Highlight")]
    [Tooltip("Ek vurgu mesh’i (halka vb.). Boş bırakılabilir — o zaman sadece soket mesh’i kullanılır.")]
    public GameObject highlightObject;
    [Tooltip("Soketin kendi mesh’lerini de vurgula (takılacak yeri görmek için genelde açık kalsın).")]
    public bool highlightIncludeSocketMesh = true;
    public bool emissionHighlight = true;
    public Color highlightColor = new Color(0.2f, 1f, 0.35f, 1f);
    [Tooltip("Emission çarpanı — yüksek = daha parlak (Bloom ile çok görünür).")]
    [Range(0f, 20f)] public float emissionSiddeti = 8f;
    [Tooltip("Emission HDR kanal tavanı; bloom ile tüm yüzey bembeyaz oluyorsa düşürün (ör. 3–6).")]
    [Min(0f)] public float maxEmissionChannelValue = 5f;
    [Tooltip("Ana renge ne kadar yaklaşsın (0 = hafif, 1 = tam highlight rengi). preserveBaseColorDuringHighlight kapalıyken kullanılır.")]
    [Range(0f, 1f)] public float baseRenkKarisimi = 0.75f;
    [Tooltip("Açıksa HighlightAc ana/base rengi değiştirmez; sadece emission vurgular. Tutunca materyalin kendi rengi korunur.")]
    public bool preserveBaseColorDuringHighlight = true;
    private Renderer[] _highlightRenderers;
    private Color[] _originalColors;
    private Color[] _originalEmissionColors;
    private bool[] _originalEmissionEnabled;
    private Transform _snappedObject;
    private XRGrabInteractable _snappedInteractable;
    private bool _isMagnetizing = false;
    private static KabloGrabHighlight[] _cachedKablolar;
    private static float _cacheZamani;
    private void Awake()
    {
        EnsureHighlightRendererCache();
    }
    private void Start()
    {
        EnsureHighlightRendererCache();
    }
    private void EnsureHighlightRendererCache()
    {
        if (!emissionHighlight) return;
        RebuildHighlightRendererList();
        if (_highlightRenderers == null || _highlightRenderers.Length == 0) return;
        if (_originalColors == null || _originalColors.Length != _highlightRenderers.Length)
            StoreOriginalColors();
    }
    public void RefreshHighlightRendererCache() => EnsureHighlightRendererCache();
    private void RebuildHighlightRendererList()
    {
        var set = new HashSet<Renderer>();
        if (highlightObject != null)
        {
            foreach (var r in highlightObject.GetComponentsInChildren<Renderer>(true))
                if (r != null) set.Add(r);
        }
        if (highlightIncludeSocketMesh)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != null) set.Add(r);
        }
        _highlightRenderers = set.Count > 0 ? new List<Renderer>(set).ToArray() : null;
    }
    private static Color ReadBaseFromFirstMaterialInstance(Renderer rend, out Material m0)
    {
        m0 = null;
        if (rend == null) return new Color(0.45f, 0.45f, 0.45f, 1f);
        m0 = rend.material;
        if (m0 == null) return new Color(0.45f, 0.45f, 0.45f, 1f);
        if (m0.HasProperty("_BaseColor")) return m0.GetColor("_BaseColor");
        if (m0.HasProperty("_Color")) return m0.GetColor("_Color");
        return new Color(0.45f, 0.45f, 0.45f, 1f);
    }
    private void StoreOriginalColors()
    {
        if (_highlightRenderers == null) return;
        _originalColors = new Color[_highlightRenderers.Length];
        _originalEmissionColors = new Color[_highlightRenderers.Length];
        _originalEmissionEnabled = new bool[_highlightRenderers.Length];
        for (int i = 0; i < _highlightRenderers.Length; i++)
        {
            var rend = _highlightRenderers[i];
            if (rend == null) continue;
            Material m0 = null;
            _originalColors[i] = ReadBaseFromFirstMaterialInstance(rend, out m0);
            if (m0 != null && m0.HasProperty("_EmissionColor"))
            {
                _originalEmissionColors[i] = m0.GetColor("_EmissionColor");
                _originalEmissionEnabled[i] = m0.IsKeywordEnabled("_EMISSION");
            }
        }
    }
   public void HighlightAc()
{
    if (isConnected) return;
    EnsureHighlightRendererCache();
    if (emissionHighlight && _highlightRenderers != null)
    {
        if (_originalColors == null || _originalColors.Length != _highlightRenderers.Length)
            StoreOriginalColors();
        if (_originalColors == null || _originalColors.Length != _highlightRenderers.Length)
            return;
        Color emissive = highlightColor * Mathf.Max(0.5f, emissionSiddeti);
        if (maxEmissionChannelValue > 0f)
        {
            emissive.r = Mathf.Min(emissive.r, maxEmissionChannelValue);
            emissive.g = Mathf.Min(emissive.g, maxEmissionChannelValue);
            emissive.b = Mathf.Min(emissive.b, maxEmissionChannelValue);
        }
        for (int ri = 0; ri < _highlightRenderers.Length; ri++)
        {
            var rend = _highlightRenderers[ri];
            if (rend == null) continue;
            Color origBase = _originalColors[ri];
            foreach (var mat in rend.materials)
            {
                if (mat == null) continue;
                if (!preserveBaseColorDuringHighlight)
                {
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", Color.Lerp(origBase, highlightColor, baseRenkKarisimi));
                    else if (mat.HasProperty("_Color"))
                        mat.SetColor("_Color", Color.Lerp(origBase, highlightColor, baseRenkKarisimi));
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", emissive);
                }
            }
        }
    }
    AvometreSistemi avo = FindFirstObjectByType<AvometreSistemi>();
    PinKimligi pinKimligi = GetComponent<PinKimligi>();
    if (avo != null && avo.kalibrasyonTamamlandi && pinKimligi != null && pinKimligi.grupAdi == "KalibrasyonAvo")
        return;
}
    public void HighlightKapat()
    {
        if (!emissionHighlight || _highlightRenderers == null || _highlightRenderers.Length == 0)
            return;
        if (_originalColors == null || _originalEmissionColors == null || _originalEmissionEnabled == null
            || _originalColors.Length != _highlightRenderers.Length
            || _originalEmissionColors.Length != _highlightRenderers.Length
            || _originalEmissionEnabled.Length != _highlightRenderers.Length)
        {
            return;
        }
        for (int i = 0; i < _highlightRenderers.Length; i++)
        {
            var rend = _highlightRenderers[i];
            if (rend == null) continue;
            Color origBase = _originalColors[i];
            Color origEmi = _originalEmissionColors[i];
            bool emiOn = _originalEmissionEnabled[i];
            foreach (var mat in rend.materials)
            {
                if (mat == null) continue;
                if (!preserveBaseColorDuringHighlight)
                {
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", origBase);
                    else if (mat.HasProperty("_Color")) mat.SetColor("_Color", origBase);
                }
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.SetColor("_EmissionColor", origEmi);
                    if (!emiOn) mat.DisableKeyword("_EMISSION");
                }
            }
        }
    }
   private void Update()
{   
    if (_snappedObject != null)
    {        
        if (_snappedInteractable != null && _snappedInteractable.isSelected)
        {
            Ayir(_snappedObject);
            return;
        }
       Vector3 beklenenPozisyon = transform.TransformPoint(snapLocalOffset);
        Quaternion beklenenRotasyon = transform.rotation * Quaternion.Euler(snapLocalRotation);
       if (!_isMagnetizing)
        {
            if (Vector3.Distance(_snappedObject.position, beklenenPozisyon) > 0.001f)
                _snappedObject.position = beklenenPozisyon;
            if (Quaternion.Angle(_snappedObject.rotation, beklenenRotasyon) > 0.1f)
                _snappedObject.rotation = beklenenRotasyon;
            float distFromSnap = Vector3.Distance(_snappedObject.position, beklenenPozisyon);
            if (distFromSnap > 0.15f)
                Ayir(_snappedObject);
        }
        return;
    }
    if (snappableObject != null)
    {
        TrySnapObject(snappableObject);
    }
    else
    {      
        if (Time.time - _cacheZamani > 0.5f)
        {
            _cachedKablolar = FindObjectsByType<KabloGrabHighlight>(FindObjectsSortMode.None);
            _cacheZamani = Time.time;
        }
        if (_cachedKablolar != null)
        {
            foreach (var kablo in _cachedKablolar)
            {
                if (kablo == null) continue;
                if (TrySnapObject(kablo.transform)) break; 
            }
        }
    }
}
    private static List<SnapTarget> _tumSnapTargetlar = new List<SnapTarget>();
    public static bool IsSnapped(Transform obj)
    {
        foreach (var target in _tumSnapTargetlar)
        {
            if (target != null && target._snappedObject == obj)
                return true;
        }
        return false;
    }
    private void OnEnable()
    {
        if (!_tumSnapTargetlar.Contains(this))
            _tumSnapTargetlar.Add(this);
    }
    private void OnDisable()
    {
        if (_tumSnapTargetlar.Contains(this))
            _tumSnapTargetlar.Remove(this);
    }
    private bool BaskaBirTargetaBagliMi(Transform obj)
    {
        foreach (var target in _tumSnapTargetlar)
        {
            if (target != null && target != this && target._snappedObject == obj)
                return true;
        }
        return false;
    }
    private bool TrySnapObject(Transform target)
    {
        var grab = target.GetComponent<XRGrabInteractable>();
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (grab == null || rb == null) return false;
        if (BaskaBirTargetaBagliMi(target)) 
        {       
            return false; 
        }
        if (!grab.isSelected)
        {
            float dist = Vector3.Distance(target.position, transform.position);
        if (dist <= snapRange)
        {
            SnapYap(target);
            return true;
        }
    }
    return false;
}
    private void SnapYap(Transform obj)
    {
        if (_snappedObject != null) return;
        if (obj.parent != null && obj.parent.GetComponent<SnapTarget>() != null) return;
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        _snappedObject = obj;
        _snappedInteractable = obj.GetComponent<XRGrabInteractable>();
        isConnected = true;
    if (obj.name.ToLower().Contains("fis1") && mesajText != null)
    {      
        mesajText.gameObject.SetActive(true);      
        if (gameObject.name.ToLower().Contains("fisyeri1"))
        {
            mesajText.text = "Gerilim Var";           
        }
    }
        if (_snappedInteractable != null)
        {
            _snappedInteractable.selectEntered.AddListener(TutuluncaAyir);
            if (_snappedInteractable.isSelected)
            {
                var interactor = _snappedInteractable.firstInteractorSelecting;
                if (interactor != null)
                {
                    var mgr = _snappedInteractable.interactionManager;
                    if (mgr != null)
                        mgr.SelectCancel((IXRSelectInteractor)interactor, (IXRSelectInteractable)_snappedInteractable);
                }
            }
            _snappedInteractable.trackPosition = false;
            _snappedInteractable.trackRotation = false;
        }
        HighlightKapat();
        if (magnetEffect)
            StartMagnetCoroutine(obj);
        else
            SnapInstant(obj);
AvometreSistemi avo = FindFirstObjectByType<AvometreSistemi>();
    PinKimligi pin = GetComponent<PinKimligi>();
    if (avo != null && pin != null)
    {        
        avo.BaglantiGuncelle(probNumarasi, pin);
    }
    onObjectSnapped?.Invoke(); 
        if (HVManager.Instance != null) HVManager.Instance.BaglantiDurumunuGuncelle();
    }
    private void StartMagnetCoroutine(Transform obj)
    {
        MonoBehaviour host = _snappedInteractable != null ? _snappedInteractable : obj.GetComponent<MonoBehaviour>();
        if (host != null && host.gameObject.activeInHierarchy)
            host.StartCoroutine(MagnetRoutine(obj));
        else
            SnapInstant(obj);
    }
    private void SnapInstant(Transform obj)
    {
        obj.position = transform.TransformPoint(snapLocalOffset);
        obj.rotation = transform.rotation * Quaternion.Euler(snapLocalRotation);
    }
    private System.Collections.IEnumerator MagnetRoutine(Transform obj)
    {
        _isMagnetizing = true;
        Vector3 startPos = obj.position;
        Quaternion startRot = obj.rotation;
        Quaternion hedefRot = transform.rotation * Quaternion.Euler(snapLocalRotation);
        float t = 0;
        while (t < magnetDuration)
        {
            t += Time.deltaTime;
            float n = Mathf.SmoothStep(0, 1, t / magnetDuration);
            obj.position = Vector3.Lerp(startPos, transform.TransformPoint(snapLocalOffset), n);
            obj.rotation = Quaternion.Slerp(startRot, hedefRot, n);
            yield return null;
        }
        SnapInstant(obj);
        _isMagnetizing = false;
    }
  public void Ayir(Transform snappedObj)
{
    if (_snappedObject == null || _snappedObject != snappedObj) return;
    AvometreSistemi avo = FindFirstObjectByType<AvometreSistemi>();
    if (avo != null) avo.BaglantiKopart(probNumarasi);
    if (mesajText != null)
    {
        mesajText.gameObject.SetActive(false);
    }
    var rb = snappedObj.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.isKinematic = false; 
        rb.useGravity = true;  
    }
    if (_snappedInteractable != null)
    {
        _snappedInteractable.selectEntered.RemoveListener(TutuluncaAyir);
        _snappedInteractable.trackPosition = true;
        _snappedInteractable.trackRotation = true;
    }
    _snappedObject = null;
    _snappedInteractable = null;
    isConnected = false;
    Debug.Log(gameObject.name + " pini tamamen serbest bıraktı.");
onObjectUnsnapped?.Invoke();
if (HVManager.Instance != null)
{
    HVManager.Instance.BaglantiDurumunuGuncelle();
}
}
    private void TutuluncaAyir(SelectEnterEventArgs args)
    {
        if (_snappedObject == null) return;
        Ayir(_snappedObject);
    }
    private void OnDestroy()
    {
        if (_snappedInteractable != null)
            _snappedInteractable.selectEntered.RemoveListener(TutuluncaAyir);
    }
    public PinKimligi GetSnappedPinKimligi()
    {
        return _snappedObject != null ? _snappedObject.GetComponent<PinKimligi>() : null;
    }
}