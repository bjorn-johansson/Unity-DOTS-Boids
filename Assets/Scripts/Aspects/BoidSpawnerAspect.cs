using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

readonly partial struct BoidSpawnerAspect : IAspect
{
    readonly RefRO<BoidSpawner> m_boidSpawner;

    public Entity boidPrefab => m_boidSpawner.ValueRO.boidPrefab;
}