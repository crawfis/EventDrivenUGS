using System;

using Unity.Services.Core;

namespace CrawfisSoftware.UGS
{
    internal static class ServiceObserverHelpers
    {

        public static void CleanupServiceObserver<T>(ref ServiceObserver<T> serviceObserver)
        {
            if (serviceObserver != null)
            {
                serviceObserver.Dispose();
                serviceObserver = null;
            }
        }

        // Todo: This pattern is repeated in the Blocks, which is behind an Assembly Definition.
        public static ServiceObserver<T> InitializeServiceObserver<T>(Action<T> onInitialized)
        {
            var serviceObserver = new ServiceObserver<T>();
            if (serviceObserver.Service == null)
            {
                var observer = serviceObserver;
                Action<T> initAndDeregister = null;
                initAndDeregister = _ =>
                {
                    onInitialized(observer.Service);
                    observer.Initialized -= initAndDeregister;
                };
                serviceObserver.Initialized += initAndDeregister;
            }
            else
            {
                onInitialized(serviceObserver.Service);
            }

            return serviceObserver;
        }
    }
}