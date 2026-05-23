using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class IslandGameManager : MonoBehaviour
{
    public static IslandGameManager Instance { get; private set; }

    private int totalCrystals;
    private int collectedCrystals;
    private bool won;
    private bool lost;
    private float startTime;
    private float finalTime;
    private AudioSource audioSource;
    private AudioClip crystalClip;
    private AudioClip hitClip;
    private AudioClip winClip;
    private Texture2D whiteTexture;
    private GUIStyle labelStyle;
    private GUIStyle messageStyle;
    private GUIStyle buttonStyle;

    public bool HasCollectedAllCrystals => totalCrystals > 0 && collectedCrystals >= totalCrystals;
    public bool IsRunning => !won && !lost;

    private void Awake()
    {
        Instance = this;
        startTime = Time.time;
        audioSource = gameObject.AddComponent<AudioSource>();
        crystalClip = CreateTone("Crystal", 880f, 0.12f, 0.22f);
        hitClip = CreateTone("Hit", 140f, 0.16f, 0.28f);
        winClip = CreateTone("Win", 660f, 0.45f, 0.25f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RegisterCrystal()
    {
        totalCrystals++;
    }

    public void CollectCrystal()
    {
        collectedCrystals = Mathf.Min(collectedCrystals + 1, totalCrystals);
        PlayClip(crystalClip);
    }

    public void Win()
    {
        if (!IsRunning)
        {
            return;
        }

        won = true;
        finalTime = Time.time - startTime;
        SaveBestTime(finalTime);
        PlayClip(winClip);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Lose()
    {
        if (!IsRunning)
        {
            return;
        }

        lost = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void PlayHitSound()
    {
        PlayClip(hitClip);
    }

    private void OnGUI()
    {
        EnsureStyles();

        float elapsed = Time.time - startTime;
        int health = 0;
        int maxHealth = 100;
        var playerHealth = FindObjectOfType<IslandPlayerHealth>();
        if (playerHealth != null)
        {
            health = playerHealth.CurrentHealth;
            maxHealth = playerHealth.MaxHealth;
        }

        GUI.Label(new Rect(20f, 18f, 620f, 40f), $"Health: {health}/{maxHealth}    Crystals: {collectedCrystals}/{totalCrystals}    Time: {elapsed:0.0}s", labelStyle);
        DrawHealthBar(20f, 54f, 260f, 18f, health / (float)Mathf.Max(1, maxHealth));
        DrawMinimap();

        if (won)
        {
            DrawEndMenu($"You escaped the island!\nFinal time: {finalTime:0.0}s\nBest time: {GetBestTimeText()}");
        }
        else if (lost)
        {
            DrawEndMenu("You were caught by island sentries.\nTry again?");
        }
        else if (HasCollectedAllCrystals)
        {
            GUI.Label(new Rect(0f, 66f, Screen.width, 50f), "All crystals collected. Run to the golden beacon!", messageStyle);
        }
        else
        {
            GUI.Label(new Rect(0f, 66f, Screen.width, 50f), "Collect all crystals, then reach the golden beacon.", messageStyle);
        }
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
        {
            return;
        }

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            normal = { textColor = Color.white }
        };

        messageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 24,
            normal = { textColor = Color.white }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 22
        };

        whiteTexture = Texture2D.whiteTexture;
    }

    private void DrawHealthBar(float x, float y, float width, float height, float healthPercent)
    {
        DrawRect(new Rect(x, y, width, height), new Color(0f, 0f, 0f, 0.55f));
        DrawRect(new Rect(x + 2f, y + 2f, (width - 4f) * Mathf.Clamp01(healthPercent), height - 4f), Color.Lerp(Color.red, Color.green, healthPercent));
    }

    private void DrawMinimap()
    {
        const float mapSize = 170f;
        Rect mapRect = new Rect(Screen.width - mapSize - 20f, 20f, mapSize, mapSize);
        DrawRect(mapRect, new Color(0f, 0f, 0f, 0.45f));
        DrawRect(new Rect(mapRect.x + 4f, mapRect.y + 4f, mapSize - 8f, mapSize - 8f), new Color(0.11f, 0.38f, 0.22f, 0.5f));

        IslandMinimapIcon[] icons = FindObjectsOfType<IslandMinimapIcon>();
        foreach (IslandMinimapIcon icon in icons)
        {
            Vector3 position = icon.transform.position;
            float px = Mathf.InverseLerp(-120f, 120f, position.x);
            float py = Mathf.InverseLerp(-120f, 120f, position.z);
            Color color = icon.IconType switch
            {
                IslandMinimapIconType.Player => Color.white,
                IslandMinimapIconType.Crystal => Color.cyan,
                IslandMinimapIconType.Enemy => Color.red,
                IslandMinimapIconType.Beacon => Color.yellow,
                IslandMinimapIconType.Palm => new Color(0.13f, 0.55f, 0.13f), // vihreä puulle
                IslandMinimapIconType.Heal => new Color(1f, 0.4f, 0.8f), // pinkki healille
                _ => Color.gray
            };

            DrawRect(new Rect(mapRect.x + px * mapSize - 4f, mapRect.y + (1f - py) * mapSize - 4f, 8f, 8f), color);
        }
    }

    private void DrawEndMenu(string text)
    {
        Rect panel = new Rect(Screen.width * 0.5f - 230f, Screen.height * 0.5f - 125f, 460f, 250f);
        DrawRect(panel, new Color(0f, 0f, 0f, 0.72f));
        GUI.Label(new Rect(panel.x + 20f, panel.y + 24f, panel.width - 40f, 110f), text, messageStyle);

        if (GUI.Button(new Rect(panel.x + 55f, panel.y + 160f, 150f, 48f), "Restart", buttonStyle))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (GUI.Button(new Rect(panel.x + 255f, panel.y + 160f, 150f, 48f), "Quit", buttonStyle))
        {
            Application.Quit();
        }
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = previousColor;
    }

    private void SaveBestTime(float time)
    {
        float best = PlayerPrefs.GetFloat("IslandBestTime", 0f);
        if (best <= 0f || time < best)
        {
            PlayerPrefs.SetFloat("IslandBestTime", time);
            PlayerPrefs.Save();
        }
    }

    private string GetBestTimeText()
    {
        float best = PlayerPrefs.GetFloat("IslandBestTime", 0f);
        return best <= 0f ? "--" : $"{best:0.0}s";
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private AudioClip CreateTone(string clipName, float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float fade = 1f - (i / (float)sampleCount);
            samples[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * volume * fade;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
