using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Serangan jarak dekat Player dengan klik kiri mouse.
/// Semua <see cref="Health"/> (selain milik Player sendiri) di dalam bola di depan Player kena damage.
/// Ada cooldown supaya klik beruntun tidak langsung menghabisi Enemy.
/// </summary>
public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 20f;
    [SerializeField, Min(0.05f)] private float cooldown = 0.5f;
    [Tooltip("Jarak pusat area pukulan di depan Player.")]
    [SerializeField, Min(0f)] private float reach = 1.2f;
    [SerializeField, Min(0.1f)] private float radius = 1.2f;
    [Tooltip("Model (child) yang digeser maju sebentar saat memukul.")]
    [SerializeField] private Transform model;

    private Health self;
    private float nextAttackTime;
    private Vector3 modelRestPosition;
    private Coroutine lungeRoutine;
    private readonly Collider[] hits = new Collider[16];

    private Vector3 HitCenter => transform.position + Vector3.up * 1f + transform.forward * reach;

    private void Awake()
    {
        self = GetComponent<Health>();
        if (model != null)
            modelRestPosition = model.localPosition;
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;
        if ((self != null && self.IsDead) || Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + cooldown;
        Swing();
    }

    private void Swing()
    {
        if (model != null)
        {
            if (lungeRoutine != null)
                StopCoroutine(lungeRoutine);
            lungeRoutine = StartCoroutine(Lunge());
        }

        int count = Physics.OverlapSphereNonAlloc(HitCenter, radius, hits, ~0, QueryTriggerInteraction.Ignore);
        bool hitSomething = false;
        for (int i = 0; i < count; i++)
        {
            Health target = hits[i].GetComponentInParent<Health>();
            if (target == null || target == self || target.IsDead)
                continue;
            target.TakeDamage(damage);
            hitSomething = true;
            Debug.Log($"[Player] Pukul {target.name}: -{damage:0} HP (sisa {target.Current:0}/{target.Max:0})", target);
        }
        if (!hitSomething)
            Debug.Log("[Player] Pukulan meleset (Enemy di luar jangkauan).");
    }

    private IEnumerator Lunge()
    {
        const float time = 0.2f;
        for (float t = 0f; t < time; t += Time.deltaTime)
        {
            model.localPosition = modelRestPosition + Vector3.forward * (0.35f * Mathf.Sin(t / time * Mathf.PI));
            yield return null;
        }
        model.localPosition = modelRestPosition;
        lungeRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.6f);
        Gizmos.DrawWireSphere(HitCenter, radius);
    }
}
