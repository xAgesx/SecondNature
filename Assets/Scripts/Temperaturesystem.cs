using UnityEngine;
using UnityEngine.Events;

public class TemperatureSystem : MonoBehaviour
{
    public static TemperatureSystem Instance { get; private set; }

    [Header("Temperature Settings")]
    public float startingTemp = 34f;
    public float maxTemp = 45f;

    [Header("VFX — Damage Flash")]
    public GameObject damageVFX;

    public UnityEvent<float> onTemperatureChanged;
    public UnityEvent onGameOver;

    public float CurrentTemp { get; private set; }
    private bool _isDead = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentTemp = startingTemp;
    }

    private void Start()
    {
        Debug.Log($"[TemperatureSystem] Started at {CurrentTemp:F1}°C. Game over at {maxTemp:F1}°C.");
    }

    public void AddHeat(float amount, string source = "Unknown")
    {
        if (_isDead) return;
        CurrentTemp += amount;
        CurrentTemp = Mathf.Clamp(CurrentTemp, startingTemp, maxTemp);
        PlayDamageVFX();
        onTemperatureChanged?.Invoke(CurrentTemp);
        Debug.Log($"[TemperatureSystem] +{amount:F1}°C from '{source}'. Current: {CurrentTemp:F1}°C / {maxTemp:F1}°C");
        if (CurrentTemp >= maxTemp) TriggerGameOver();
    }

    private void PlayDamageVFX()
    {
        if (damageVFX == null) return;
        var ps = damageVFX.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
        else damageVFX.SetActive(true);
    }

    private void TriggerGameOver()
    {
        _isDead = true;
        Debug.LogError($"[TemperatureSystem] GAME OVER — reached {maxTemp:F1}°C!");
        onGameOver?.Invoke();
    }
}