using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tampilan Enemy, terpisah dari logika FSM:
/// - Parameter "Speed" Animator diisi dari kecepatan NavMeshAgent (Idle / Run).
/// - Indikator di atas kepala berwarna sesuai state.
/// - Attack: model maju sebentar (lunge). Kena damage: indikator berkedip putih.
/// - Dead: Animator berhenti dan model rebah ke belakang.
/// Aset Skeleton tidak punya clip attack/death, jadi efek tersebut dibuat lewat kode.
/// </summary>
public class EnemyVisualFeedback : MonoBehaviour
{
    [SerializeField] private EnemyFSM fsm;
    [SerializeField] private Health health;
    [SerializeField] private NavMeshAgent agent;
    [Tooltip("Animator pada model karakter (child).")]
    [SerializeField] private Animator animator;
    [Tooltip("Transform model (child) yang digeser saat lunge dan diputar saat mati.")]
    [SerializeField] private Transform model;
    [Tooltip("Renderer indikator state di atas kepala.")]
    [SerializeField] private Renderer stateIndicator;
    [SerializeField] private string speedParameter = "Speed";

    [SerializeField] private float lungeDistance = 0.5f;
    [SerializeField] private float lungeTime = 0.25f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MaterialPropertyBlock block;
    private int speedHash;
    private bool hasSpeedParameter;
    private Vector3 modelRestPosition;
    private float flashTimer;
    private Coroutine lungeRoutine;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        speedHash = Animator.StringToHash(speedParameter);
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter p in animator.parameters)
                hasSpeedParameter |= p.nameHash == speedHash;
        }
        if (model != null)
            modelRestPosition = model.localPosition;
    }

    private void OnEnable()
    {
        fsm.StateChanged += OnStateChanged;
        fsm.Attacked += OnAttacked;
        health.Damaged += OnDamaged;
    }

    private void OnDisable()
    {
        fsm.StateChanged -= OnStateChanged;
        fsm.Attacked -= OnAttacked;
        health.Damaged -= OnDamaged;
    }

    private void Update()
    {
        if (hasSpeedParameter && animator.enabled)
        {
            float speed = agent != null && agent.enabled ? agent.velocity.magnitude : 0f;
            animator.SetFloat(speedHash, speed, 0.1f, Time.deltaTime);
        }

        flashTimer -= Time.deltaTime;
        Color c = flashTimer > 0f ? Color.white : EnemyFSM.StateColor(fsm.CurrentState);
        SetIndicatorColor(c);

        if (stateIndicator != null && fsm.CurrentState != EnemyFSMState.Dead)
            stateIndicator.transform.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
    }

    private void SetIndicatorColor(Color c)
    {
        if (stateIndicator == null)
            return;
        stateIndicator.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        stateIndicator.SetPropertyBlock(block);
    }

    private void OnDamaged(float amount)
    {
        flashTimer = 0.15f;
    }

    private void OnAttacked()
    {
        if (model == null)
            return;
        if (lungeRoutine != null)
            StopCoroutine(lungeRoutine);
        lungeRoutine = StartCoroutine(Lunge());
    }

    private IEnumerator Lunge()
    {
        for (float t = 0f; t < lungeTime; t += Time.deltaTime)
        {
            // 0 -> 1 -> 0 dalam satu kali lunge.
            float k = Mathf.Sin(t / lungeTime * Mathf.PI);
            model.localPosition = modelRestPosition + Vector3.forward * (lungeDistance * k);
            yield return null;
        }
        model.localPosition = modelRestPosition;
        lungeRoutine = null;
    }

    private void OnStateChanged(EnemyFSMState from, EnemyFSMState to)
    {
        if (to == EnemyFSMState.Dead)
            StartCoroutine(FallDown());
    }

    private IEnumerator FallDown()
    {
        if (lungeRoutine != null)
            StopCoroutine(lungeRoutine);
        if (animator != null)
            animator.enabled = false;
        if (model == null)
            yield break;

        model.localPosition = modelRestPosition;
        Quaternion start = model.localRotation;
        Quaternion end = Quaternion.Euler(-90f, 0f, 0f) * start;
        const float duration = 0.6f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            model.localRotation = Quaternion.Slerp(start, end, k * k);
            yield return null;
        }
        model.localRotation = end;
    }
}
