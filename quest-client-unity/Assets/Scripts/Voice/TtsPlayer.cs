using UnityEngine;

namespace QuestSpatialAssistant.Voice
{
    /// <summary>
    /// 用 Android 原生 TextToSpeech 播报句子（V0 的 tts_mode = "local"）。
    /// 在 Quest（Android）真机上生效；在 Editor 里只打日志，方便调试。
    ///
    /// 实现：通过 AndroidJavaObject 反射调用 android.speech.tts.TextToSpeech。
    /// 首次初始化是异步的，初始化完成前的 Speak 会被忽略（会有日志）。
    /// </summary>
    public class TtsPlayer : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _tts;
        private bool _ready;

        private void Start()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // OnInitListener 回调：status == 0 (SUCCESS) 时标记就绪
            var listener = new TtsInitListener(this);
            _tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, listener);
        }

        internal void MarkReady()
        {
            _ready = true;
            // 默认英文；需要时可改 Locale
            using var locale = new AndroidJavaObject("java.util.Locale", "en", "US");
            _tts.Call<int>("setLanguage", locale);
            Debug.Log("[TtsPlayer] ready");
        }

        public void Speak(string text)
        {
            if (!_ready || _tts == null)
            {
                Debug.LogWarning("[TtsPlayer] not ready, skip: " + text);
                return;
            }
            // QUEUE_FLUSH = 0：打断上一句
            _tts.Call<int>("speak", text, 0, null, "qsa_utterance");
        }

        private void OnDestroy()
        {
            if (_tts != null)
            {
                _tts.Call("stop");
                _tts.Call("shutdown");
            }
        }

        /// <summary>实现 android TextToSpeech.OnInitListener。</summary>
        private class TtsInitListener : AndroidJavaProxy
        {
            private readonly TtsPlayer _owner;
            public TtsInitListener(TtsPlayer owner)
                : base("android.speech.tts.TextToSpeech$OnInitListener") => _owner = owner;

            // void onInit(int status)
            public void onInit(int status)
            {
                if (status == 0) _owner.MarkReady();
                else Debug.LogWarning("[TtsPlayer] init failed, status=" + status);
            }
        }
#else
        public void Speak(string text)
        {
            // Editor / 非 Android：只打印，方便联调
            Debug.Log("[TtsPlayer:editor] would speak -> " + text);
        }
#endif
    }
}
