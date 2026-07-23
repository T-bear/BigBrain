namespace BigBrain.Sentinel;

public interface ICapabilityRegistry
{
    int Count { get; }
}

public sealed class EmptyCapabilityRegistry : ICapabilityRegistry
{
    public int Count => 0;
}
