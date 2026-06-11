using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using GogoGaga.OptimizedRopesAndCables;
[RequireComponent(typeof(XRGrabInteractable))]
public class KabloGrabHighlight : MonoBehaviour
{
    public List<SnapTarget> snapTargetlar;
    public bool otomatikBul = true;
    public KabloYonOku yonOku;
    [Header("Bağlı değilken eve dönüş (parent ile)")]
    [Tooltip("Hiçbir sokete takılı değilken bırakınca kayıtlı transformlar başlangıç yerel pozlarına döner")]
    public bool eveDonWhenNotSnapped = true;
    [Tooltip("Yerel pose kaydı başlayan kök (boş = bu obje). Genelde kablo grubunun üst boş objesi")]
    public Transform eveDonKok;
    [Tooltip("0: sadece kök, 1: kök + bir üst parent, 2: +2 üst ... (üst motoru da oynatmamak için dikkat)")]
    [Range(0, 8)] public int ustParentCaptureDerinligi = 1;
    [Tooltip("Eve dönüş animasyon süresi (saniye)")]
    public float eveDonSure = 2.2f;
    [Header("Eve highlight")]
    [Tooltip("Eve vardıktan sonra bağlanmamış soket highlight'ının kalma süresi (saniye)")]
    public float eveHighlightSuresi = 4f;
    private XRGrabInteractable _grabInteractable;
    private Rope _rope;
    private Coroutine _eveDonCoroutine;
    private readonly List<HomePoseRecord> _homeRecords = new List<HomePoseRecord>();
    private sealed class HomePoseRecord
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public Transform Parent;
    }
    private void Awake()
    {
        _grabInteractable = GetComponent<XRGrabInteractable>();
        _rope = GetComponent<Rope>() ?? GetComponentInParent<Rope>();
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }
    }
    private void Start()
    {
        if (otomatikBul && (snapTargetlar == null || snapTargetlar.Count == 0))
            snapTargetlar = new List<SnapTarget>(FindObjectsByType<SnapTarget>(FindObjectsSortMode.None));
        CaptureHomeTransforms();
    }
    private void OnEnable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.AddListener(OnGrabbed);
            _grabInteractable.selectExited.AddListener(OnReleased);
        }
    }
    private void OnDisable()
    {
        if (_grabInteractable != null)
        {
            _grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            _grabInteractable.selectExited.RemoveListener(OnReleased);
        }
        if (_eveDonCoroutine != null)
        {
            StopCoroutine(_eveDonCoroutine);
            _eveDonCoroutine = null;
        }
    }
    public void CaptureHomeTransforms()
    {
        _homeRecords.Clear();
        Transform start = eveDonKok != null ? eveDonKok : transform;
        Transform t = start;
        for (int d = 0; d <= ustParentCaptureDerinligi && t != null; d++)
        {
            _homeRecords.Add(new HomePoseRecord
            {
                Transform = t,
                LocalPosition = t.localPosition,
                LocalRotation = t.localRotation,
                LocalScale = t.localScale,
                Parent = t.parent
            });
            t = t.parent;
        }
    }
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (_eveDonCoroutine != null)
        {
            StopCoroutine(_eveDonCoroutine);
            _eveDonCoroutine = null;
        }
        _rope?.SetEndpointHeld(true);
        if (snapTargetlar != null)
        {
            foreach (var target in snapTargetlar)
            {
                if (target != null && !target.isConnected)
                    target.HighlightAc();
            }
        }
        if (yonOku != null)
            yonOku.KabloTutuldu(this, snapTargetlar);
    }
    private void OnReleased(SelectExitEventArgs args)
    {
        if (snapTargetlar != null)
        {
            foreach (var target in snapTargetlar)
            {
                if (target != null)
                    target.HighlightKapat();
            }
        }
        bool birSoketeBaglandi = false;
        if (snapTargetlar != null)
        {
            foreach (var target in snapTargetlar)
            {
                if (target != null && target.isConnected)
                {
                    birSoketeBaglandi = true;
                    break;
                }
            }
        }
        var rb = GetComponent<Rigidbody>();
        if (rb != null && !birSoketeBaglandi)
            rb.isKinematic = false;
        if (yonOku != null && !birSoketeBaglandi)
            yonOku.KabloBirakildi();
        _rope?.SetEndpointHeld(false);
        if (!birSoketeBaglandi && eveDonWhenNotSnapped && _homeRecords.Count > 0)
            _eveDonCoroutine = StartCoroutine(EveDonVeHighlightRoutine(rb));
    }
    private IEnumerator EveDonVeHighlightRoutine(Rigidbody rb)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        if (snapTargetlar != null)
        {
            foreach (var st in snapTargetlar)
            {
                if (st != null && !st.isConnected)
                    st.HighlightAc();
            }
        }
        var lerpFrom = new List<(Vector3 lp, Quaternion lq, Vector3 ls)>(_homeRecords.Count);
        foreach (var r in _homeRecords)
        {
            if (r.Transform == null)
            {
                lerpFrom.Add((Vector3.zero, Quaternion.identity, Vector3.one));
                continue;
            }
            lerpFrom.Add((r.Transform.localPosition, r.Transform.localRotation, r.Transform.localScale));
        }
        float elapsed = 0f;
        while (elapsed < eveDonSure)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / eveDonSure);
            a = a * a * (3f - 2f * a);
            for (int i = 0; i < _homeRecords.Count; i++)
            {
                var rec = _homeRecords[i];
                if (rec.Transform == null || rec.Transform.parent != rec.Parent)
                    continue;
                var fr = lerpFrom[i];
                rec.Transform.localPosition = Vector3.Lerp(fr.lp, rec.LocalPosition, a);
                rec.Transform.localRotation = Quaternion.Slerp(fr.lq, rec.LocalRotation, a);
                rec.Transform.localScale = Vector3.Lerp(fr.ls, rec.LocalScale, a);
            }
            bool bagli = false;
            if (snapTargetlar != null)
            {
                foreach (var st in snapTargetlar)
                {
                    if (st != null && st.isConnected)
                    {
                        bagli = true;
                        break;
                    }
                }
            }
            if (bagli || (_grabInteractable != null && _grabInteractable.isSelected))
            {
                EveDonIptalHighlightVeRb(rb);
                _eveDonCoroutine = null;
                yield break;
            }
            yield return null;
        }
        for (int i = 0; i < _homeRecords.Count; i++)
        {
            var rec = _homeRecords[i];
            if (rec.Transform == null || rec.Transform.parent != rec.Parent)
                continue;
            rec.Transform.localPosition = rec.LocalPosition;
            rec.Transform.localRotation = rec.LocalRotation;
            rec.Transform.localScale = rec.LocalScale;
        }
        _rope?.RecalculateRope();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
        yield return new WaitForSeconds(eveHighlightSuresi);
        if (snapTargetlar != null)
        {
            foreach (var st in snapTargetlar)
            {
                if (st != null && !st.isConnected)
                    st.HighlightKapat();
            }
        }
        _eveDonCoroutine = null;
    }
    private void EveDonIptalHighlightVeRb(Rigidbody rb)
    {
        if (snapTargetlar != null)
        {
            foreach (var st in snapTargetlar)
            {
                if (st != null && !st.isConnected)
                    st.HighlightKapat();
            }
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
    }
}