namespace BimosVrInjector.Core.Abstractions
{
    public interface IRigSpawner
    {
        void Spawn(float[] pos, float[] rotEuler, float[] scale);

        void DespawnExisting();
    }
}
