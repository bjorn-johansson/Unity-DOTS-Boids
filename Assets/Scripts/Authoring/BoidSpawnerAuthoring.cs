using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class BoidSpawnerAuthoring : MonoBehaviour
{
    public Transform boidPrefab;
}

class BoidSpawnerBaker : Baker<BoidSpawnerAuthoring>
{
    public override void Bake(BoidSpawnerAuthoring authoring)
    {
        AddComponent(new BoidSpawner
        {
            boidPrefab = GetEntity(authoring.boidPrefab),
        });
    }
}