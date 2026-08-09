using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace MathBoxing.Backend
{
    [System.Serializable]
    public class SupabaseConfigData
    {
        public string supabaseURL;
        public string supabaseApiKey;
    }

    public class ConfigManager : MonoBehaviour
    {
        public static ConfigManager Instance { get; private set; }
        public SupabaseConfigData Config { get; private set; }
        public bool IsLoaded { get; private set; } = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                transform.SetParent(null); // Lepas dari parent agar jadi Root GameObject
                DontDestroyOnLoad(gameObject);
                StartCoroutine(LoadConfigCoroutine());
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IEnumerator LoadConfigCoroutine()
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "config.json");
            string jsonText = "";

            // Di Android, StreamingAssets berada di dalam package APK (menggunakan format jar:file://)
            if (filePath.Contains("://") || filePath.Contains(":///"))
            {
                using (UnityWebRequest www = UnityWebRequest.Get(filePath))
                {
                    yield return www.SendWebRequest();
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        jsonText = www.downloadHandler.text;
                    }
                    else
                    {
                        Debug.LogError($"[ConfigManager] Gagal membaca config.json di Android: {www.error}");
                    }
                }
            }
            else
            {
                // Untuk Windows Editor / Standalone Desktop
                if (File.Exists(filePath))
                {
                    jsonText = File.ReadAllText(filePath);
                }
                else
                {
                    Debug.LogError($"[ConfigManager] File tidak ditemukan di rute: {filePath}");
                }
            }

            if (!string.IsNullOrEmpty(jsonText))
            {
                Config = JsonUtility.FromJson<SupabaseConfigData>(jsonText);
                IsLoaded = true;
                Debug.Log("<color=green>[ConfigManager] config.json berhasil dimuat secara steril!</color>");
            }
        }
    }
}