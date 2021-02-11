using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if !UNITY_SERVER
using Steamworks;
#endif
using UnityEngine;
using UnityEngine.Networking;

public class SteamTokenAuthenticator : NetworkAuthenticator
{
    public static string AuthTicket;

    private string _webAPIKey;
    private string _appID;

    private const int OK_RESPONSE_CODE = 200;

    private struct AuthRequestMessage : NetworkMessage
    {
        public string Ticket;
        public string SteamId;
    }

    private struct AuthResponseMessage : NetworkMessage
    {
        public string FailReason;
    }

    [Serializable]
    public class SteamAuthResponse
    {
        public string result;
        public string steamid;
        public string ownersteamid;
        public bool vacbanned;
        public bool publisherbanned;
    }

    #region Server

    public override void OnStartServer()
    {
        NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequestMessage, false);
    }

    public override void OnServerAuthenticate(NetworkConnection conn)
    {
    }

    private void OnAuthRequestMessage(NetworkConnection conn, AuthRequestMessage msg)
    {
        StartCoroutine(__ValidateToken(conn, msg.Ticket, msg.SteamId));
    }

    private IEnumerator __ValidateToken(NetworkConnection conn, string ticket, string steamid)
    {
        Debug.Log("Server is asked to validate token: " + steamid);

        if (string.IsNullOrEmpty(_webAPIKey))
        {
            string path = "Assets/Server/web_api_key.txt";
            StreamReader reader = new StreamReader(path);
            _webAPIKey = reader.ReadToEnd();
            reader.Close();
        }

        if (string.IsNullOrEmpty(_appID))
        {
            _appID = "1552150";
        }

        Debug.Log("appid: " + _appID);
        Debug.Log("key: " + _webAPIKey);

        string parameters = $"?key={_webAPIKey}&ticket={ticket}&appid={_appID}";

        using (UnityWebRequest webRequest = UnityWebRequest.Get("https://partner.steam-api.com/ISteamUserAuth/AuthenticateUserTicket/v1/" + parameters))
        {
            Debug.Log("sent web request");
            yield return webRequest.SendWebRequest();


             
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.Log("error occured: " + webRequest.error);
                ValidateTokenFailed(conn, "Network or HTTP error occurred.");
            }
            else
            {
                if (webRequest.responseCode != OK_RESPONSE_CODE)
                {
                    ValidateTokenFailed(conn, "Unhandled response code of " + webRequest.responseCode);
                }
                else
                {
                    if (conn == null)
                    {
                        Debug.Log("conn is null");
                    }
                    
                    if (webRequest.downloadHandler == null)
                    {
                        Debug.Log("downloadhandler is null");
                    }
                    
                    HandleValidateToken(conn, webRequest.downloadHandler.text, ticket, steamid);
                }
            }
        }
    }


    private void HandleValidateToken(NetworkConnection conn, string text, string ticket, string steamid)
    {
        if (conn == null)
        {
            Debug.Log("conn is null");
        }

        if (text == null)
        {
            Debug.Log("text is null");
        }

        if (ticket == null)
        {
            Debug.Log("ticket is null");
        }

        if (steamid == null)
        {
            Debug.Log("steamid is null");
        }

        Debug.Log("Web Request Download Handler: " + text);
        
        var jObject = JObject.Parse(text);

        var jToken = jObject["response"]["params"];

        if (jToken != null)
        {
            var authResponse = jToken.ToObject<SteamAuthResponse>();

            if (authResponse != null && (!authResponse.vacbanned || !authResponse.publisherbanned || authResponse.result != "OK" ||
                                         authResponse.steamid != steamid))
            {
                ValidateTokenSuccess(conn, ticket, steamid);
                return;
            }
        }

        ValidateTokenFailed(conn, "Authentication failed: " + text);
    }

    private void ValidateTokenSuccess(NetworkConnection conn, string ticket, string steamid)
    {
        Debug.Log("ValidateTokenSuccess. Id of " + steamid);
        AuthResponseMessage authResponseMessage = new AuthResponseMessage();
        conn.Send(authResponseMessage);
        OnServerAuthenticated.Invoke(conn);
    }

    private void ValidateTokenFailed(NetworkConnection conn, string reason)
    {
        AuthResponseMessage authResponseMessage =
            new AuthResponseMessage()
            {
                FailReason = reason
            };
        conn.Send(authResponseMessage);
    }

    #endregion


    #region Client

    public override void OnStartClient()
    {
        NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponseMessage, false);
    }

    public override void OnClientAuthenticate(NetworkConnection conn)
    {
#if !UNITY_SERVER
        AuthRequestMessage authRequestMessage = new AuthRequestMessage()
        {
            Ticket = AuthTicket,
            SteamId = SteamUser.GetSteamID().m_SteamID.ToString()
        };

        NetworkClient.Send(authRequestMessage);
#endif
    }

    private void OnAuthResponseMessage(NetworkConnection conn, AuthResponseMessage msg)
    {
        if (string.IsNullOrEmpty(msg.FailReason))
        {
            Debug.Log("Authenticator success. Player may connect.");
            OnClientAuthenticated.Invoke(conn);
        }
        else
        {
            Debug.LogError("Authenticator failed " + msg.FailReason);
            NetworkClient.Disconnect();
        }
    }

    #endregion


    public void DeserializeJSONObject()
    {
    }
}