using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
public class MotorParcalari : MonoBehaviour
{
    [Header("Sanal Montaj Ayarları")]
    [Tooltip("Bu parça tutulmadığında hangi objeyi (yuvayı) takip etmeli? Boş bırakılırsa Start'taki parent'ını hedef alır.")]
    public Transform takipHedefi;
    [SerializeField]
    [Tooltip("Bırakınca yuvaya otomatik geri dönsün mü?")]
    private bool birakincaYuvayaDon = true;
    private XRGrabInteractable _grab;
    private Rigidbody _rb;
    private readonly List<Collider> _benimColliderlarim = new List<Collider>();
    private static readonly List<MotorParcalari> KayitliParcalar = new List<MotorParcalari>();
    private bool _carpismaKapatildi;
    private Vector3 _relativePos;
    private Quaternion _relativeRot;
    private bool _takipAktif = false;
    private void Awake()
    {
        _grab = GetComponent<XRGrabInteractable>();
        _rb = GetComponent<Rigidbody>();
        _benimColliderlarim.Clear();
        GetComponentsInChildren(includeInactive: true, _benimColliderlarim);
        if (takipHedefi == null && transform.parent != null)
        {
            takipHedefi = transform.parent;
        }
    }
    private void Start()
    {
        if (takipHedefi != null)
        {
            _relativePos = takipHedefi.InverseTransformPoint(transform.position);
            _relativeRot = Quaternion.Inverse(takipHedefi.rotation) * transform.rotation;
            transform.SetParent(null, true);
            _takipAktif = true;
            _rb.isKinematic = true;
        }
    }
    private void LateUpdate()
    {
        if (_takipAktif && takipHedefi != null && !_grab.isSelected)
        {
            transform.position = takipHedefi.TransformPoint(_relativePos);
            transform.rotation = takipHedefi.rotation * _relativeRot;
        }
    }
    private void OnEnable()
    {
        KayitliParcalar.Add(this);
        if (_grab != null)
        {
            _grab.selectEntered.AddListener(Tutulunca);
            _grab.selectExited.AddListener(Birakilinca);
        }
    }
    private void OnDisable()
    {
        if (_grab != null)
        {
            _grab.selectEntered.RemoveListener(Tutulunca);
            _grab.selectExited.RemoveListener(Birakilinca);
        }
        if (_carpismaKapatildi)
            DigerParcalarlaCarpismayiAc();
        KayitliParcalar.Remove(this);
    }
    private void Tutulunca(SelectEnterEventArgs _)
    {
        _takipAktif = false;
        _rb.isKinematic = false;
        DigerParcalarlaCarpismayiKapat();
    }
    private void Birakilinca(SelectExitEventArgs _)
    {
        if (_grab != null && _grab.isSelected) return;
        DigerParcalarlaCarpismayiAc();
        if (birakincaYuvayaDon && takipHedefi != null)
        {
            _takipAktif = true;
            _rb.isKinematic = true;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }
    private void DigerParcalarlaCarpismayiKapat()
    {
        if (_carpismaKapatildi) return;
        for (int i = 0; i < KayitliParcalar.Count; i++)
        {
            MotorParcalari diger = KayitliParcalar[i];
            if (diger == null || diger == this) continue;
            foreach (var ca in _benimColliderlarim)
            {
                if (ca == null || !ca.enabled || !ca.gameObject.activeInHierarchy) continue;
                foreach (var cb in diger._benimColliderlarim)
                {
                    if (cb == null || ca == cb || !cb.enabled || !cb.gameObject.activeInHierarchy) continue;
                    Physics.IgnoreCollision(ca, cb, true);
                }
            }
        }
        _carpismaKapatildi = true;
    }
    private void DigerParcalarlaCarpismayiAc()
    {
        if (!_carpismaKapatildi) return;
        for (int i = 0; i < KayitliParcalar.Count; i++)
        {
            MotorParcalari diger = KayitliParcalar[i];
            if (diger == null || diger == this) continue;
            foreach (var ca in _benimColliderlarim)
            {
                if (ca == null || !ca.enabled || !ca.gameObject.activeInHierarchy) continue;
                foreach (var cb in diger._benimColliderlarim)
                {
                    if (cb == null || ca == cb || !cb.enabled || !cb.gameObject.activeInHierarchy) continue;
                    Physics.IgnoreCollision(ca, cb, false);
                }
            }
        }
        _carpismaKapatildi = false;
    }
}