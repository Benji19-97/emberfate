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
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if !UNITY_SERVER
            NetworkClient.RegisterHandler<ServerNotification>(OnServerNotification);
#endif
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
            PushNotification(notification.Message, notification.IsFading);
        }

        public void PushNotification(string message, bool isFading)
        {
            notificationText.text = message;
            notificationText.color = Color.white;
            _timer = 0f;
            _currentNotificationIsFading = isFading;
        }

        private void Update()
        {
            if (_currentNotificationIsFading)
            {
                _timer += Time.deltaTime;
                if (_timer > secondsBeforeFading)
                {
                    var color = notificationText.color;
                    color.a -= 1f / fadeTime * Time.deltaTime;
                    color.a = Mathf.Clamp(color.a, 0, 1);
                    notificationText.color = color;
                }
            }
        }
    }
}