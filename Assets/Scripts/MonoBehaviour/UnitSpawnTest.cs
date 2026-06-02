using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// 测试用单位生成器：配置数量与范围，进入 Play 后一次性在 SubScene 中烘焙并生成 Entity。
/// 用法：1. 将场景中的 Unit 拖成 Prefab；2. 在本组件上指定 Prefab、数量、范围；3. 组件需放在 EntitiesSubScene 内。
/// </summary>
public class UnitSpawnTest : MonoBehaviour
{
    [Tooltip("带 UnitMoverAuthoring 的单位 Prefab（须为 Project 中的 Prefab 资源）")]
    public GameObject unitPrefab;

    [Tooltip("生成数量")]
    public int spawnCount = 100;

    [Tooltip("生成范围（以本物体位置为中心，XZ 平面半边长）")]
    public float spawnRange = 15f;

    public class Baker : Baker<UnitSpawnTest>
    {
        public override void Bake(UnitSpawnTest authoring)
        {
            if (authoring.unitPrefab == null)
                return;

            DependsOn(authoring.unitPrefab);

            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new UnitSpawnConfig
            {
                UnitPrefab = GetEntity(authoring.unitPrefab, TransformUsageFlags.Dynamic),
                SpawnCount = math.max(0, authoring.spawnCount),
                SpawnRange = math.max(0f, authoring.spawnRange),
                SpawnOrigin = authoring.transform.position,
            });
        }
    }
}

/// <summary>烘焙后的生成配置，由 <see cref="UnitSpawnTestSystem"/> 在首帧读取。</summary>
public struct UnitSpawnConfig : IComponentData
{
    public Entity UnitPrefab;
    public int SpawnCount;
    public float SpawnRange;
    public float3 SpawnOrigin;
}

/// <summary>首帧根据 UnitSpawnConfig 实例化单位，完成后自动禁用。</summary>
public partial struct UnitSpawnTestSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<UnitSpawnConfig>();
    }

    public void OnUpdate(ref SystemState state)
    {
        UnitSpawnConfig config = SystemAPI.GetSingleton<UnitSpawnConfig>();
        if (config.UnitPrefab == Entity.Null || config.SpawnCount <= 0)
        {
            state.Enabled = false;
            return;
        }

        EntityManager entityManager = state.EntityManager;
        var random = Unity.Mathematics.Random.CreateFromIndex(0x52545321u);

        for (int i = 0; i < config.SpawnCount; i++)
        {
            float offsetX = random.NextFloat(-config.SpawnRange, config.SpawnRange);
            float offsetZ = random.NextFloat(-config.SpawnRange, config.SpawnRange);
            float3 position = config.SpawnOrigin + new float3(offsetX, 0f, offsetZ);

            Entity instance = entityManager.Instantiate(config.UnitPrefab);

            LocalTransform localTransform = entityManager.GetComponentData<LocalTransform>(instance);
            localTransform.Position = position;
            entityManager.SetComponentData(instance, localTransform);

            if (entityManager.HasComponent<UnitMover>(instance))
            {
                UnitMover unitMover = entityManager.GetComponentData<UnitMover>(instance);
                unitMover.targetPosition = position;
                entityManager.SetComponentData(instance, unitMover);
            }
        }

        state.Enabled = false;
    }
}
