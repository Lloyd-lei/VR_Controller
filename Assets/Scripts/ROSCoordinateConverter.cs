using UnityEngine;

/// <summary>
/// Unity 左手系 ↔ ROS2 REP 103 右手系 坐标转换
///
/// Unity: 左手系, X右 Y上 Z前
/// ROS2: 右手系, X前 Y左 Z上 (REP 103)
///
/// 转换: x_ros=z_u, y_ros=-x_u, z_ros=y_u
/// </summary>
public static class ROSCoordinateConverter
{
    /// <summary>
    /// Unity 位置 → ROS2 位置
    /// (x右, y上, z前) → (x前, y左, z上)
    /// </summary>
    public static Vector3 UnityToROS(Vector3 unityPos)
    {
        return new Vector3(unityPos.z, -unityPos.x, unityPos.y);
    }

    /// <summary>
    /// Unity 四元数 → ROS2 四元数
    /// 通过旋转矩阵变换
    /// </summary>
    public static Quaternion UnityToROS(Quaternion unityRot)
    {
        // T: Unity → ROS 轴变换矩阵 (列优先)
        // ROS X = Unity Z, ROS Y = -Unity X, ROS Z = Unity Y
        Matrix4x4 T = new Matrix4x4(
            new Vector4(0, -1, 0, 0),  // col0
            new Vector4(0, 0, 1, 0),   // col1
            new Vector4(1, 0, 0, 0),   // col2
            new Vector4(0, 0, 0, 1)    // col3
        );
        Matrix4x4 Tinv = T.inverse;

        Matrix4x4 R_u = Matrix4x4.Rotate(unityRot);
        Matrix4x4 R_ros = T * R_u * Tinv;

        return R_ros.rotation;
    }

    /// <summary>
    /// ROS2 位置 → Unity 位置
    /// </summary>
    public static Vector3 ROSToUnity(Vector3 rosPos)
    {
        return new Vector3(-rosPos.y, rosPos.z, rosPos.x);
    }

    /// <summary>
    /// ROS2 四元数 → Unity 四元数
    /// </summary>
    public static Quaternion ROSToUnity(Quaternion rosRot)
    {
        Matrix4x4 T = new Matrix4x4(
            new Vector4(0, -1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 0, 0, 1)
        );
        Matrix4x4 Tinv = T.inverse;
        Matrix4x4 R_ros = Matrix4x4.Rotate(rosRot);
        Matrix4x4 R_u = Tinv * R_ros * T;
        return R_u.rotation;
    }
}
