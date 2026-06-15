using System;
using UnityEngine;
using TMPro;

namespace MRReP.UI
{
    public class ConfirmDialog : MonoBehaviour
    {
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private TextMeshProUGUI messageText;

        private Action<bool> _callback;

        private void Start()
        {
            if (dialogPanel != null)
                dialogPanel.SetActive(false);
        }

        public void Show(string message, Action<bool> callback)
        {
            _callback = callback;
            messageText.text = message;
            dialogPanel.SetActive(true);
        }

        public void OnYesClicked()
        {
            dialogPanel.SetActive(false);
            _callback?.Invoke(true);
            _callback = null;
        }

        public void OnNoClicked()
        {
            dialogPanel.SetActive(false);
            _callback?.Invoke(false);
            _callback = null;
        }
    }
}
