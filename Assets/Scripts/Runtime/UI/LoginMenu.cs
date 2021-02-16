using System;
using System.Collections.Generic;
using Mirror;
using Runtime.Core.Server;
using Runtime.Services;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_SERVER
using Runtime.Core;
using Steamworks;

#endif

namespace Runtime.UI
{
    public class LoginMenu : MonoBehaviour
    {
        [SerializeField] private Dropdown serverDropdown;

#if !UNITY_SERVER
        protected Callback<GetAuthSessionTicketResponse_t> GetAuthSessionTicketResponse;
        private byte[] _ticket;

        private void Start()
        {
            ServerStatusService.Instance.serverStatusReceived.AddListener(UpdateServerDropdownList);
            serverDropdown.onValueChanged.AddListener(OnSelectServer);

            GetAuthSessionTicketResponse = Callback<GetAuthSessionTicketResponse_t>.Create(OnGetAuthSessionTicketResponse);
        }

        private void UpdateServerDropdownList()
        {
            var list = new List<Dropdown.OptionData>();
            foreach (var serverStatus in ServerStatusService.Instance.serverStatus) list.Add(new Dropdown.OptionData(serverStatus.name));
            serverDropdown.options = list;
        }

        private void OnSelectServer(int idx)
        {
            EmberfateNetworkManager.Instance.networkAddress = ServerStatusService.Instance.serverStatus[idx].ip;
            EmberfateNetworkManager.Instance.GetComponent<TelepathyTransport>().port = ServerStatusService.Instance.serverStatus[idx].port;
            Debug.Log("Selected GS: " + ServerStatusService.Instance.serverStatus[idx].name);
        }


        public void Connect()
        {
            GetAuthSessionTicket();
        }

        private void GetAuthSessionTicket()
        {
            _ticket = new byte[1024];
            SteamUser.GetAuthSessionTicket(_ticket, 1024, out var pcbTicket);
        }

        private void OnGetAuthSessionTicketResponse(GetAuthSessionTicketResponse_t pCallback)
        {
            if (pCallback.m_eResult == EResult.k_EResultOK)
            {
                SteamTokenAuthenticator.AuthTicket = GetHexStringFromByteArray(_ticket);
                // Debug.Log("SteamId: " + SteamUser.GetSteamID().m_SteamID);
                // Debug.Log("AuthTicket: " + SteamTokenAuthenticator.AuthTicket);
                EmberfateNetworkManager.Instance.StartClient();
            }
        }

        private string GetHexStringFromByteArray(byte[] bytes)
        {
            var hexWithDashes = BitConverter.ToString(bytes);
            return hexWithDashes.Replace("-", "");
        }
#endif
    }
}