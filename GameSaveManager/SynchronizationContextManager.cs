using System.Threading;

namespace GameSaveManager
{
    public static class SynchronizationContextManager
    {
        public static SynchronizationContext Context { get; private set; }

        public static void Initialize()
        {
            if (Context == null)
            {
                Context = SynchronizationContext.Current;
            }
        }
    }
}
