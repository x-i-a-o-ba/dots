using Common;
using UnityEngine;

/// <summary>
/// 将屏幕鼠标坐标投影到 Y=0 地面平面，供 RTS 右键移动指令使用。
/// </summary>
public class MouseWorldPositon : MonoSingleton<MouseWorldPositon>
{
    /// <summary>射线与地面平面求交，返回世界空间坐标。</summary>
    public Vector3 GetPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}
