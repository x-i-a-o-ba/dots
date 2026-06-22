using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[UpdateBefore(typeof(ResetEventsSystem))]
partial struct SelectedVisualSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>().WithPresent<Selected>())
        {
            RefRW<LocalTransform> visualTransform = SystemAPI.GetComponentRW<LocalTransform>(
                selected.ValueRO.visualEntity
            );

            if (selected.ValueRO.onSelected)
            {
                visualTransform.ValueRW.Scale = selected.ValueRO.showScale;
            }
            else
            {
                visualTransform.ValueRW.Scale = 0f;
            }

        }

        //旧代码 高消耗检测
        // foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>().WithDisabled<Selected>())
        // {
        //     RefRW<LocalTransform> visualTransform = SystemAPI.GetComponentRW<LocalTransform>(
        //         selected.ValueRO.visualEntity
        //     );
        //     visualTransform.ValueRW.Scale = 0f;
        // }

        // foreach (RefRO<Selected> selected in SystemAPI.Query<RefRO<Selected>>())
        // {
        //     RefRW<LocalTransform> visualTransform = SystemAPI.GetComponentRW<LocalTransform>(
        //         selected.ValueRO.visualEntity
        //     );
        //     visualTransform.ValueRW.Scale = selected.ValueRO.showScale;
        // }
    }
}
