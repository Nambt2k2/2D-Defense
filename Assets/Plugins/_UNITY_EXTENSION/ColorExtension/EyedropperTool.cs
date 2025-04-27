#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;

public class EyedropperTool : MonoBehaviour {
    public Image colorPreview;
    public Button toggleButton;
    private bool isEyedropperActive = false;
    private Vector2 lastMousePos;

    // Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetPhysicalCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT point, uint dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT {
        public int X;
        public int Y;
    }

    void Start() {
        if (toggleButton != null) {
            toggleButton.onClick.AddListener(ToggleEyedropper);
        }
        lastMousePos = Vector2.zero;
    }

    void Update() {
        if (isEyedropperActive) {
            POINT currentMousePos;
            if (!GetCursorPos(out currentMousePos)) {
                Debug.LogError("Không thể lấy vị trí chuột!");
                return;
            }

            Vector2 currentPos = new Vector2(currentMousePos.X, currentMousePos.Y);
            if (currentPos != lastMousePos) {
                PickColor();
                lastMousePos = currentPos;
            }
        }
    }

    void ToggleEyedropper() {
        isEyedropperActive = !isEyedropperActive;
        if (toggleButton != null) {
            toggleButton.GetComponent<Image>().color = isEyedropperActive ? Color.green : Color.white;
        }
    }

    float GetDpiScaleForMousePosition(POINT mousePos) {
        IntPtr monitor = MonitorFromPoint(mousePos, MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero) {
            Debug.LogWarning("Không thể lấy thông tin màn hình, sử dụng DPI mặc định.");
            return 1f;
        }

        uint dpiX, dpiY;
        if (GetDpiForMonitor(monitor, 0, out dpiX, out dpiY) != 0) {
            Debug.LogWarning("Không thể lấy DPI, sử dụng DPI mặc định.");
            return 1f;
        }

        Debug.Log($"DPI của màn hình: {dpiX}x{dpiY}");
        return dpiX / 96f;
    }

    bool IsValidScreenPosition(int x, int y) {
        int screenWidth = GetSystemMetrics(SM_CXSCREEN);
        int screenHeight = GetSystemMetrics(SM_CYSCREEN);
        return x >= 0 && x < screenWidth && y >= 0 && y < screenHeight;
    }

    void PickColor() {
        POINT mousePos;
        bool usePhysicalCursor = GetPhysicalCursorPos(out mousePos);
        if (!usePhysicalCursor) {
            if (!GetCursorPos(out mousePos)) {
                Debug.LogError("Không thể lấy vị trí chuột!");
                return;
            }
        }

        Debug.Log($"Tọa độ chuột (vật lý): ({mousePos.X}, {mousePos.Y})");

        float dpiScale = GetDpiScaleForMousePosition(mousePos);
        int adjustedX = usePhysicalCursor ? mousePos.X : (int)(mousePos.X / dpiScale);
        int adjustedY = usePhysicalCursor ? mousePos.Y : (int)(mousePos.Y / dpiScale);
        Debug.Log($"Tọa độ sau khi điều chỉnh: ({adjustedX}, {adjustedY})");

        if (!IsValidScreenPosition(adjustedX, adjustedY)) {
            Debug.LogError($"Tọa độ chuột không hợp lệ: ({adjustedX}, {adjustedY})");
            return;
        }

        IntPtr hdc = GetDC(IntPtr.Zero);
        if (hdc == IntPtr.Zero) {
            Debug.LogError("Không thể lấy device context!");
            return;
        }

        uint pixel = GetPixel(hdc, adjustedX, adjustedY);
        ReleaseDC(IntPtr.Zero, hdc);

        if (pixel == 0xFFFFFFFF) {
            Debug.LogError("GetPixel trả về giá trị không hợp lệ (0xFFFFFFFF).");
            return;
        }

        byte r = (byte)(pixel & 0x000000FF);
        byte g = (byte)((pixel & 0x0000FF00) >> 8);
        byte b = (byte)((pixel & 0x00FF0000) >> 16);
        Debug.Log($"RGB: ({r}, {g}, {b}), Hex={ColorUtility.ToHtmlStringRGB(new Color32(r, g, b, 255))}");

        Color pickedColor = new Color32(r, g, b, 255);
        if (colorPreview != null) {
            colorPreview.color = pickedColor;
        }
    }
}
#endif