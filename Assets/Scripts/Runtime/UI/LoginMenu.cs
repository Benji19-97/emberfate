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

        private void Start()
        {
            ServerStatusService.Instance.serverStatusReceived.AddListener(UpdateServerDropdownList);
            serverDropdown.onValueChanged.AddListener(OnSelectServer);
            ServerStatusService.Instance.SendGetRequest();
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
    }
}