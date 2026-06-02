using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Authoring 组件：挂在 SubScene / 预制体上，Baking 时转换为 ECS 组件。
/// Baker 在烘焙阶段运行（非运行时），将 MonoBehaviour 数据写入 Entity。
/// </summary>
public class UnitMoverAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public float rotationSpeed;

    public class Baker : Baker<UnitMoverAuthoring>
    {
        public override void Bake(UnitMoverAuthoring authoring)
        {
            // TransformUsageFlags.Dynamic：实体位置/旋转由 ECS 或物理系统驱动。
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new UnitMover
            {
                moveSpeed = authoring.moveSpeed,
                rotationSpeed = authoring.rotationSpeed,
            });
        }
    }
}

/// <summary>
/// 单位移动运行时数据。IComponentData 表示纯值类型组件，存在 Archetype Chunk 中。
/// targetPosition 由主线程 MonoBehaviour（如 UnitSelectionManager）写入，System 只读并驱动移动。
/// </summary>
public struct UnitMover : IComponentData
{
    public float moveSpeed;
    public float rotationSpeed;

    /// <summary>世界空间目标点，右键选点后由外部系统批量更新。</summary>
    public float3 targetPosition;
}
