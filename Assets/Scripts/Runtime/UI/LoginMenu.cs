using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Runtime.UI
{
    public class LoginMenu : MonoBehaviour
    {
        [SerializeField] private Dropdown serverDropdown;

#if !UNITY_SERVER
        protected Steamworks.Callback<Steamworks.GetAuthSessionTicketResponse_t> GetAuthSessionTicketResponse;
        
        private byte[] _ticket;
        private uint _pcbTicket;
        private Steamworks.HAuthTicket _hAuthTicket;


        private void Start()
        {
            ServerStatusService.Instance.serverStatusReceived.AddListener(UpdateServerDropdownList);
            serverDropdown.onValueChanged.AddListener(OnSelectServer);
            ServerStatusService.Instance.SendGetRequest();
            GetAuthSessionTicketResponse = Steamworks.Callback<Steamworks.GetAuthSessionTicketResponse_t>.Create(OnGetAuthSessionTicketResponse);

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
            _hAuthTicket = Steamworks.SteamUser.GetAuthSessionTicket(_ticket, 1024, out _pcbTicket);

        }

        private void OnGetAuthSessionTicketResponse(Steamworks.GetAuthSessionTicketResponse_t pCallback)
        {
            if (pCallback.m_eResult == Steamworks.EResult.k_EResultOK)
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