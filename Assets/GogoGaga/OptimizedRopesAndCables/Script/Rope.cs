using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GogoGaga.OptimizedRopesAndCables
{
    [ExecuteAlways]
    [RequireComponent(typeof(LineRenderer))]
    public class Rope : MonoBehaviour
    {
        public event Action OnPointsChanged;

        [Header("Rope Transforms")]
        [Tooltip("The rope will start at this point")]
        [SerializeField] private Transform startPoint;
        public Transform StartPoint => startPoint;

        [Tooltip("This will move at the center hanging from the rope, like a necklace, for example")]
        [SerializeField] private Transform midPoint;
        public Transform MidPoint => midPoint;

        [Tooltip("The rope will end at this point")]
        [SerializeField] private Transform endPoint;
        public Transform EndPoint => endPoint;

        [Header("Uç / orta transform (Inspector)")]
        [Tooltip("false: Auto Return uç transforma pozisyon yazmaz (XR/fizik uçları taşır; ip sadece okur). true: kinematik dönüşte endPoint.position hareket eder")]
        [SerializeField] private bool ropeMutatesEndTransform = false;
        [Tooltip("false: midPoint transform pozisyonu güncellenmez (sabit kalır). true: eğrideki kontrol noktasını takip eder (eski paket davranışı)")]
        [SerializeField] private bool driveMidPointTransform = false;

        [Header("Rope Settings")]
        [Tooltip("How many points should the rope have, 2 would be a triangle with straight lines, 100 would be a very flexible rope with many parts")]
        [Range(2, 100)] public int linePoints = 10;

        [Tooltip("Value highly dependent on use case, a metal cable would have high stiffness, a rubber rope would have a low one")]
        public float stiffness = 1800f;

        [Tooltip("0 is no damping, 50 is a lot. Higher = less wobble, calmer cable")]
        public float damping = 46f;

        [Tooltip("Orta kontrol noktasının max hızı (0 = sınır yok). Ani sarsıntılarda ipin uçmasını keser")]
        [Min(0f)] public float maxMidVelocity = 5f;

        [Tooltip("Her fizik adımında orta noktayı hedefe doğrudan yaklaştırır (0 = sadece yay). Yüksek = kablo gibi daha sabit")]
        [Range(0f, 1f)] public float midHardSnap = 0.28f;

        [Tooltip("How long is the rope, it will hang more or less from starting point to end point depending on this value")]
        public float ropeLength = 15;

        [Tooltip("The Rope width set at start (changing this value during run time will produce no effect)")]
        public float ropeWidth = 0.1f;

        [Header("Collision Settings")]
        [Tooltip("İpin collider'larla çarpışmasını etkinleştirir")]
        [SerializeField] private bool enableCollision = true;
        [Tooltip("Gerçekçi kablo ağırlığı (Yere Raycast ile yapışma/sarkma)")]
        [SerializeField] private bool enableGroundRaycast = true;
        [Tooltip("Kablo yere çarptığında ne kadar sertçe yere yapışacak (0.1 yumuşak, 1.0 anında)")]
        [Range(0f, 1f)][SerializeField] private float groundSagStrength = 0.5f;
        [Tooltip("Yere çarpma testi için raycast mesafesi")]
        [SerializeField] private float groundRaycastLength = 5f;
        [Tooltip("Çarpışma kontrolü yapılacak layer'lar")]
        [SerializeField] private LayerMask collisionLayers = ~0;
        [Tooltip("İpin yüzey üzerinde tutulacağı mesafe")]
        [SerializeField] private float collisionOffset = 0.02f;
        [Tooltip("Nüfuz çözümü için iterasyon sayısı")]
        [Range(1, 10)][SerializeField] private int collisionIterations = 5;
        [Tooltip("Start/End point yakınındaki noktalara collision uygulanmaz")]
        [Range(0f, 0.4f)][SerializeField] private float endpointDeadzone = 0.15f;

        [Header("Collision Improvements")]
        [Tooltip("Use the hit normal to resolve collisions instead of just pushing UP (Y-axis).")]
        public bool useNormalForCollision = true;

        [Header("Rational Bezier Weight Control")]
        [Tooltip("Adjust the middle control point weight for the Rational Bezier curve")]
        [Range(1, 15)] public float midPointWeight = 1f;
        private const float StartPointWeight = 1f;
        private const float EndPointWeight = 1f;

        [Header("Midpoint Position")]
        [Tooltip("Position of the midpoint along the line between start and end points")]
        [Range(0.25f, 0.75f)] public float midPointPosition = 0.5f;

        [Header("Distance Enforcement")]
        [Tooltip("Maximum allowed physical distance between Start and End points")]
        public float maxDistance = 10f;
        [Tooltip("Enforce maximum physical distance between Start and End points")]
        public bool enforceMaxDistance = true;
        [Tooltip("Kablo max uzunluğa ulaştığında bu obje sürüklenir (Inspector'dan ata)")]
        public Transform dragTarget;
        [Tooltip("Max mesafe aşıldığında tek FixedUpdate'te eklenebilecek düzeltme hızı üst sınırı (m/s). Yüksek değer lastik gibi fırlatır")]
        [Min(0.5f)] public float maxDistanceCorrectionSpeed = 8f;

        [Header("Auto Return")]
        [Tooltip("Bırakıldıktan sonra kablo başlangıç konumuna döner")]
        public bool enableAutoReturn = true;
        [Tooltip("Geri dönmeden önce bekleme süresi (saniye)")]
        public float autoReturnDelay = 2.0f;
        [Tooltip("Geri dönme genel hız çarpanı (kinematik uç için SmoothDamp, rigidbody için hedef hız ölçeği)")]
        public float autoReturnSpeed = 0.85f;
        [Tooltip("Rigidbody eve dönerken max lineer hız (m/s)")]
        [Min(0.05f)] public float autoReturnMaxSpeed = 0.5f;
        [Tooltip("Rigidbody hızının hedefe yaklaşma katsayısı (düşük = daha yumuşak, örn. 1.5–3)")]
        [Min(0.25f)] public float autoReturnVelocityBlend = 2.4f;
        [Tooltip("Bu mesafenin altındaysa 'yerine geldi' sayılır")]
        public float returnedThreshold = 0.15f;
        [Tooltip("Dışarıdan true yapılırsa kablo geri dönmez (sokete takılı vb.)")]
        public bool isEndpointAttached = false;

        [Header("Bırakma (tut / bırak)")]
        [Tooltip("Bırakınca uç rigidbody hızı bu oranla çarpılır (0–1). Küçük = daha az lastik sıçraması")]
        [Range(0f, 1f)] public float releaseBodyVelocityDamp = 0.38f;
        [Tooltip("Bırakınca ip eğrisi orta nokta hızı bu oranla sönümlenir")]
        [Range(0f, 1f)] public float releaseMidCurveDamp = 0.22f;

        // --- private state ---
        private Vector3 currentValue;
        private Vector3 currentVelocity;
        private Vector3 targetValue;
        public Vector3 otherPhysicsFactors { get; set; }
        private const float valueThreshold = 0.035f;
        private const float velocityThreshold = 0.035f;

        private LineRenderer lineRenderer;
        private bool isFirstFrame = true;

        private Vector3 prevStartPointPosition;
        private Vector3 prevEndPointPosition;
        private float prevMidPointPosition;
        private float prevMidPointWeight;
        private float prevLineQuality;
        private float prevRopeWidth;
        private float prevstiffness;
        private float prevDampness;
        private float prevRopeLength;
        private float prevMidHardSnap;

        private Vector3[] cachedPoints;

        // Auto-return state
        private bool _isEndpointHeld = false;
        private float _autoReturnTimer = 0f;
        private bool _isReturning = false;
        private Vector3 _returnSmoothVelocity = Vector3.zero;

        // Cached references
        private Rigidbody _cachedEndRb;
        private Rigidbody _cachedDragRb;
        private Vector3 _endInitialWorldPos;

        public bool IsPrefab => gameObject.scene.rootCount == 0;

        // ===================== LIFECYCLE =====================

        private void Start()
        {
            InitializeLineRenderer();
            if (AreEndPointsValid())
            {
                currentValue = GetMidPoint();
                targetValue = currentValue;
                currentVelocity = Vector3.zero;
                SetSplinePoint();
            }

            if (endPoint != null)
                _cachedEndRb = endPoint.GetComponentInParent<Rigidbody>();

            if (dragTarget != null)
                _cachedDragRb = dragTarget.GetComponent<Rigidbody>() ?? dragTarget.GetComponentInParent<Rigidbody>();
            else if (startPoint != null)
                _cachedDragRb = startPoint.GetComponentInParent<Rigidbody>();

            // Oyun başındaki endpoint dünya pozisyonunu kaydet
            if (_cachedEndRb != null)
                _endInitialWorldPos = _cachedEndRb.position;
            else if (endPoint != null)
                _endInitialWorldPos = endPoint.position;
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                InitializeLineRenderer();
                if (AreEndPointsValid())
                {
                    RecalculateRope();
                    SimulatePhysics();
                }
                else
                {
                    lineRenderer.positionCount = 0;
                }
            }
        }

        private void InitializeLineRenderer()
        {
            if (!lineRenderer)
                lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.startWidth = ropeWidth;
            lineRenderer.endWidth = ropeWidth;
        }

        private void Update()
        {
            if (IsPrefab) return;

            if (AreEndPointsValid())
            {
                SetSplinePoint();

                if (!Application.isPlaying && (IsPointsMoved() || IsRopeSettingsChanged()))
                {
                    SimulatePhysics();
                    NotifyPointsChanged();
                }

                prevStartPointPosition = startPoint.position;
                prevEndPointPosition = endPoint.position;
                prevMidPointPosition = midPointPosition;
                prevMidPointWeight = midPointWeight;
                prevLineQuality = linePoints;
                prevRopeWidth = ropeWidth;
                prevstiffness = stiffness;
                prevDampness = damping;
                prevRopeLength = ropeLength;
                prevMidHardSnap = midHardSnap;
            }
        }

        private void FixedUpdate()
        {
            if (IsPrefab) return;

            if (AreEndPointsValid())
            {
                if (!isFirstFrame)
                    SimulatePhysics();
                isFirstFrame = false;

                if (Application.isPlaying)
                {
                    if (enforceMaxDistance)
                        EnforceMaxDistance();
                    if (enableAutoReturn)
                        HandleAutoReturn();
                }
            }
        }

        // ===================== PUBLIC API =====================

        public void SetEndpointHeld(bool held)
        {
            bool wasHeld = _isEndpointHeld;
            _isEndpointHeld = held;
            if (held)
            {
                _isReturning = false;
                _autoReturnTimer = 0f;
            }
            else if (wasHeld && Application.isPlaying)
                ApplyReleaseDamping();
        }

        private void ApplyReleaseDamping()
        {
            if (releaseMidCurveDamp > 0f)
                currentVelocity *= Mathf.Clamp01(releaseMidCurveDamp);

            float bodyDamp = Mathf.Clamp01(releaseBodyVelocityDamp);
            if (bodyDamp <= 0f)
                return;

            void DampRb(Rigidbody rb)
            {
                if (rb == null || rb.isKinematic) return;
                rb.linearVelocity *= bodyDamp;
                rb.angularVelocity *= bodyDamp;
            }

            DampRb(_cachedEndRb);
            if (_cachedDragRb != null && _cachedDragRb != _cachedEndRb)
                DampRb(_cachedDragRb);
        }

        public void SetStartPoint(Transform newStartPoint, bool instantAssign = false)
        {
            startPoint = newStartPoint;
            prevStartPointPosition = startPoint == null ? Vector3.zero : startPoint.position;
            if (instantAssign || newStartPoint == null) RecalculateRope();
            NotifyPointsChanged();
        }

        public void SetMidPoint(Transform newMidPoint, bool instantAssign = false)
        {
            midPoint = newMidPoint;
            prevMidPointPosition = midPoint == null ? 0.5f : midPointPosition;
            if (instantAssign || newMidPoint == null) RecalculateRope();
            NotifyPointsChanged();
        }

        public void SetEndPoint(Transform newEndPoint, bool instantAssign = false)
        {
            endPoint = newEndPoint;
            prevEndPointPosition = endPoint == null ? Vector3.zero : endPoint.position;
            if (instantAssign || newEndPoint == null) RecalculateRope();
            NotifyPointsChanged();
        }

        public Vector3 GetPointAt(float t)
        {
            if (!AreEndPointsValid())
            {
                Debug.LogError("StartPoint or EndPoint is not assigned.", gameObject);
                return Vector3.zero;
            }

            if (cachedPoints != null && cachedPoints.Length == linePoints + 1 && Application.isPlaying && enableCollision)
            {
                float fIndex = t * linePoints;
                int index = Mathf.FloorToInt(fIndex);
                if (index >= linePoints) return cachedPoints[linePoints];
                if (index < 0) return cachedPoints[0];
                return Vector3.Lerp(cachedPoints[index], cachedPoints[index + 1], fIndex - index);
            }

            return GetRationalBezierPoint(startPoint.position, currentValue, endPoint.position, t, StartPointWeight, midPointWeight, EndPointWeight);
        }

        public void RecalculateRope()
        {
            if (!AreEndPointsValid())
            {
                lineRenderer.positionCount = 0;
                return;
            }
            currentValue = GetMidPoint();
            targetValue = currentValue;
            currentVelocity = Vector3.zero;
            SetSplinePoint();
        }

        // ===================== DISTANCE ENFORCEMENT =====================

        private void EnforceMaxDistance()
        {
            float dist = Vector3.Distance(startPoint.position, endPoint.position);
            if (dist <= maxDistance) return;

            float overshoot = dist - maxDistance;
            Vector3 dir = (endPoint.position - startPoint.position).normalized;

            // FixedUpdate'te velocity correction: bir fizik adımında gap'i kapatır,
            // ama joint'leri kırmaz çünkü pozisyon değil hız manipüle ediyoruz.
            Vector3 correctionVelocity = dir * (overshoot / Time.fixedDeltaTime);
            float corrMag = correctionVelocity.magnitude;
            if (corrMag > maxDistanceCorrectionSpeed && corrMag > 1e-5f)
                correctionVelocity *= maxDistanceCorrectionSpeed / corrMag;

            if (_cachedDragRb != null && !_cachedDragRb.isKinematic)
            {
                // Drag target'ı endpoint'e doğru çek
                _cachedDragRb.linearVelocity += correctionVelocity;
            }
            else if (_cachedEndRb != null && !_cachedEndRb.isKinematic)
            {
                // Endpoint'i startPoint'e doğru geri çek
                _cachedEndRb.linearVelocity -= correctionVelocity;
            }
        }

        // ===================== AUTO RETURN =====================

        private void HandleAutoReturn()
        {
            if (_isEndpointHeld || isEndpointAttached)
            {
                _autoReturnTimer = 0f;
                _isReturning = false;
                return;
            }

            Vector3 returnTarget = _endInitialWorldPos;
            Vector3 currentEndPos = _cachedEndRb != null ? _cachedEndRb.position : endPoint.position;
            float distFromHome = Vector3.Distance(currentEndPos, returnTarget);

            // Zaten yerindeyse
            if (distFromHome <= returnedThreshold)
            {
                if (_isReturning && _cachedEndRb != null && !_cachedEndRb.isKinematic)
                {
                    _cachedEndRb.linearVelocity = Vector3.zero;
                    _cachedEndRb.angularVelocity = Vector3.zero;
                }
                _autoReturnTimer = 0f;
                _isReturning = false;
                return;
            }

            _autoReturnTimer += Time.fixedDeltaTime;
            if (_autoReturnTimer >= autoReturnDelay && !_isReturning)
            {
                _isReturning = true;
                _returnSmoothVelocity = Vector3.zero;
                if (_cachedEndRb != null && !_cachedEndRb.isKinematic)
                {
                    _cachedEndRb.linearVelocity = Vector3.zero;
                    _cachedEndRb.angularVelocity = Vector3.zero;
                }
            }

            if (!_isReturning) return;

            if (_cachedEndRb != null && !_cachedEndRb.isKinematic)
            {
                Vector3 toTarget = returnTarget - _cachedEndRb.position;
                float dist = toTarget.magnitude;
                Vector3 dir = dist > 1e-5f ? toTarget / dist : Vector3.zero;
                float speedScale = Mathf.Max(0.2f, autoReturnSpeed);
                float proportional = dist * (0.7f * speedScale);
                float cap = autoReturnMaxSpeed * speedScale;
                Vector3 desiredVelocity = dir * Mathf.Min(proportional, Mathf.Max(0.08f, cap));

                float blend = Mathf.Clamp01(autoReturnVelocityBlend * Time.fixedDeltaTime);
                _cachedEndRb.linearVelocity = Vector3.Lerp(_cachedEndRb.linearVelocity, desiredVelocity, blend);
                _cachedEndRb.angularVelocity *= Mathf.Lerp(0.94f, 0.99f, blend);
            }
            else if (endPoint != null && ropeMutatesEndTransform)
            {
                float smoothTime = Mathf.Max(0.55f, 1.35f / Mathf.Max(0.25f, autoReturnSpeed));
                endPoint.position = Vector3.SmoothDamp(
                    endPoint.position, returnTarget,
                    ref _returnSmoothVelocity, smoothTime);
            }
        }

        // ===================== ROPE VISUALS =====================

        private bool AreEndPointsValid()
        {
            return startPoint != null && endPoint != null;
        }

        private void SetSplinePoint()
        {
            if (lineRenderer.positionCount != linePoints + 1)
                lineRenderer.positionCount = linePoints + 1;
            if (cachedPoints == null || cachedPoints.Length != linePoints + 1)
                cachedPoints = new Vector3[linePoints + 1];

            Vector3 mid = GetMidPoint();
            targetValue = AdjustMidPointForCollisions(mid);
            mid = AdjustMidPointForCollisions(currentValue);

            if (midPoint != null && driveMidPointTransform)
                midPoint.position = GetRationalBezierPoint(startPoint.position, mid, endPoint.position, midPointPosition, StartPointWeight, midPointWeight, EndPointWeight);

            for (int i = 0; i <= linePoints; i++)
                cachedPoints[i] = GetRationalBezierPoint(startPoint.position, mid, endPoint.position, i / (float)linePoints, StartPointWeight, midPointWeight, EndPointWeight);

            if (enableGroundRaycast && Application.isPlaying)
            {
                int deadZone = Mathf.RoundToInt(endpointDeadzone * linePoints);
                for (int i = 1 + deadZone; i < linePoints - deadZone; i++)
                {
                    Vector3 p = cachedPoints[i];
                    // Noktadan aşağı doğru raycast at
                    if (Physics.Raycast(p + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, groundRaycastLength + 0.2f, collisionLayers, QueryTriggerInteraction.Ignore))
                    {
                        float targetY = hit.point.y + (ropeWidth / 2f) + collisionOffset;
                        // Nokta hedefe çok yüksekte değilse yere çek (ağırlık hissi)
                        if (p.y < targetY + 1.5f)
                        {
                            p.y = Mathf.Lerp(p.y, targetY, groundSagStrength);
                        }
                    }
                    cachedPoints[i] = p;
                }
            }

            if (enableCollision && Application.isPlaying)
            {
                int deadZone = Mathf.RoundToInt(endpointDeadzone * linePoints);

                for (int iter = 0; iter < collisionIterations; iter++)
                {
                    for (int i = 1 + deadZone; i < linePoints - deadZone; i++)
                    {
                        Vector3 prev = cachedPoints[i - 1];
                        Vector3 curr = cachedPoints[i];
                        Vector3 dir = curr - prev;
                        float dist = dir.magnitude;
                        if (dist > 0.0001f)
                        {
                            dir /= dist;
                            if (Physics.SphereCast(prev, ropeWidth / 2f, dir, out RaycastHit hit, dist, collisionLayers, QueryTriggerInteraction.Ignore))
                                cachedPoints[i] = hit.point + hit.normal * (ropeWidth / 2f + collisionOffset);
                        }
                    }

                    for (int i = linePoints - 1 - deadZone; i > deadZone; i--)
                    {
                        Vector3 next = cachedPoints[i + 1];
                        Vector3 curr = cachedPoints[i];
                        Vector3 dir = curr - next;
                        float dist = dir.magnitude;
                        if (dist > 0.0001f)
                        {
                            dir /= dist;
                            if (Physics.SphereCast(next, ropeWidth / 2f, dir, out RaycastHit hit, dist, collisionLayers, QueryTriggerInteraction.Ignore))
                                cachedPoints[i] = hit.point + hit.normal * (ropeWidth / 2f + collisionOffset);
                        }
                    }
                }
            }

            // İlk ve son noktayı her zaman endpoint'lere sabitle
            cachedPoints[0] = startPoint.position;
            cachedPoints[linePoints] = endPoint.position;

            for (int i = 0; i <= linePoints; i++)
                lineRenderer.SetPosition(i, cachedPoints[i]);
        }

        private float CalculateYFactorAdjustment(float weight)
        {
            float k = Mathf.Lerp(0.493f, 0.323f, Mathf.InverseLerp(1, 15, weight));
            float w = 1f + k * Mathf.Log(weight);
            return w;
        }

        private Vector3 GetMidPoint()
        {
            Vector3 startPointPosition = startPoint.position;
            Vector3 endPointPosition = endPoint.position;
            Vector3 midpos = Vector3.Lerp(startPointPosition, endPointPosition, midPointPosition);
            float yFactor = (ropeLength - Mathf.Min(Vector3.Distance(startPointPosition, endPointPosition), ropeLength)) / CalculateYFactorAdjustment(midPointWeight);
            midpos.y -= yFactor;
            return midpos;
        }

        private Vector3 AdjustMidPointForCollisions(Vector3 mid)
        {
            if (!enableCollision || !Application.isPlaying)
                return mid;

            Vector3 start = startPoint.position;
            Vector3 end = endPoint.position;

            for (int iter = 0; iter < collisionIterations; iter++)
            {
                bool collisionFound = false;
                float maxHitY = float.NegativeInfinity;
                Vector3 worstNormal = Vector3.up;
                float minDistanceToPlane = float.MaxValue;

                Vector3 prevPoint = start;
                for (int i = 1; i <= linePoints; i++)
                {
                    float t = i / (float)linePoints;
                    Vector3 nextPoint = (i == linePoints)
                        ? end
                        : GetRationalBezierPoint(start, mid, end, t, StartPointWeight, midPointWeight, EndPointWeight);

                    float dist = Vector3.Distance(prevPoint, nextPoint);
                    if (dist > 0.0001f)
                    {
                        Vector3 dir = (nextPoint - prevPoint) / dist;
                        if (Physics.SphereCast(prevPoint, ropeWidth / 2f, dir, out RaycastHit hit, dist, collisionLayers, QueryTriggerInteraction.Ignore))
                        {
                            collisionFound = true;
                            if (useNormalForCollision)
                            {
                                float d = Vector3.Dot(hit.normal, mid - hit.point);
                                if (d < minDistanceToPlane)
                                {
                                    minDistanceToPlane = d;
                                    worstNormal = hit.normal;
                                }
                            }
                            else
                            {
                                if (hit.point.y > maxHitY)
                                    maxHitY = hit.point.y;
                            }
                        }
                    }
                    prevPoint = nextPoint;
                }

                if (!collisionFound) break;

                if (useNormalForCollision)
                {
                    if (minDistanceToPlane < collisionOffset)
                        mid += worstNormal * (collisionOffset - minDistanceToPlane);
                    else
                        mid += worstNormal * 0.02f;
                }
                else
                {
                    float targetY = maxHitY + collisionOffset;
                    if (mid.y < targetY) mid.y = targetY;
                    else mid.y += 0.02f;
                }
            }

            return mid;
        }

        private Vector3 GetRationalBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t, float w0, float w1, float w2)
        {
            Vector3 wp0 = w0 * p0;
            Vector3 wp1 = w1 * p1;
            Vector3 wp2 = w2 * p2;
            float denominator = w0 * Mathf.Pow(1 - t, 2) + 2 * w1 * (1 - t) * t + w2 * Mathf.Pow(t, 2);
            Vector3 point = (wp0 * Mathf.Pow(1 - t, 2) + wp1 * 2 * (1 - t) * t + wp2 * Mathf.Pow(t, 2)) / denominator;
            return point;
        }

        private void SimulatePhysics()
        {
            float dt = Time.fixedDeltaTime;
            float dampingFactor = Mathf.Max(0f, 1f - damping * dt);
            Vector3 error = targetValue - currentValue;
            Vector3 acceleration = error * stiffness * dt;
            currentVelocity = currentVelocity * dampingFactor + acceleration + otherPhysicsFactors;

            if (maxMidVelocity > 0f && currentVelocity.sqrMagnitude > maxMidVelocity * maxMidVelocity)
                currentVelocity = currentVelocity.normalized * maxMidVelocity;

            currentValue += currentVelocity * dt;

            if (midHardSnap > 0f)
            {
                float t = Mathf.Clamp01(midHardSnap);
                currentValue = Vector3.Lerp(currentValue, targetValue, t);
                currentVelocity *= 1f - t * 0.5f;
            }

            Vector3 residual = targetValue - currentValue;
            if (residual.sqrMagnitude < valueThreshold * valueThreshold && currentVelocity.sqrMagnitude < velocityThreshold * velocityThreshold)
            {
                currentValue = targetValue;
                currentVelocity = Vector3.zero;
            }
        }

        private void OnDrawGizmos()
        {
            if (!AreEndPointsValid()) return;
        }

        private void NotifyPointsChanged()
        {
            OnPointsChanged?.Invoke();
        }

        private bool IsPointsMoved()
        {
            return startPoint.position != prevStartPointPosition || endPoint.position != prevEndPointPosition;
        }

        private bool IsRopeSettingsChanged()
        {
            return !Mathf.Approximately(linePoints, prevLineQuality)
                || !Mathf.Approximately(ropeWidth, prevRopeWidth)
                || !Mathf.Approximately(stiffness, prevstiffness)
                || !Mathf.Approximately(damping, prevDampness)
                || !Mathf.Approximately(ropeLength, prevRopeLength)
                || !Mathf.Approximately(midPointPosition, prevMidPointPosition)
                || !Mathf.Approximately(midPointWeight, prevMidPointWeight)
                || !Mathf.Approximately(midHardSnap, prevMidHardSnap);
        }
    }
}
