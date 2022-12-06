using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

public class BoidAuthoring : MonoBehaviour
{
}

class BoidBaker : Baker<BoidAuthoring>
{
    public override void Bake(BoidAuthoring authoring)
    {
        AddComponent(new Boid());
    }
}