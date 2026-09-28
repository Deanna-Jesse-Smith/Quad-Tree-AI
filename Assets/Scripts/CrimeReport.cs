using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CrimeReport : MonoBehaviour
{
    [Header("Entity Count Labels")]
    [SerializeField] private TextMeshProUGUI txtHeroCount;
    [SerializeField] private TextMeshProUGUI txtVillainCount;
    [SerializeField] private TextMeshProUGUI txtCivilianCount;
    [SerializeField] private TextMeshProUGUI txtVigilanteCount;

    [Header("Morale")]
    [SerializeField] private UnityEngine.UI.Slider moraleSlider;
    [SerializeField] private TextMeshProUGUI moraleLabel;

    [Header("Event Log")]
    [SerializeField] private TextMeshProUGUI eventLogLabel;
    [SerializeField] private int maxLogLines = 1;

    [Header("Refresh Rate")]
    [SerializeField] private float refreshInterval = 0.5f;

    private float refreshTimer;
    private Queue<string> eventLog = new Queue<string>();

    public static CrimeReport instance { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RefreshCounts();
            RefreshMorale();
        }
    }

    void Awake()
    {
        if (instance != null && instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        instance = this;
    }

    private void RefreshCounts()
    {
        int villains = FindObjectsByType<Villain>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        int civilians = FindObjectsByType<Civilian>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
        int vigilantes = FindObjectsByType<Vigilante>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;

        if (txtHeroCount) 
            txtHeroCount.text = $"Heroes:     {HeroTracker.instance?.heroCount ?? 0}";
        if (txtVillainCount) 
            txtVillainCount.text = $"Villains:   {villains}";
        if (txtCivilianCount) 
            txtCivilianCount.text = $"Civilians:  {civilians}";
        if (txtVigilanteCount) 
            txtVigilanteCount.text = $"Vigilantes: {vigilantes}";
    }

    private void RefreshMorale()
    {
        if (MoraleManager.Instance == null) return;
        float morale = MoraleManager.Instance.morale;

        if (moraleSlider)
        {
            moraleSlider.minValue = 0f;
            moraleSlider.maxValue = 100f;
            moraleSlider.value = morale;

            var fill = moraleSlider.fillRect?.GetComponent<UnityEngine.UI.Image>();
            if (fill != null)
                fill.color = Color.Lerp(Color.red, Color.green, morale / 100f);
        }

        if (moraleLabel)
            moraleLabel.text = $"Morale: {morale:F0}";
    }

    public void LogEvent(string message)
    {
        string timestamped = $"—{message}";
        eventLog.Enqueue(timestamped);
        while (eventLog.Count > maxLogLines)
            eventLog.Dequeue();

        if (eventLogLabel)
            eventLogLabel.text = string.Join("\n", eventLog);
    }
}
