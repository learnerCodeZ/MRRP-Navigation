using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using MRReP.Path;
using MRReP.ROS;

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
        [SerializeField] private QRAlignment qrAlignment;

        // ── 面板 ──
        // 1. MainMenu（Preferred Path + Calibrate）
        private GameObject mainMenuPanel;
        private GameObject btnPreferredPath;
        private GameObject btnCalibrate;
        private TextMesh alignStatusText;

        // 2. 操作菜单（Add/Clear/Send/Back）
        private GameObject menuPanel;
        private GameObject[] buttons;

        // 3. 画线面板（Points:N + Back）
        private GameObject drawPanel;
        private TextMesh pointCountText;
        private GameObject drawBackButton;

        // 4. 确认面板（Yes/No）
        private GameObject confirmPanel;
        private TextMesh confirmText;
        private GameObject confirmYesButton;
        private GameObject confirmNoButton;

        // 5. 扫描面板（"扫描中..." / "已对齐" + Back）
        private GameObject calibratePanel;
        private TextMesh calibrateText;
        private GameObject calibrateBackButton;

        // ── 状态 ──
        private enum PanelMode { MainMenu, Menu, Draw, Confirm, Calibrate }
        private PanelMode mode = PanelMode.MainMenu;
        private bool inConfirmMode;
        private string pendingAction;
        private float lastActionTime;
        private PathData pathData;

        private void Start()
        {
            pathData = FindObjectOfType<PathData>();
            CreateMainMenu();
            CreateMenu();
            CreateDrawPanel();
            CreateConfirmPanel();
            CreateCalibratePanel();
            ShowMainMenu();
        }

        private void Update()
        {
            if (controller == null) return;

            if (Time.time - lastActionTime > 0.3f)
                TryTouchButton();

            // 画线模式：实时更新点数
            if (mode == PanelMode.Draw && pathData != null && pointCountText != null)
                pointCountText.text = "Points: " + pathData.Count;

            // MainMenu：实时更新对齐状态
            if (mode == PanelMode.MainMenu && alignStatusText != null && qrAlignment != null)
                alignStatusText.text = qrAlignment.IsAligned ? "Aligned" : "Not aligned";

            // 扫描模式：检测对齐完成
            if (mode == PanelMode.Calibrate && qrAlignment != null && qrAlignment.IsAligned)
            {
                if (calibrateText != null) calibrateText.text = "Aligned!";
                StartCoroutine(ReturnToMainAfterDelay(2f));
            }

#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
                TrySelectByRay(Camera.main.ScreenPointToRay(Input.mousePosition));
#endif
        }

        #region 面板切换

        private void HideAll()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (menuPanel) menuPanel.SetActive(false);
            if (drawPanel) drawPanel.SetActive(false);
            if (confirmPanel) confirmPanel.SetActive(false);
            if (calibratePanel) calibratePanel.SetActive(false);
        }

        private void ShowMainMenu()
        {
            HideAll();
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
            mode = PanelMode.MainMenu;
        }

        private void ShowMenuPanel()
        {
            HideAll();
            if (menuPanel) menuPanel.SetActive(true);
            mode = PanelMode.Menu;
        }

        private void ShowDrawPanel()
        {
            HideAll();
            if (drawPanel) drawPanel.SetActive(true);
            mode = PanelMode.Draw;
        }

        private void ShowConfirmPanel(string question, string action)
        {
            HideAll();
            if (confirmPanel) confirmPanel.SetActive(true);
            if (confirmText) confirmText.text = question;
            inConfirmMode = true;
            pendingAction = action;
            mode = PanelMode.Confirm;
        }

        private void ShowCalibratePanel()
        {
            HideAll();
            if (calibratePanel) calibratePanel.SetActive(true);
            if (calibrateText) calibrateText.text = "Scanning...";
            if (qrAlignment != null) qrAlignment.StartScanning();
            mode = PanelMode.Calibrate;
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

        private System.Collections.IEnumerator ReturnToMainAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (mode == PanelMode.Calibrate)
                ShowMainMenu();
        }

        #endregion

        #region 触摸检测

        private void TryTouchButton()
        {
            foreach (var hand in new[] { Handedness.Right })
            {
                if (!HandJointUtils.TryGetJointPose(TrackedHandJoint.IndexTip, hand, out var index))
                    continue;

                switch (mode)
                {
                    case PanelMode.MainMenu:
                        TryTouchMainMenu(index.Position);
                        break;
                    case PanelMode.Menu:
                        TryTouchMenu(index.Position);
                        break;
                    case PanelMode.Draw:
                        TryTouchDraw(index.Position);
                        break;
                    case PanelMode.Confirm:
                        TryTouchConfirm(index.Position);
                        break;
                    case PanelMode.Calibrate:
                        TryTouchCalibrate(index.Position);
                        break;
                }
            }
        }

        private void TryTouchMainMenu(Vector3 indexPos)
        {
            if (btnPreferredPath != null && Distance(indexPos, btnPreferredPath))
            { ShowMenuPanel(); Flash(btnPreferredPath); }
            else if (btnCalibrate != null && Distance(indexPos, btnCalibrate))
            { ShowCalibratePanel(); Flash(btnCalibrate); }
        }

        private void TryTouchMenu(Vector3 indexPos)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                if (Distance(indexPos, buttons[i]))
                {
                    OnMenuButtonClicked(i);
                    Flash(buttons[i]);
                    return;
                }
            }
        }

        private void TryTouchDraw(Vector3 indexPos)
        {
            if (drawBackButton != null && Distance(indexPos, drawBackButton))
            { controller.OnBackClicked(); ShowMenuPanel(); Flash(drawBackButton); }
        }

        private void TryTouchConfirm(Vector3 indexPos)
        {
            if (confirmYesButton != null && Distance(indexPos, confirmYesButton))
            { ConfirmYes(); Flash(confirmYesButton); }
            else if (confirmNoButton != null && Distance(indexPos, confirmNoButton))
            { ConfirmNo(); Flash(confirmNoButton); }
        }

        private void TryTouchCalibrate(Vector3 indexPos)
        {
            if (calibrateBackButton != null && Distance(indexPos, calibrateBackButton))
            {
                if (qrAlignment != null) qrAlignment.StopScanning();
                ShowMainMenu();
                Flash(calibrateBackButton);
            }
        }

        private bool Distance(Vector3 a, GameObject btn)
        {
            if (Vector3.Distance(a, btn.transform.position) < touchDistance)
            { lastActionTime = Time.time; return true; }
            return false;
        }

        private void TrySelectByRay(Ray ray)
        {
            if (Time.time - lastActionTime < 0.3f) return;

            GameObject target = null;
            switch (mode)
            {
                case PanelMode.MainMenu:
                    target = RayHitButton(ray, new[] { btnPreferredPath, btnCalibrate });
                    if (target == btnPreferredPath) { ShowMenuPanel(); }
                    else if (target == btnCalibrate) { ShowCalibratePanel(); }
                    break;
                case PanelMode.Menu:
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        if (buttons[i] != null && buttons[i].GetComponent<Collider>().Raycast(ray, out RaycastHit h, 1f))
                        { OnMenuButtonClicked(i); break; }
                    }
                    break;
                case PanelMode.Draw:
                    if (drawBackButton != null && drawBackButton.GetComponent<Collider>().Raycast(ray, out RaycastHit h2, 1f))
                    { controller.OnBackClicked(); ShowMenuPanel(); }
                    break;
                case PanelMode.Confirm:
                    if (confirmYesButton != null && confirmYesButton.GetComponent<Collider>().Raycast(ray, out RaycastHit h3, 1f))
                    { ConfirmYes(); }
                    else if (confirmNoButton != null && confirmNoButton.GetComponent<Collider>().Raycast(ray, out h3, 1f))
                    { ConfirmNo(); }
                    break;
                case PanelMode.Calibrate:
                    if (calibrateBackButton != null && calibrateBackButton.GetComponent<Collider>().Raycast(ray, out RaycastHit h4, 1f))
                    { if (qrAlignment != null) qrAlignment.StopScanning(); ShowMainMenu(); }
                    break;
            }
            if (target != null) { lastActionTime = Time.time; Flash(target); }
        }

        private GameObject RayHitButton(Ray ray, GameObject[] candidates)
        {
            foreach (var c in candidates)
            {
                if (c == null) continue;
                if (c.GetComponent<Collider>().Raycast(ray, out RaycastHit h, 1f))
                { lastActionTime = Time.time; return c; }
            }
            return null;
        }

        #endregion

        private void OnMenuButtonClicked(int index)
        {
            switch (index)
            {
                case 0:
                    controller.OnAddClicked();
                    ShowDrawPanel();
                    break;
                case 1:
                    ShowConfirmPanel("Clear path?", "Clear");
                    break;
                case 2:
                    ShowConfirmPanel("Send path?", "Send");
                    break;
                case 3:
                    ShowMainMenu();
                    break;
            }
        }

        private void Flash(GameObject button)
        {
            StartCoroutine(FlashRoutine(button));
        }

        private System.Collections.IEnumerator FlashRoutine(GameObject button)
        {
            var r = button.GetComponent<Renderer>();
            if (r == null) yield break;
            Color orig = r.material.color;
            r.material.color = Color.white;
            yield return new WaitForSeconds(0.15f);
            if (r != null) r.material.color = orig;
        }

        #region 创建 UI

        private void CreateMainMenu()
        {
            mainMenuPanel = new GameObject("MainMenuPanel");
            mainMenuPanel.transform.SetParent(transform, false);
            mainMenuPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            // 对齐状态文字
            var statusObj = new GameObject("AlignStatus");
            statusObj.transform.SetParent(mainMenuPanel.transform, false);
            statusObj.transform.localPosition = new Vector3(0, 0.015f, 0);
            alignStatusText = statusObj.AddComponent<TextMesh>();
            alignStatusText.text = "Not aligned";
            alignStatusText.characterSize = 0.002f;
            alignStatusText.fontSize = 48;
            alignStatusText.anchor = TextAnchor.MiddleCenter;
            alignStatusText.alignment = TextAlignment.Center;
            alignStatusText.color = new Color(1f, 0.5f, 0f); // 橙色

            // Preferred Path（绿）
            btnPreferredPath = CreateButton("Path", Color.green,
                mainMenuPanel.transform, new Vector3(0, -0.01f, 0));

            // Calibrate（蓝）
            btnCalibrate = CreateButton("Align", new Color(0.2f, 0.6f, 1f),
                mainMenuPanel.transform, new Vector3(0, -0.01f - spacing, 0));

            Debug.Log("[SimpleHandMenu] 主菜单(MainMenu)已生成");
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
                buttons[i] = CreateButton(labels[i], colors[i], menuPanel.transform, new Vector3(0, -i * spacing, 0));

            menuPanel.SetActive(false);
            Debug.Log("[SimpleHandMenu] 操作菜单(MenuPanel)已生成");
        }

        private void CreateDrawPanel()
        {
            drawPanel = new GameObject("DrawPanel");
            drawPanel.transform.SetParent(transform, false);
            drawPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            var countObj = new GameObject("PointCount");
            countObj.transform.SetParent(drawPanel.transform, false);
            countObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            pointCountText = countObj.AddComponent<TextMesh>();
            pointCountText.text = "Points: 0";
            pointCountText.characterSize = 0.002f;
            pointCountText.fontSize = 48;
            pointCountText.anchor = TextAnchor.MiddleCenter;
            pointCountText.alignment = TextAlignment.Center;
            pointCountText.color = Color.cyan;

            drawBackButton = CreateButton("Back", Color.red, drawPanel.transform, new Vector3(0, -0.04f, 0));
            drawPanel.SetActive(false);
            Debug.Log("[SimpleHandMenu] 画线面板已生成");
        }

        private void CreateConfirmPanel()
        {
            confirmPanel = new GameObject("ConfirmPanel");
            confirmPanel.transform.SetParent(transform, false);
            confirmPanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

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

            confirmYesButton = CreateButton("Yes", Color.green, confirmPanel.transform, new Vector3(-0.025f, -0.03f, 0));
            confirmYesButton.transform.localScale = new Vector3(0.03f, 0.03f, 0.01f);

            confirmNoButton = CreateButton("No", Color.red, confirmPanel.transform, new Vector3(0.025f, -0.03f, 0));
            confirmNoButton.transform.localScale = new Vector3(0.03f, 0.03f, 0.01f);

            confirmPanel.SetActive(false);
            Debug.Log("[SimpleHandMenu] 确认面板已生成");
        }

        private void CreateCalibratePanel()
        {
            calibratePanel = new GameObject("CalibratePanel");
            calibratePanel.transform.SetParent(transform, false);
            calibratePanel.transform.localPosition = new Vector3(0, 0, menuOffsetZ);

            var scanObj = new GameObject("ScanText");
            scanObj.transform.SetParent(calibratePanel.transform, false);
            scanObj.transform.localPosition = new Vector3(0, 0.01f, 0);
            calibrateText = scanObj.AddComponent<TextMesh>();
            calibrateText.text = "Scanning...";
            calibrateText.characterSize = 0.002f;
            calibrateText.fontSize = 48;
            calibrateText.anchor = TextAnchor.MiddleCenter;
            calibrateText.alignment = TextAlignment.Center;
            calibrateText.color = Color.cyan;

            calibrateBackButton = CreateButton("Back", Color.red, calibratePanel.transform, new Vector3(0, -0.04f, 0));
            calibratePanel.SetActive(false);
            Debug.Log("[SimpleHandMenu] 扫描面板已生成");
        }

        private GameObject CreateButton(string label, Color color, Transform parent, Vector3 localPos)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = label;
            b.layer = 2; // IgnoreRaycast
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
