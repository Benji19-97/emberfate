using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
#if !UNITY_SERVER
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
            foreach (var serverStatus in ServerStatusService.Instance.serverStatus)
            {
                list.Add(new Dropdown.OptionData(serverStatus.name));
            }
            serverDropdown.options = list;
        }

        private void OnSelectServer(int idx)
        {
            NetworkManager.singleton.networkAddress = ServerStatusService.Instance.serverStatus[idx].ip;
            Debug.Log("Selected GS: " + ServerStatusService.Instance.serverStatus[idx].name);
        }


        public void Connect()
        {
            GetAuthSessionTicket();
        }

        private void GetAuthSessionTicket()
        {
            _ticket = new byte[1024];
            Steamworks.SteamUser.GetAuthSessionTicket(_ticket, 1024, out uint pcbTicket);
        }

        private void OnGetAuthSessionTicketResponse(GetAuthSessionTicketResponse_t pCallback)
        {
            if (pCallback.m_eResult == EResult.k_EResultOK)
            {
                SteamTokenAuthenticator.AuthTicket = GetHexStringFromByteArray(_ticket);
                NetworkManager.singleton.StartClient();
            }
        }

        private string GetHexStringFromByteArray(byte[] bytes)
        {
            string hexWithDashes = BitConverter.ToString(bytes);
            return hexWithDashes.Replace("-", "");
        }
#endif
    }
}