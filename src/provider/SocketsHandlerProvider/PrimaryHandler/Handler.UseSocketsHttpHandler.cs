using System;
using System.Net.Http;
using PrimeFuncPack;

namespace GarageGroup.Infra;

partial class PrimaryHandler
{
    public static Dependency<SocketsHttpHandler> UseSocketsHttpHandler(
        this Dependency<ISocketsHttpHandlerProvider, ISocketsHttpHandlerConfiguration> dependency)
        =>
        InnerUseSocketsHttpHandler(
            dependency ?? throw new ArgumentNullException(nameof(dependency)));

    public static Dependency<SocketsHttpHandler> UseSocketsHttpHandler(
        Func<IServiceProvider, ISocketsHttpHandlerConfiguration> configurationResolver)
        =>
        InnerUseSocketsHttpHandler(
            configurationResolver ?? throw new ArgumentNullException(nameof(configurationResolver)));

    public static Dependency<SocketsHttpHandler> UseStandardSocketsHttpHandler()
        =>
        InnerUseSocketsHttpHandler(
            static sp => sp.GetSocketsHttpHandlerConfigurationFromEnvironment(string.Empty));
}
