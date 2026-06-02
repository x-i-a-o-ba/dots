using System;
using Common;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// MonoBehaviour ↔ ECS 桥接：在主线程读取输入，向所有带 UnitMover 的 Entity 写入目标位置。
/// DOTS 中 System 不宜直接读 Input，常见做法是由 MonoBehaviour 写 Component，System 读 Component。
/// </summary>
public class UnitSelectionManager : MonoSingleton<UnitSelectionManager>
{

    public UnityAction<Vector2> OnSelectionStart;
    public UnityAction<Vector2> OnSelectionEnd;

    public Vector2 selectStartPosition;
    public Vector2 selectEndPosition;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectStartPosition = Input.mousePosition;
            OnSelectionStart?.Invoke(selectStartPosition);
        }
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 selectEndPosition = Input.mousePosition;

            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityQuery entityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, Unit>()
                .Build(entityManager);
            NativeArray<Entity> entitieyArray = entityQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entitieyArray.Length; i++)
            {
                entityManager.SetComponentEnabled<Selected>(entitieyArray[i], false);
            }


            entityQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<LocalTransform, Unit>()
                .WithPresent<Selected>()
                .Build(entityManager);
            Rect selectionAreaRect = GetSlectionAreaRect();

          entitieyArray = entityQuery.ToEntityArray(Allocator.Temp);
            NativeArray<LocalTransform> localTransformArray =
                entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            for (int i = 0; i < entitieyArray.Length; i++)
            {
                LocalTransform unitLocalTransform = localTransformArray[i];
                Vector2 unitScreenPosition = Camera.main.WorldToScreenPoint(
                    unitLocalTransform.Position
                );
                if (selectionAreaRect.Contains(unitScreenPosition))
                {
                    entityManager.SetComponentEnabled<Selected>(entitieyArray[i], true);
                }
            }
            entityQuery.CopyFromComponentDataArray(localTransformArray);

            OnSelectionEnd?.Invoke(selectEndPosition);
        }
        if (Input.GetMouseButtonDown(1))
        {
            Vector3 mousePosition = MouseWorldPositon.Instance.GetPosition();

            // DefaultGameObjectInjectionWorld：默认 ECS World，SubScene 烘焙出的实体在此运行。
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // EntityQueryBuilder：声明查询条件（此处为拥有 UnitMover 的所有实体）。
            // Allocator.Temp 分配的 Native 容器须在本帧内 Dispose，此处 using 隐式释放。
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<UnitMover, Selected>()
                .Build(entityManager);

            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
            NativeArray<UnitMover> unitMovers = query.ToComponentDataArray<UnitMover>(
                Allocator.Temp
            );

            for (int i = 0; i < entities.Length; i++)
            {
                UnitMover unitMover = unitMovers[i];
                unitMover.targetPosition = mousePosition;
                unitMovers[i] = unitMover;
            }

            // 将修改后的组件数组批量写回 Query 匹配的 Entity（等价于逐 Entity SetComponentData）。
            query.CopyFromComponentDataArray(unitMovers);
        }
    }

    public Rect GetSlectionAreaRect()
    {
        Vector2 selectionEndMousePosition = Input.mousePosition;

        Vector2 lowerLeft = new Vector2(
            Mathf.Min(selectStartPosition.x, selectionEndMousePosition.x),
            Mathf.Min(selectStartPosition.y, selectionEndMousePosition.y)
        );
        Vector2 upperRight = new Vector2(
            Mathf.Max(selectStartPosition.x, selectionEndMousePosition.x),
            Mathf.Max(selectStartPosition.y, selectionEndMousePosition.y)
        );
        return new Rect(
            lowerLeft.x,
            lowerLeft.y,
            upperRight.x - lowerLeft.x,
            upperRight.y - lowerLeft.y
        );
    }
}
