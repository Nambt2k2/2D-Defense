#if UNITY_EDITOR
using UnityEngine;

public class AutoGetColorSprite : MonoBehaviour {
    public Vector2Int cellSize;
    public SpriteRenderer sprite;
    public Color[] arr_colorSprite;

    [ContextMenu("GetSpriteColor")]
    public void GetColorSprite() {
        arr_colorSprite = new Color[cellSize.x * cellSize.y];
        Vector2[] centers = GetQuadrantCenters();
        for (int i = 0; i < cellSize.x * cellSize.y; i++) {
            Vector2Int pixelPos = new Vector2Int(Mathf.FloorToInt(centers[i].x), Mathf.FloorToInt(centers[i].y));
            arr_colorSprite[i] = sprite.sprite.texture.GetPixel(pixelPos.x, pixelPos.y);
        }
    }

    public Vector2[] GetQuadrantCenters() {
        if (sprite == null || sprite.sprite == null) {
            Debug.LogError("SpriteRenderer hoặc Sprite bị thiếu!");
            return null;
        }

        // Lấy texture của sprite
        Texture2D texture = sprite.sprite.texture;
        float w = texture.width;
        float h = texture.height;

        // Kích thước mỗi mảnh
        float cellSizeX = w / cellSize.x;
        float cellSizeY = h / cellSize.y;

        // Mảng chứa vị trí trung tâm các ô trong sprite
        Vector2[] centers = new Vector2[cellSize.x * cellSize.y];
        int index = 0;

        // Duyệt qua lưới XY
        for (int j = 0; j < cellSize.y; j++) {// Hàng (dưới lên trên)
            for (int i = 0; i < cellSize.x; i++) {// Cột (trái sang phải)
                // Tính trung tâm của mảnh (i, j)
                float centerX = (i + 0.5f) * cellSizeX;
                float centerY = (j + 0.5f) * cellSizeY;
                centers[index] = new Vector2(centerX, centerY);
                index++;
            }
        }
        return centers;
    }
}
#endif