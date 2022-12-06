using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct Boid : IComponentData
{
    public LocalToWorldTransform target;
}