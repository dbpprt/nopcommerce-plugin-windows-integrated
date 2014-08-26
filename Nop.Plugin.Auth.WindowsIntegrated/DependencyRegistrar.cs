using Autofac;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Services.Authentication;

namespace Nop.Plugin.Auth.WindowsIntegrated
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public void Register(ContainerBuilder builder, ITypeFinder typeFinder)
        {
            builder.RegisterType<WindowsIntegratedAuthenticationService>()
                .As<IAuthenticationService>()
                .InstancePerLifetimeScope();
        }

        public int Order { get { return 47; } }
    }
}
