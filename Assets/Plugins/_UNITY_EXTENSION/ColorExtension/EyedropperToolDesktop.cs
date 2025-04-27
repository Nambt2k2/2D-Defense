#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using System.Runtime.InteropServices;
using System;

public class EyedropperToolDesktop : MonoBehaviour {
    public Image colorPreview; // UI Image để hiển thị màu đã chọn
    public Button toggleButton; // Nút để bật/tắt chế độ eyedropper
    private bool isEyedropperActive = false;
    private Vector2 lastMousePos; // Lưu vị trí chuột trước đó để kiểm tra di chuyển

    // Win32 API để lấy màu và vị trí chuột
    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern uint GetPixel(IntPtr hdc, int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    private const int LOGPIXELSX = 88; // Hằng số để lấy DPI theo trục X

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT {
        public int X;
        public int Y;
    }

    void Start() {
        // Gán sự kiện cho nút toggle
        if (toggleButton != null) {
            toggleButton.onClick.AddListener(ToggleEyedropper);
        }
        // Khởi tạo vị trí chuột ban đầu
        lastMousePos = Vector2.zero;
    }

    void Update() {
        if (isEyedropperActive) {
            // Lấy vị trí chuột hiện tại bằng Win32 API
            POINT currentMousePos;
            if (!GetCursorPos(out currentMousePos)) {
                Debug.LogError("Không thể lấy vị trí chuột!");
                return;
            }

            // Chuyển đổi vị trí chuột thành Vector2 để so sánh
            Vector2 currentPos = new Vector2(currentMousePos.X, currentMousePos.Y);

            // Kiểm tra xem chuột có di chuyển không
            if (currentPos != lastMousePos) {
                PickColor();
                lastMousePos = currentPos; // Cập nhật vị trí chuột trước đó
            }
        }
    }

    void ToggleEyedropper() {
        isEyedropperActive = !isEyedropperActive;
        if (toggleButton != null) {
            toggleButton.GetComponent<Image>().color = isEyedropperActive ? Color.green : Color.white;
        }
    }

    void PickColor() {
        // Lấy vị trí chuột thực tế trên màn hình bằng Win32 API
        POINT mousePos;
        if (!GetCursorPos(out mousePos)) {
            Debug.LogError("Không thể lấy vị trí chuột!");
            return;
        }

        // Debug tọa độ chuột
        Debug.Log($"Tọa độ chuột: ({mousePos.X}, {mousePos.Y})");

        // Lấy device context của toàn màn hình
        IntPtr hdc = GetDC(IntPtr.Zero); // Lấy device context của toàn màn hình
        if (hdc == IntPtr.Zero) {
            Debug.LogError("Không thể lấy device context!");
            return;
        }

        // Lấy DPI của hệ thống bằng GetDeviceCaps
        int dpi = GetDeviceCaps(hdc, LOGPIXELSX);
        if (dpi == 0) {
            dpi = 96; // DPI mặc định nếu không lấy được
        }
        float dpiScale = dpi / 96f; // Tính tỷ lệ DPI scaling
        Debug.Log($"DPI: {dpi}, DPI Scale: {dpiScale}");

        // Điều chỉnh tọa độ chuột theo DPI scaling
        int adjustedX = (int)(mousePos.X / dpiScale);
        int adjustedY = (int)(mousePos.Y / dpiScale);
        Debug.Log($"Tọa độ sau khi điều chỉnh DPI: ({adjustedX}, {adjustedY})");

        uint pixel = GetPixel(hdc, adjustedX, adjustedY); // Lấy màu tại vị trí chuột
        ReleaseDC(IntPtr.Zero, hdc); // Giải phóng device context

        // Kiểm tra giá trị pixel
        if (pixel == 0xFFFFFFFF) // Giá trị lỗi của GetPixel
        {
            Debug.LogError("GetPixel trả về giá trị không hợp lệ (0xFFFFFFFF). Có thể tọa độ ngoài vùng hợp lệ.");
            return;
        }

        // Debug giá trị pixel thô
        Debug.Log($"Giá trị pixel thô: 0x{pixel:X8}");

        // Chuyển đổi màu từ định dạng Win32 (COLORREF) sang Unity Color
        byte r = (byte)(pixel & 0x000000FF); // Red
        byte g = (byte)((pixel & 0x0000FF00) >> 8); // Green
        byte b = (byte)((pixel & 0x00FF0000) >> 16); // Blue
        Debug.Log($"RGB: ({r}, {g}, {b})");

        Color pickedColor = new Color32(r, g, b, 255);

        // Cập nhật màu cho UI Image
        if (colorPreview != null) {
            colorPreview.color = pickedColor;
        }
    }
}
#endif