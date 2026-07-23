using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using MRReP.Path;

namespace MRReP.UI
{
    public class SimpleHandMenu : MonoBehaviour
    {
        [Header("菜单设置")]
        [SerializeField] private float spacing = 0.04f;
        [SerializeField] private Vector3 buttonSize = new Vector3(0.04f, 0.03f, 0.01f);
        [SerializeField] private float menuOffsetZ = 0.15f;
        [SerializeField] private float touchDistance = 0.03f;

        [Header("功能引用")]
        [SerializeField] private PreferredPathMenuController controller;

        // 主菜单
        private GameObject menuPanel;
        private GameObject[] buttons;

        // 画线状态面板
        private GameObject drawPanel;
        private TextMesh pointCountText;
        private GameObject drawBackButton;

        // 确认面板（Send/Clear 的 Yes/No）
        private GameObject confirmPanel;
        private TextMesh confirmText;
        private GameObject confirmYesButton;
        private GameObject confirmNoButton;
        private bool inConfirmMode;
        private string pendingAction; // "Send" or "Clear"

        private float lastActionTime;
        private bool inDrawMode;
        private PathData pathData;

        private void Start()
        {
            pathData = FindObjectOfType<PathData>();
            CreateMenu();
            CreateDrawPanel();
            CreateConfirmPanel();
            ShowMenuPanel();
        }

        private void Update()
        {
            if (controller == null) return;

            // 直接触摸检测
            if (Time.time - lastActionTime > 0.3f)
                TryTouchButton();

            // 画线模式：实时更新点数显示
            if (inDrawMode && pathData != null && pointCountText != null)
                pointCountText.text = "Points: " + pathData.Count;

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
                TrySelectByRay(Camera.main.ScreenPointToRay(Input.mousePosition));
#endif
        }

        #region 面板切换

        private void ShowMenuPanel()
        {
            if (menuPanel) menuPanel.SetActive(true);
            if (drawPanel) drawPanel.SetActive(false);
            if (confirmPanel) confirmPanel.SetActive(false);
            inDrawMode = false;
            inConfirmMode = false;
        }

        private void ShowDrawPanel()
        {
            if (menuPanel) menuPanel.SetActive(false);
            if (drawPanel) drawPanel.SetActive(true);
            inDrawMode = true;
        }

        private void ShowConfirmPanel(string question, string action)
        {
            if (menuPanel) menuPanel.SetActive(false);
            if (confirmPanel) confirmPanel.SetActive(true);
            if (confirmText) confirmText.text = question;
            inConfirmMode = true;
            pendingAction = action;
        }

        private void ConfirmYes()
        {
            if (pendingAction == "Send") controller.OnSendClicked();
            else if (pendingAction == "Clear") controller.OnClearClicked();
            Debug.Log("[Menu] Confirmed: " + pendingAction);
            inConfirmMode = false;
            pendingAction = null;
            ShowMenuPanel();
        }

        private void ConfirmNo()
        {
            Debug.Log("[Menu] Cancelled: " + pendingAction);
            inConfirmMode = false;
            pendingAction = null;
            ShowMenuPanel();
        }

        #endregion

        #region 触摸检测

        private void TryTouchButton()
        {
            // 只右手点按钮（左手只负责显示面板，完全不参与交互）
            foreach (var hand in new[] { Handedness.Right })
            {
                if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, hand, out var index))
                    continue;

                // 确认面板：检测 Yes/No
                if (inConfirmMode)
                {
                    if (confirmYesButton != null &&
                        Vector3.Distance(index.Position, confirmYesButton.transform.position) < touchDistance)
                    {
                        ConfirmYes();
                        lastActionTime = Time.time;
                        StartCoroutine(Flash(confirmYesButton));
                        return;
                    }
                    if (confirmNoButton != null &&
                        Vector3.Distance(index.Position, confirmNoButton.transform.position) < touchDistance)
                    {
                        ConfirmNo();
                        lastActionTime = Time.time;
                        StartCoroutine(Flash(confirmNoButton));
                        return;
                    }
                    continue; // 确认模式下不检测其他按钮
                }

