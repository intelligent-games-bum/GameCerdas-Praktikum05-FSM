using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// (Bonus UI Current State) Panel debug Praktikum 5 + tombol uji coba:
///   Klik kiri = Player menyerang (lihat <see cref="PlayerMeleeAttack"/>),
///   L = bunuh Enemy, H = pulihkan HP Player, R = restart scene.
/// Juga menampilkan health bar di atas kepala Player dan Enemy (+ label state Enemy),
/// dan mematikan kontrol Player saat HP Player habis.
/// Ukuran UI diskalakan terhadap tinggi layar 720 px supaya tetap terbaca di resolusi rekaman 1080p.
/// </summary>
public class FSMDebugHUD : MonoBehaviour
{
    [SerializeField] private EnemyFSM enemy;
    [SerializeField] private Health enemyHealth;
    [SerializeField] private Health playerHealth;
    [Tooltip("Script Player yang dimatikan saat Player kalah (gerak, serangan).")]
    [SerializeField] private MonoBehaviour[] disableOnPlayerDeath;
    [Tooltip("Tinggi health bar di atas pivot (kaki) karakter.")]
    [SerializeField] private float barHeight = 2.7f;

    private static readonly Color EnemyBarColor = new Color(0.9f, 0.25f, 0.25f);
    private static readonly Color PlayerBarColor = new Color(0.25f, 0.6f, 1f);

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;
    private GUIStyle bigStyle;
    private GUIStyle worldStyle;
    private GUIStyle barTextStyle;
    private Texture2D pixel;
    private float uiScale = 1f;

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.Died += OnPlayerDied;
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.Died -= OnPlayerDied;
    }

    private void OnPlayerDied()
    {
        if (disableOnPlayerDeath != null)
        {
            foreach (MonoBehaviour mb in disableOnPlayerDeath)
                if (mb != null)
                    mb.enabled = false;
        }
        Debug.Log("[Praktikum 5] Player kalah. Tekan R untuk restart.");
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.lKey.wasPressedThisFrame && enemyHealth != null)
            enemyHealth.Kill();
        if (kb.hKey.wasPressedThisFrame && playerHealth != null)
            playerHealth.HealTo(1f);
        if (kb.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void InitStyles()
    {
        if (boxStyle != null)
            return;
        pixel = MakeTex(Color.white);

        boxStyle = new GUIStyle(GUI.skin.box) { normal = { background = MakeTex(new Color(0f, 0f, 0f, 0.6f)) } };
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
        labelStyle.normal.textColor = Color.white;
        bigStyle = new GUIStyle(labelStyle) { fontSize = 22, fontStyle = FontStyle.Bold };
        worldStyle = new GUIStyle(labelStyle) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        barTextStyle = new GUIStyle(labelStyle) { fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }

    private static Texture2D MakeTex(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    private void OnGUI()
    {
        if (enemy == null)
            return;
        InitStyles();

        uiScale = Mathf.Max(0.5f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

        EnemyFSMState state = enemy.CurrentState;
        string hex = ColorUtility.ToHtmlStringRGB(EnemyFSM.StateColor(state));

        GUILayout.BeginArea(new Rect(12, 12, 330, 250), boxStyle);
        GUILayout.Label("<b>PRAKTIKUM 5 - ENEMY FSM</b>", labelStyle);
        GUILayout.Label($"Current State: <color=#{hex}>{state.ToString().ToUpper()}</color>", bigStyle);
        if (enemyHealth != null)
            PanelBar("HP Enemy", enemyHealth, EnemyBarColor);
        if (playerHealth != null)
            PanelBar("HP Player", playerHealth, PlayerBarColor);
        if (state == EnemyFSMState.Attack)
            GUILayout.Label($"Attack cooldown: {enemy.AttackCooldownLeft:0.0} / {enemy.AttackCooldown:0.0} s", labelStyle);
        if (state == EnemyFSMState.Flee && enemy.FleeTarget != null)
            GUILayout.Label($"Kabur ke: {enemy.FleeTarget.name}", labelStyle);
        GUILayout.Space(4);
        GUILayout.Label("<size=12>WASD gerak | Klik kiri serang | L bunuh Enemy\nH pulihkan Player | R restart</size>", labelStyle);
        GUILayout.EndArea();

        if (playerHealth != null && playerHealth.IsDead)
        {
            var center = new GUIStyle(bigStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 32 };
            float w = Screen.width / uiScale;
            GUI.Label(new Rect(0, 720f * 0.4f, w, 60), "<color=#ff5050>PLAYER KALAH</color> - tekan R", center);
        }

        if (enemyHealth != null)
            WorldBar(enemy.transform, enemyHealth, EnemyBarColor, $"<color=#{hex}>{state.ToString().ToUpper()}</color>");
        if (playerHealth != null)
            WorldBar(playerHealth.transform, playerHealth, PlayerBarColor, "PLAYER");

        GUI.matrix = Matrix4x4.identity;
    }

    private void PanelBar(string label, Health h, Color color)
    {
        GUILayout.Label($"{label}: {h.Current:0} / {h.Max:0}", labelStyle);
        Rect r = GUILayoutUtility.GetRect(300, 10);
        GUI.color = new Color(1f, 1f, 1f, 0.25f);
        GUI.DrawTexture(r, pixel);
        GUI.color = color;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * h.Normalized, r.height), pixel);
        GUI.color = Color.white;
    }

    /// <summary>Label + health bar yang mengikuti kepala karakter di layar.</summary>
    private void WorldBar(Transform who, Health h, Color color, string title)
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;
        Vector3 sp = cam.WorldToScreenPoint(who.position + Vector3.up * barHeight);
        if (sp.z <= 0f)
            return;

        // Koordinat layar -> koordinat GUI yang sudah diskalakan.
        float x = sp.x / uiScale;
        float y = (Screen.height - sp.y) / uiScale;

        const float width = 110f;
        const float height = 14f;
        GUI.Label(new Rect(x - 80, y - 24, 160, 20), title, worldStyle);

        var outer = new Rect(x - width * 0.5f - 2, y - 2, width + 4, height + 4);
        var inner = new Rect(x - width * 0.5f, y, width, height);
        GUI.color = Color.black;
        GUI.DrawTexture(outer, pixel);
        GUI.color = new Color(0.25f, 0.25f, 0.25f);
        GUI.DrawTexture(inner, pixel);
        GUI.color = color;
        GUI.DrawTexture(new Rect(inner.x, inner.y, inner.width * h.Normalized, inner.height), pixel);
        GUI.color = Color.white;
        GUI.Label(inner, $"{h.Current:0} / {h.Max:0}", barTextStyle);
    }
}
