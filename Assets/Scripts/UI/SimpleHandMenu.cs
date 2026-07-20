using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

namespace MRReP.UI
{
    public class SimpleHandMenu : MonoBehaviour
    {
        [Header("菜单设置")]
        [SerializeField] private float spacing = 0.04f;
        [SerializeField] private Vector3 buttonSize = new Vector3(0.04f, 0.03f, 0.01f);
        [SerializeField] private float menuOffsetZ = 0.15f;
        [SerializeField] private float clickDistance = 0.08f;

        [Header("功能引用")]
        [SerializeField] private PreferredPathMenuController controller;

        private GameObject menuPanel;
        private GameObject[] buttons;
        private bool wasPinching;
        private float lastActionTime;

        private void Start() { CreateMenu(); }

        private void Update()
        {
            if (controller == null) return;
            bool isPinching = CheckPinch();
            if (isPinching && !wasPinching) TrySelectButton();
            wasPinching = isPinching;

            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
                TrySelectByRay(Camera.main.ScreenPointToRay(Input.mousePosition));
            #endif
        }

        private bool CheckPinch()
        {
            foreach (var hand in new[] { Handedness.Right, Handedness.Left })
            {
                if (HandJointUtils.TryGetJointPose(TrackedHandJoint.ThumbTip, hand, out var t) &&
                    HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, hand, out var i))
                {
                    float d = Vector3.Distance(t.Position, i.Position);
                    if (d < 0.03f) return true;
                }
            }
            return false;
        }

        private void TrySelectButton()
        {
            if (Time.time - lastActionTime < 0.3f) return;

            foreach (var hand in new[] { Handedness.Right, Handedness.Left })
            {
                // 用掌心位置检测，比食指尖更稳定
                if (HandJointUtils.TryGetJointPose(TrackedHandJoint.Palm, hand, out var palm))
                {
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i] == null) continue;
                        float dist = Vector3.Distance(palm.Position, buttons[i].transform.position);
                        if (dist < clickDistance)
                        {
                            OnButtonClicked(i);
                            lastActionTime = Time.time;
                            StartCoroutine(Flash(buttons[i]));
                            return;
                        }
                    }
                }
            }
        }

        private void TrySelectByRay(Ray ray)
        {
            if (Time.time - lastActionTime < 0.3f) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                if (buttons[i].GetComponent<Collider>().Raycast(ray, out RaycastHit hit, 1.0f))
                {
                    OnButtonClicked(i);
                    lastActionTime = Time.time;
                    StartCoroutine(Flash(buttons[i]));
                    return;
                }
            }
        }

        private void OnButtonClicked(int index)
        {
            switch (index)
            {
                case 0: controller.OnAddClicked(); Debug.Log("[Menu] Add"); break;
                case 1: controller.OnClearClicked(); Debug.Log("[Menu] Clear"); break;
                case 2: controller.OnSendClicked(); Debug.Log("[Menu] Send"); break;
                case 3: controller.OnBackClicked(); Debug.Log("[Menu] Back"); break;
            }
        }

        private System.Collections.IEnumerator Flash(GameObject button)
        {
            var r = button.GetComponent<Renderer>();
            if (r == null) yield break;
            Color orig = r.material.color;
            r.material.color = Color.white;
            yield return new WaitForSeconds(0.15f);
            if (r != null) r.material.color = orig;
        }

        private void CreateMenu()
        {
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(transform, false);
            menuPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            string[] labels = { "Add", "Clear", "Send", "Back" };
            Color[] colors = { Color.green, Color.yellow, Color.blue, Color.red };
            buttons = new GameObject[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
                b.name = labels[i];
                b.transform.SetParent(menuPanel.transform, false);
                b.transform.localPosition = new Vector3(0, -i * spacing, 0);
                b.transform.localScale = buttonSize;
                b.GetComponent<Renderer>().material.color = colors[i];

                var t = new GameObject("Text_" + labels[i]);
                t.transform.SetParent(b.transform, false);
                t.transform.localPosition = new Vector3(0, 0, -0.006f);
                var tm = t.AddComponent<TextMesh>();
                tm.text = labels[i];
                tm.characterSize = 0.05f;
                tm.fontSize = 120;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = Color.white;

                buttons[i] = b;
            }
            Debug.Log("[SimpleHandMenu] 菜单已生成");
        }
    }
}