                if (!inDrawMode)
                {
                    // 主菜单：检测 4 个按钮
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i] == null || !buttons[i].activeSelf) continue;
                        if (Vector3.Distance(index.Position, buttons[i].transform.position) < touchDistance)
                        {
                            OnMenuButtonClicked(i);
                            lastActionTime = Time.time;
                            StartCoroutine(Flash(buttons[i]));
                            return;
                        }
                    }
                }
                else
                {
                    // 画线面板：检测 Back 按钮
                    if (drawBackButton != null &&
                        Vector3.Distance(index.Position, drawBackButton.transform.position) < touchDistance)
                    {
                        controller.OnBackClicked();
                        ShowMenuPanel();
                        lastActionTime = Time.time;
                        StartCoroutine(Flash(drawBackButton));
                        return;
                    }
                }
            }
        }

        private void TrySelectByRay(Ray ray)
        {
            if (Time.time - lastActionTime < 0.3f) return;

            // 确认面板：Editor 鼠标也能点 Yes/No
            if (inConfirmMode)
            {
                if (confirmYesButton != null && confirmYesButton.GetComponent<Collider>().Raycast(ray, out RaycastHit h, 1f))
                { ConfirmYes(); lastActionTime = Time.time; return; }
                if (confirmNoButton != null && confirmNoButton.GetComponent<Collider>().Raycast(ray, out h, 1f))
                { ConfirmNo(); lastActionTime = Time.time; return; }
                return;
            }

            if (!inDrawMode && !inConfirmMode)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;
                    if (buttons[i].GetComponent<Collider>().Raycast(ray, out RaycastHit hit, 1.0f))
                    {
                        OnMenuButtonClicked(i);
                        lastActionTime = Time.time;
                        StartCoroutine(Flash(buttons[i]));
                        return;
                    }
                }
            }
            else
            {
                if (drawBackButton != null &&
                    drawBackButton.GetComponent<Collider>().Raycast(ray, out RaycastHit hit, 1.0f))
                {
                    controller.OnBackClicked();
                    ShowMenuPanel();
                    lastActionTime = Time.time;
                    StartCoroutine(Flash(drawBackButton));
                }
            }
        }

        #endregion

        private void OnMenuButtonClicked(int index)
        {
            switch (index)
            {
                case 0: // Add → 进画线模式
                    controller.OnAddClicked();
                    Debug.Log("[Menu] Add → 画线模式");
                    ShowDrawPanel();
                    break;
                case 1: // Clear → 确认面板
                    ShowConfirmPanel("Clear path?", "Clear");
                    break;
                case 2: // Send → 确认面板
                    ShowConfirmPanel("Send path?", "Send");
                    break;
                case 3:
                    controller.OnBackClicked();
                    Debug.Log("[Menu] Back");
                    break;
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

        #region 创建 UI

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
                buttons[i] = CreateButton(labels[i], colors[i],
                    menuPanel.transform, new Vector3(0, -i * spacing, 0));
            }
            Debug.Log("[SimpleHandMenu] 主菜单已生成");
        }

        private void CreateDrawPanel()
        {
            drawPanel = new GameObject("DrawPanel");
            drawPanel.transform.SetParent(transform, false);
            drawPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            // 点数显示
            var countObj = new GameObject("PointCount");
            countObj.transform.SetParent(drawPanel.transform, false);
            countObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            pointCountText = countObj.AddComponent<TextMesh>();
            pointCountText.text = "Points: 0";
            pointCountText.characterSize = 0.002f;   // 跟 Back 按钮文字一样小（Back 在 Cube 里被缩了）
            pointCountText.fontSize = 48;
            pointCountText.anchor = TextAnchor.MiddleCenter;
            pointCountText.alignment = TextAlignment.Center;
            pointCountText.color = Color.cyan;

            // Back 按钮
            drawBackButton = CreateButton("Back", Color.red,
                drawPanel.transform, new Vector3(0, -0.04f, 0));

            drawPanel.SetActive(false);
            Debug.Log("[SimpleHandMenu] 画线面板已生成");
        }

        private void CreateConfirmPanel()
        {
            confirmPanel = new GameObject("ConfirmPanel");
            confirmPanel.transform.SetParent(transform, false);
            confirmPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            // 问题文字
            var qObj = new GameObject("ConfirmText");
            qObj.transform.SetParent(confirmPanel.transform, false);
            qObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            confirmText = qObj.AddComponent<TextMesh>();
            confirmText.text = "Sure?";
            confirmText.characterSize = 0.002f;
            confirmText.fontSize = 48;
            confirmText.anchor = TextAnchor.MiddleCenter;
            confirmText.alignment = TextAlignment.Center;
            confirmText.color = Color.cyan;

            // Yes（绿）和 No（红）并排
            confirmYesButton = CreateButton("Yes", Color.green,
                confirmPanel.transform, new Vector3(-0.025f, -0.03f, 0));
            confirmYesButton.transform.localScale = new Vector3(0.03f, 0.03f, 0.01f);

            confirmNoButton = CreateButton("No", Color.red,
                confirmPanel.transform, new Vector3(0.025f, -0.03f, 0));
            confirmNoButton.transform.localScale = new Vector3(0.03f, 0.03f, 0.01f);

            confirmPanel.SetActive(false);
            Debug.Log("[SimpleHandMenu] 确认面板已生成");
        }

        private GameObject CreateButton(string label, Color color, Transform parent, Vector3 localPos)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = label;
            b.layer = 2; // IgnoreRaycast：画线射线不打到按钮上，只打地板
            b.transform.SetParent(parent, false);
            b.transform.localPosition = localPos;
            b.transform.localScale = buttonSize;
            b.GetComponent<Renderer>().material.color = color;

            var t = new GameObject("Text_" + label);
            t.transform.SetParent(b.transform, false);
            t.transform.localPosition = new Vector3(0, 0, -0.006f);
            var tm = t.AddComponent<TextMesh>();
            tm.text = label;
            tm.characterSize = 0.05f;
            tm.fontSize = 120;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;

            return b;
        }

        #endregion
    }
}
