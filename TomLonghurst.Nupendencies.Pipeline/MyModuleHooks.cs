using Microsoft.Extensions.Logging;
using ModularPipelines.Context;
using ModularPipelines.Interfaces;
using ModularPipelines.Modules;

namespace TomLonghurst.Nupendencies.Pipeline;

public class MyModuleHooks : IPipelineModuleHooks
{
    public Task OnBeforeModuleStartAsync(IPipelineContext moduleContext, ModuleBase module)
    {
        moduleContext.Logger.LogInformation("{Module} is starting at {DateTime}", module.GetType().Name, DateTimeOffset.UtcNow);
        return Task.CompletedTask;
    }

    public Task OnBeforeModuleEndAsync(IPipelineContext moduleContext, ModuleBase module)
    {
        moduleContext.Logger.LogInformation("{Module} finished at {DateTime} after {Elapsed}", module.GetType().Name, DateTimeOffset.UtcNow, module.Duration);
        return Task.CompletedTask;
    }
}
