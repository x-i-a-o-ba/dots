using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 单位移动系统：每帧驱动带 <see cref="UnitMover"/> 的物理实体朝目标点移动。
/// 使用 ISystem + SystemAPI.Query，逻辑在 Simulation 阶段与 Unity Physics 协同。
/// </summary>
partial struct UnitMoveSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //创建一个Job 并调度
        UnitMoveJob job = new UnitMoveJob { deltaTime = SystemAPI.Time.DeltaTime };

        //系统会根据实体数量和Job的大小来决定是否并行执行，如果实体数量很多，或者Job很大，系统会自动并行执行，调用多个线程执行Job，
        // 如果实体数量很少，或者Job很小，系统会自动串行执行，调用一个线程执行Job，
        // 如果实体数量和Job的大小适中，系统会自动并行执行，调用多个线程执行Job，

        job.ScheduleParallel();

        /*
        // 查询同时持有 LocalTransform、UnitMover、PhysicsVelocity 的实体。
        // RefRW = 可读写，RefRO = 只读；PhysicsVelocity 由物理系统每帧积分。
        foreach (
            (
                RefRW<LocalTransform> localTransform,
                RefRO<UnitMover> unitMover,
                RefRW<PhysicsVelocity> physicsVelocity
            ) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<UnitMover>, RefRW<PhysicsVelocity>>()
        )
        {
            float3 targetPosition = unitMover.ValueRO.targetPosition;
            float3 displacement = targetPosition - localTransform.ValueRO.Position;

            // RTS 平面移动：忽略 Y 轴位移，只在 XZ 平面朝向目标。
            displacement.y = 0;
            float3 direction = math.normalize(displacement);

            // 平滑旋转朝向移动方向（Slerp 插值，速度由 UnitMover.rotationSpeed 控制）。
            quaternion targetRotation = quaternion.LookRotation(direction, math.up());
            localTransform.ValueRW.Rotation = math.slerp(
                localTransform.ValueRO.Rotation,
                targetRotation,
                SystemAPI.Time.DeltaTime * unitMover.ValueRO.rotationSpeed
            );

            // 通过设置线速度驱动 RigidBody 移动；Angular 清零避免物理旋转干扰。
            // 注意：Random 为托管 API，若启用 Burst 严格编译可能需改为 Unity.Mathematics.Random。
            physicsVelocity.ValueRW.Linear = direction * unitMover.ValueRO.moveSpeed;
            physicsVelocity.ValueRW.Linear.y = 0;
            physicsVelocity.ValueRW.Angular = float3.zero;
        }
        */
    }
}

[BurstCompile]
public partial struct UnitMoveJob : IJobEntity
{
    public float deltaTime;

    public void Execute(
        ref LocalTransform localTransform,
        in UnitMover unitMover,
        ref PhysicsVelocity physicsVelocity
    )
    {
        float3 targetPosition = unitMover.targetPosition;
        float3 displacement = targetPosition - localTransform.Position;
        displacement.y = 0;
        if (math.lengthsq(displacement)<= 1f)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }
        
        float3 direction = math.normalize(displacement);
        quaternion targetRotation = quaternion.LookRotation(direction, math.up());
        localTransform.Rotation = math.slerp(
            localTransform.Rotation,
            targetRotation,
            deltaTime * unitMover.rotationSpeed
        );
        physicsVelocity.Linear = direction * unitMover.moveSpeed;
       // physicsVelocity.Linear.y = 0;
        physicsVelocity.Angular = float3.zero;
    }
}
