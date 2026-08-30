using Microsoft.Extensions.DependencyInjection;

using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;

namespace CurrencyModule;

/// <summary>
/// Registers what PlayerCurrencyService needs from the Cloud Code host. Same shape as the
/// ModuleSetup in the existing Blocks modules.
/// </summary>
public class ModuleSetup : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.Dependencies.AddSingleton(GameApiClient.Create());
    }
}
