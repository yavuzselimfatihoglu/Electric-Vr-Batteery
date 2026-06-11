using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
[RequireComponent(typeof(XRGrabInteractable))]
public class DevicePlacementSlots : MonoBehaviour
{
    [System.Serializable]
    public class PlacementSlot
    {
        [Tooltip("Yer işareti transformu. Boşsa placementSnapTarget'ın transformu kullanılır.")]
        public Transform slot;
        [Tooltip("SnapTarget ile aynı emission highlight (HighlightAc/Kapat). Doluysa bu slot için highlightVisual ve aşağıdaki renk alanları kullanılmaz.")]
        public SnapTarget placementSnapTarget;
        [FormerlySerializedAs("yaricap")]
        [Tooltip("Sphere radius when acceptZoneMatchBoundsOf is empty")]
        [Min(0.02f)] public float acceptRadius = 0.22f;
        [Tooltip("If set, accept sphere uses this object's combined Renderer bounds (center + max extent). " +
                 "Often a static desk copy of the device mesh. acceptRadius is ignored for this slot.")]
        public Transform acceptZoneMatchBoundsOf;
        [FormerlySerializedAs("highlightGorsel")]
        [Tooltip("Shown while grabbed — optional")]
        public GameObject highlightVisual;
        [Tooltip("If set with highlightVisual, highlight is positioned/scaled to match this object's bounds under the slot (e.g. same mesh as device).")]
        public Transform highlightBoundsShapeReference;
        [FormerlySerializedAs("hizalaRotasyon")]
        [Tooltip("When snapped, movement root aligns to this slot's pose")]
        public bool alignRotation = true;
        [FormerlySerializedAs("ekSlotLocalOffset")]
        [Tooltip("Offset in slot local space (TransformPoint) — sphere center when not using bounds ref")]
        public Vector3 slotLocalOffset = Vector3.zero;
        [FormerlySerializedAs("ekEulerOffset")]
        public Vector3 eulerOffset = Vector3.zero;
        [Tooltip("Extra padding added to bounds-derived accept radius")]
        [Min(0f)] public float acceptBoundsPadding = 0.02f;
    }
    [Header("Movement")]
    [FormerlySerializedAs("hareketKoku")]
    [Tooltip("Root transform that moves when snapping / returning (default: this object)")]
    public Transform movementRoot;
    [FormerlySerializedAs("mesafeSensoru")]
    [Tooltip("World point used for radius checks (default: movement root). Often device base")]
    public Transform distanceProbe;
    [FormerlySerializedAs("ustParentCaptureDerinligi")]
    [Tooltip("How many parents (including root) participate in stored local poses")]
    [Range(0, 8)] public int parentChainDepth = 0;
    [FormerlySerializedAs("donusSuresi")]
    [Tooltip("Smooth return duration when dropped outside all slots")]
    public float returnDuration = 1.8f;
    [Header("Yerleşim toleransı (XR)")]
    [Tooltip("Kabul yarıçapı bu çarpanla genişletilir (bırakınca el hâlâ hafif hareketliyken)")]
    [Min(1f)] public float acceptRadiusSlack = 1.35f;
    [Tooltip("Bırakınca fizik/XR pozunun oturması için ek bekleme (saniye)")]
    [Min(0f)] public float postReleaseSettleSeconds = 0.08f;
    [Tooltip("True: hem distanceProbe hem movementRoot konumundan en yakın mesafe kullanılır")]
    public bool checkBothProbeAndRoot = true;
    [Header("Slots")]
    [FormerlySerializedAs("yerlesimNoktalari")]
    public PlacementSlot[] placementSlots = System.Array.Empty<PlacementSlot>();
    [Header("Highlight fallback (sadece placementSnapTarget yok ve highlightVisual doluysa)")]
    [Tooltip("Tutunca slot vurgusu (MaterialPropertyBlock)")]
    public Color heldHighlightColor = new Color(0.2f, 1f, 0.28f, 1f);
    [Tooltip("Bırakınca ana renk — alfa düşük = şeffaf")]
    public Color releasedHighlightColor = new Color(1f, 1f, 1f, 0.08f);
    [Tooltip("Tutarken emission = renk * bu katsayı")]
    [Min(0f)] public float heldEmissionMultiplier = 4f;
    [Tooltip("Bırakınca emission çarpanı")]
    [Min(0f)] public float releasedEmissionMultiplier = 0.15f;
    [Tooltip("Açıksa MPB ile _BaseColor/_Color değiştirilmez; sadece emission. Mesh kendi renginde kalır.")]
    public bool slotHighlightPreserveBaseColor = true;
    [Tooltip("ApplyTintTree emission HDR tavanı; bloom ile cihaz bembeyaz oluyorsa düşürün (ör. 2–4). 0 = tavan yok.")]
    [Min(0f)] public float slotHighlightMaxEmissionChannel = 3.5f;
    private static readonly int IdBaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int IdColor = Shader.PropertyToID("_Color");
    private static readonly int IdEmissionColor = Shader.PropertyToID("_EmissionColor");
    private MaterialPropertyBlock _mpb;
    private XRGrabInteractable _grab;
    private Coroutine _returnCoroutine;
    private List<PoseRecord> _lastValidRest;
    private float[] _effectiveAcceptRadius;
    private Vector3[] _acceptSphereCenterLocal;
    private Vector3[] _highlightDefaultLocalPos;
    private Quaternion[] _highlightDefaultLocalRot;
    private Vector3[] _highlightDefaultLocalScale;
    private Vector3[] _highlightTargetLocalPos;
    private Quaternion[] _highlightTargetLocalRot;
    private Vector3[] _highlightTargetLocalScale;
    private bool[] _highlightHasShapeLayout;
    private sealed class PoseRecord
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public Transform Parent;
    }
    private void EnsureLastValidRestList()
    {
        if (_lastValidRest == null)
            _lastValidRest = new List<PoseRecord>();
    }
    private static Transform ResolveSlotTransform(PlacementSlot s)
    {
        if (s == null) return null;
        if (s.slot != null) return s.slot;
        return s.placementSnapTarget != null ? s.placementSnapTarget.transform : null;
    }
    private static bool IsTransformUnder(Transform t, Transform root)
    {
        if (t == null || root == null) return false;
        for (Transform x = t; x != null; x = x.parent)
        {
            if (x == root) return true;
        }
        return false;
    }
    private bool HighlightVisualIsMovementRoot(PlacementSlot s)
    {
        if (s?.highlightVisual == null || movementRoot == null) return false;
        return s.highlightVisual.transform == movementRoot || IsTransformUnder(s.highlightVisual.transform, movementRoot);
    }
    private void TryPlacementSnapHighlight(SnapTarget snap)
    {
        if (snap == null || movementRoot == null) return;
        if (snap.transform == movementRoot || IsTransformUnder(snap.transform, movementRoot))
            return;
        snap.RefreshHighlightRendererCache();
        if (!snap.isConnected)
            snap.HighlightAc();
    }
    private void Awake()
    {
        EnsureLastValidRestList();
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();
        _grab = GetComponent<XRGrabInteractable>();
        if (movementRoot == null)
            movementRoot = transform;
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(OnGrabbed);
            _grab.selectExited.AddListener(OnReleased);
        }
    }
    private void OnDestroy()
    {
        if (placementSlots != null)
        {
            foreach (var s in placementSlots)
            {
                if (s?.placementSnapTarget != null)
                    s.placementSnapTarget.HighlightKapat();
                if (s?.highlightVisual != null)
                    ClearTintTree(s.highlightVisual);
            }
        }
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(OnGrabbed);
            _grab.selectExited.RemoveListener(OnReleased);
        }
    }
    private void OnDisable()
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
        if (placementSlots != null)
        {
            foreach (var s in placementSlots)
            {
                if (s?.placementSnapTarget != null)
                    s.placementSnapTarget.HighlightKapat();
                if (s?.highlightVisual != null)
                    ClearTintTree(s.highlightVisual);
            }
        }
    }
    private void Start()
    {
        EnsureLastValidRestList();
        RebuildSlotCaches();
        RecaptureRestPose();
    }
    public void RecaptureRestPose()
    {
        EnsureLastValidRestList();
        CaptureChain(_lastValidRest);
    }
    public void SonGecerliPozuYenidenYakala() => RecaptureRestPose();
    public void RebuildSlotCaches()
    {
        int n = placementSlots != null ? placementSlots.Length : 0;
        _effectiveAcceptRadius = new float[n];
        _acceptSphereCenterLocal = new Vector3[n];
        _highlightDefaultLocalPos = new Vector3[n];
        _highlightDefaultLocalRot = new Quaternion[n];
        _highlightDefaultLocalScale = new Vector3[n];
        _highlightTargetLocalPos = new Vector3[n];
        _highlightTargetLocalRot = new Quaternion[n];
        _highlightTargetLocalScale = new Vector3[n];
        _highlightHasShapeLayout = new bool[n];
        for (int i = 0; i < n; i++)
        {
            var s = placementSlots[i];
            Transform slotTf = ResolveSlotTransform(s);
            if (slotTf == null)
                continue;
            if (s.acceptZoneMatchBoundsOf != null)
            {
                Bounds b = GetCombinedRendererBounds(s.acceptZoneMatchBoundsOf);
                float ext = Mathf.Max(b.extents.x, b.extents.y, b.extents.z);
                _effectiveAcceptRadius[i] = Mathf.Max(0.02f, ext + s.acceptBoundsPadding);
                _acceptSphereCenterLocal[i] = slotTf.InverseTransformPoint(b.center);
            }
            else
            {
                _effectiveAcceptRadius[i] = s.acceptRadius;
                _acceptSphereCenterLocal[i] = s.slotLocalOffset;
            }
            if (s.placementSnapTarget == null && s.highlightVisual != null)
            {
                Transform h = s.highlightVisual.transform;
                _highlightDefaultLocalPos[i] = h.localPosition;
                _highlightDefaultLocalRot[i] = h.localRotation;
                _highlightDefaultLocalScale[i] = h.localScale;
                _highlightHasShapeLayout[i] = false;
                if (s.highlightBoundsShapeReference != null)
                {
                    Bounds hb = GetCombinedRendererBounds(s.highlightBoundsShapeReference);
                    Vector3 localPos = slotTf.InverseTransformPoint(hb.center);
                    Quaternion localRot = Quaternion.Inverse(slotTf.rotation) * s.highlightBoundsShapeReference.rotation;
                    Vector3 worldSize = hb.size;
                    Vector3 localScale = WorldSizeToLocalScaleLossyApprox(slotTf, worldSize);
                    _highlightTargetLocalPos[i] = localPos;
                    _highlightTargetLocalRot[i] = localRot;
                    _highlightTargetLocalScale[i] = localScale;
                    _highlightHasShapeLayout[i] = true;
                }
            }
        }
    }
    private static Bounds GetCombinedRendererBounds(Transform root)
    {
        if (root == null)
            return new Bounds(Vector3.zero, Vector3.one * 0.1f);
        var rends = root.GetComponentsInChildren<Renderer>(true);
        if (rends == null || rends.Length == 0)
            return new Bounds(root.position, Vector3.one * 0.1f);
        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++)
        {
            if (rends[i] != null)
                b.Encapsulate(rends[i].bounds);
        }
        return b;
    }
    private static Vector3 WorldSizeToLocalScaleLossyApprox(Transform slot, Vector3 worldSize)
    {
        Vector3 ps = slot.lossyScale;
        return new Vector3(
            SafeDiv(worldSize.x, Mathf.Max(1e-4f, ps.x)),
            SafeDiv(worldSize.y, Mathf.Max(1e-4f, ps.y)),
            SafeDiv(worldSize.z, Mathf.Max(1e-4f, ps.z)));
    }
    private static float SafeDiv(float a, float b) => a / Mathf.Max(1e-4f, b);
    private static void ClearTintTree(GameObject root)
    {
        if (root == null) return;
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r != null)
                r.SetPropertyBlock(null);
        }
    }
    private void ApplyTintTree(GameObject root, Color baseColor, float emissionMul)
    {
        if (root == null) return;
        if (_mpb == null)
            _mpb = new MaterialPropertyBlock();
        Color emission = baseColor * emissionMul;
        if (slotHighlightMaxEmissionChannel > 0f)
        {
            emission.r = Mathf.Min(emission.r, slotHighlightMaxEmissionChannel);
            emission.g = Mathf.Min(emission.g, slotHighlightMaxEmissionChannel);
            emission.b = Mathf.Min(emission.b, slotHighlightMaxEmissionChannel);
        }
        foreach (var r in root.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            _mpb.Clear();
            var mats = r.sharedMaterials;
            bool wroteBase = false;
            bool hasEmission = false;
            for (int mi = 0; mi < mats.Length; mi++)
            {
                var m = mats[mi];
                if (m == null) continue;
                if (!slotHighlightPreserveBaseColor && !wroteBase)
                {
                    if (m.HasProperty(IdBaseColor))
                    {
                        _mpb.SetColor(IdBaseColor, baseColor);
                        wroteBase = true;
                    }
                    else if (m.HasProperty(IdColor))
                    {
                        _mpb.SetColor(IdColor, baseColor);
                        wroteBase = true;
                    }
                }
                if (m.HasProperty(IdEmissionColor))
                    hasEmission = true;
            }
            if (hasEmission)
                _mpb.SetColor(IdEmissionColor, emission);
            r.SetPropertyBlock(_mpb);
        }
    }
    private void CaptureChain(List<PoseRecord> list)
    {
        if (list == null)
            return;
        list.Clear();
        if (!transform)
            return;
        Transform start = movementRoot != null ? movementRoot : transform;
        if (!start)
            return;
        Transform t = start;
        for (int d = 0; d <= parentChainDepth && t; d++)
        {
            list.Add(new PoseRecord
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
    private void OnGrabbed(SelectEnterEventArgs _)
    {
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
        if (placementSlots == null || placementSlots.Length == 0)
            return;
        for (int i = 0; i < placementSlots.Length; i++)
        {
            var s = placementSlots[i];
            if (s == null)
                continue;
            if (s.placementSnapTarget != null)
            {
                TryPlacementSnapHighlight(s.placementSnapTarget);
                continue;
            }
            if (s.highlightVisual == null)
                continue;
            if (HighlightVisualIsMovementRoot(s))
            {
                Debug.LogWarning(
                    "DevicePlacementSlots: Bu slotta highlightVisual, movementRoot ile aynı obje. " +
                    "Vurgu tüm cihaza MPB basar ve genelde bembeyaz görünür. " +
                    "highlightVisual olarak sadece ince bir vurgu mesh’i kullanın veya placementSnapTarget ile SnapTarget vurgusu verin.",
                    this);
                continue;
            }
            if (_highlightHasShapeLayout != null && i < _highlightHasShapeLayout.Length && _highlightHasShapeLayout[i])
            {
                Transform h = s.highlightVisual.transform;
                h.localPosition = _highlightTargetLocalPos[i];
                h.localRotation = _highlightTargetLocalRot[i];
                h.localScale = _highlightTargetLocalScale[i];
            }
            s.highlightVisual.SetActive(true);
            ApplyTintTree(s.highlightVisual, heldHighlightColor, heldEmissionMultiplier);
        }
    }
    private void OnReleased(SelectExitEventArgs _)
    {
        if (placementSlots != null)
        {
            for (int i = 0; i < placementSlots.Length; i++)
            {
                var s = placementSlots[i];
                if (s == null)
                    continue;
                if (s.placementSnapTarget != null)
                {
                    s.placementSnapTarget.HighlightKapat();
                    continue;
                }
                if (s.highlightVisual == null)
                    continue;
                if (!HighlightVisualIsMovementRoot(s))
                    ApplyTintTree(s.highlightVisual, releasedHighlightColor, releasedEmissionMultiplier);
                if (_highlightHasShapeLayout != null && i < _highlightHasShapeLayout.Length && _highlightHasShapeLayout[i])
                {
                    Transform h = s.highlightVisual.transform;
                    h.localPosition = _highlightDefaultLocalPos[i];
                    h.localRotation = _highlightDefaultLocalRot[i];
                    h.localScale = _highlightDefaultLocalScale[i];
                }
                s.highlightVisual.SetActive(false);
                ClearTintTree(s.highlightVisual);
            }
        }
        if (placementSlots == null || placementSlots.Length == 0)
            return;
        _returnCoroutine = StartCoroutine(EvaluateAfterRelease());
    }
    private float GetEffectiveAcceptRadius(int index, PlacementSlot s)
    {
        if (_effectiveAcceptRadius != null && index >= 0 && index < _effectiveAcceptRadius.Length)
            return _effectiveAcceptRadius[index];
        return s.acceptRadius;
    }
    private Vector3 GetAcceptSphereCenterWorld(int index, PlacementSlot s)
    {
        Transform slotTf = ResolveSlotTransform(s);
        if (slotTf == null)
            return Vector3.zero;
        if (_acceptSphereCenterLocal != null && index >= 0 && index < _acceptSphereCenterLocal.Length)
            return slotTf.TransformPoint(_acceptSphereCenterLocal[index]);
        return slotTf.TransformPoint(s.slotLocalOffset);
    }
    private IEnumerator EvaluateAfterRelease()
    {
        yield return null;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        if (postReleaseSettleSeconds > 0f)
            yield return new WaitForSeconds(postReleaseSettleSeconds);
        EnsureLastValidRestList();
        Transform root = movementRoot != null ? movementRoot : transform;
        Transform probe = distanceProbe != null ? distanceProbe : root;
        Vector3 probePos = probe.position;
        Vector3 rootPos = root.position;
        PlacementSlot best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < placementSlots.Length; i++)
        {
            var s = placementSlots[i];
            if (ResolveSlotTransform(s) == null)
                continue;
            Vector3 center = GetAcceptSphereCenterWorld(i, s);
            float r = GetEffectiveAcceptRadius(i, s) * acceptRadiusSlack;
            float d = checkBothProbeAndRoot
                ? Mathf.Min(Vector3.Distance(probePos, center), Vector3.Distance(rootPos, center))
                : Vector3.Distance(probePos, center);
            if (d <= r && d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }
        Rigidbody rb = null;
        if (root != null)
            rb = root.GetComponent<Rigidbody>() ?? root.GetComponentInParent<Rigidbody>();
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        if (best != null)
        {
            SnapToSlot(best, root);
            CaptureChain(_lastValidRest);
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            _returnCoroutine = null;
            yield break;
        }
        if (_lastValidRest.Count == 0)
        {
            _returnCoroutine = null;
            yield break;
        }
        yield return ReturnToRestPose(rb);
    }
    private void SnapToSlot(PlacementSlot n, Transform root)
    {
        Transform s = ResolveSlotTransform(n);
        if (s == null || root == null) return;
        Quaternion targetRot = n.alignRotation
            ? s.rotation * Quaternion.Euler(n.eulerOffset)
            : root.rotation;
        Vector3 targetPos = s.TransformPoint(n.slotLocalOffset);
        if (!n.alignRotation)
            targetPos = root.position;
        root.SetPositionAndRotation(targetPos, targetRot);
    }
    private IEnumerator ReturnToRestPose(Rigidbody rb)
    {
        EnsureLastValidRestList();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        var lerpFrom = new List<(Vector3 lp, Quaternion lq, Vector3 ls)>(_lastValidRest.Count);
        foreach (var r in _lastValidRest)
        {
            if (r.Transform == null)
            {
                lerpFrom.Add((Vector3.zero, Quaternion.identity, Vector3.one));
                continue;
            }
            lerpFrom.Add((r.Transform.localPosition, r.Transform.localRotation, r.Transform.localScale));
        }
        float elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Clamp01(elapsed / returnDuration);
            a = a * a * (3f - 2f * a);
            for (int i = 0; i < _lastValidRest.Count; i++)
            {
                var rec = _lastValidRest[i];
                if (rec.Transform == null || rec.Transform.parent != rec.Parent)
                    continue;
                var fr = lerpFrom[i];
                rec.Transform.localPosition = Vector3.Lerp(fr.lp, rec.LocalPosition, a);
                rec.Transform.localRotation = Quaternion.Slerp(fr.lq, rec.LocalRotation, a);
                rec.Transform.localScale = Vector3.Lerp(fr.ls, rec.LocalScale, a);
            }
            if (_grab != null && _grab.isSelected)
            {
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.isKinematic = false;
                }
                _returnCoroutine = null;
                yield break;
            }
            yield return null;
        }
        for (int i = 0; i < _lastValidRest.Count; i++)
        {
            var rec = _lastValidRest[i];
            if (rec.Transform == null || rec.Transform.parent != rec.Parent)
                continue;
            rec.Transform.localPosition = rec.LocalPosition;
            rec.Transform.localRotation = rec.LocalRotation;
            rec.Transform.localScale = rec.LocalScale;
        }
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }
        _returnCoroutine = null;
    }
}