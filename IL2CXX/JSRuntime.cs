using System.Collections.Concurrent;
using System.Threading;

namespace IL2CXX;

public sealed class JSSynchronizationContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback Delegate, object? State)> queue = [];

    public override void Post(SendOrPostCallback d, object? state)
    {
        queue.Add((d, state));
        Notify();
    }

    public static void Install() => SetSynchronizationContext(new JSSynchronizationContext());
    public static void Notify() => throw new NotImplementedException();
    public static void Pump()
    {
        if (Current is JSSynchronizationContext jssc) while (jssc.queue.TryTake(out var item)) item.Delegate(item.State);
    }
}
