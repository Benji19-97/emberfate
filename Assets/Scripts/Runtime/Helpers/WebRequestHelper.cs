using System.Text;
using UnityEngine.Networking;

namespace Runtime.Helpers
{
    public static class WebRequestHelper
    {
        public static UnityWebRequest GetPostRequest(string url, string attachedJson)
        {
            var webRequest = new UnityWebRequest(url, "POST");

            var jsonToSend = new UTF8Encoding().GetBytes(attachedJson);
            webRequest.uploadHandler = new UploadHandlerRaw(jsonToSend);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");

            return webRequest;
        }
    }
}