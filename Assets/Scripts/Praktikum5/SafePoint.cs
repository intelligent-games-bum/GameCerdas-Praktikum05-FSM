using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Area aman tujuan state Flee. (Bonus) Enemy yang tiba di sini dipulihkan sampai healToFraction * MaxHP,
/// sehingga setelah kembali Patrol ia tidak langsung masuk Flee lagi.
/// </summary>
public class SafePoint : MonoBehaviour
{
    [Tooltip("Enemy dianggap sudah sampai jika jaraknya <= radius ini.")]
    [SerializeField, Min(0.1f)] private float radius = 1.5f;
    [Tooltip("HP dipulihkan sampai fraksi ini dari Max HP (0 = tanpa heal).")]
    [SerializeField, Range(0f, 1f)] private float healToFraction = 0.7f;

    public float Radius => radius;

    public bool Contains(Vector3 position)
    {
        Vector3 d = position - transform.position;
        d.y = 0f;
        return d.magnitude <= radius;
    }

    public void Rest(Health health)
    {
        if (health != null && healToFraction > 0f)
            health.HealTo(healToFraction);
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        Handles.color = new Color(0.2f, 1f, 0.45f, 0.9f);
        Handles.DrawWireDisc(transform.position + Vector3.up * 0.05f, Vector3.up, radius, 3f);
        Handles.Label(transform.position + Vector3.up * 2f, name);
#endif
    }
}
