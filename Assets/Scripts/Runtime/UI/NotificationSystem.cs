using System;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class NotificationSystem : MonoBehaviour
    {
        public static NotificationSystem Instance;

        [SerializeField] private Text notificationText;
        [SerializeField] private float secondsBeforeFading;
        [SerializeField] private float fadeTime;

        private bool _currentNotificationIsFading;
        private float _timer;

        #region Unity Event functions

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject); TODO: make this dont destroy somehow
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (GameServer.START_SERVER_IN_UNITY_EDITOR)
            {
                return;
            }
#elif UNITY_SERVER
            return;
#endif
            NetworkClient.RegisterHandler<ServerNotification>(OnServerNotification);
        }

        #endregion

        public struct ServerNotification : NetworkMessage
        {
            public string Message;
            public bool IsFading;
        }

        [ClientCallback]
        private void OnServerNotification(NetworkConnection conn, ServerNotification notification)
        {
            Push(notification.Message, notification.IsFading);
        }

        public static void Push(string message, bool isFading)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.notificationText.text = message;
            Instance.notificationText.color = Color.white;
            Instance._timer = 0f;
            Instance._currentNotificationIsFading = isFading;
        }

        private void Update()
        {
            if (!_currentNotificationIsFading) return;
            
            _timer += Time.deltaTime;

            if (!(_timer > secondsBeforeFading)) return;

            ReduceTextOpacity();
        }

        private void ReduceTextOpacity()
        {
            var color = notificationText.color;
            color.a -= 1f / fadeTime * Time.deltaTime;
            color.a = Mathf.Clamp(color.a, 0, 1);
            notificationText.color = color;
        }
    }
}