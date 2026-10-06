using System;
using UnityEngine;

/// <summary>
/// HP sederhana yang dipakai Player dan Enemy.
/// Script lain cukup membaca <see cref="Current"/> / <see cref="Normalized"/> atau mendengarkan event-nya,
/// jadi FSM tidak perlu tahu siapa yang memberi damage.
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float current = 100f;

    public event Action<float> Damaged;
    public event Action<float> Healed;
    public event Action Died;

    public float Max => maxHealth;
    public float Current => current;
    public float Normalized => current / maxHealth;
    public bool IsDead => current <= 0f;

    private void Awake()
    {
        current = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        current = Mathf.Max(0f, current - amount);
        Damaged?.Invoke(amount);
        if (IsDead)
            Died?.Invoke();
    }

    /// <summary>Mengisi HP. Yang sudah mati tidak bisa di-heal (Dead adalah state akhir).</summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
            return;

        float before = current;
        current = Mathf.Min(maxHealth, current + amount);
        if (current > before)
            Healed?.Invoke(current - before);
    }

    /// <summary>Isi HP sampai minimal fraction * Max (0..1).</summary>
    public void HealTo(float fraction)
    {
        Heal(Mathf.Clamp01(fraction) * maxHealth - current);
    }

    public void Kill()
    {
        TakeDamage(current);
    }
}
