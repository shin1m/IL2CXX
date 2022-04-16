using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DemoRenderer
{
    public abstract class Disposable : IAsyncDisposable
    {
#if DEBUG
        ~Disposable()
        {
            Debug.Fail($"An object of type {GetType()} was not disposed prior to finalization.");
        }
#endif
        protected abstract ValueTask DoDisposeAsync();
        public async ValueTask DisposeAsync()
        {
            await DoDisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
