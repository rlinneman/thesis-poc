using Microsoft.Practices.Unity;
using Rel.Data;
using Rel.Data.Bulk;
using Rel.Data.Ef6;
using Rel.Merge;
using System.Web.Http;
using Unity.WebApi;

namespace ThesisPortal
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();

            // register all your components with the container here
            // it is NOT necessary to register your controllers
            container.RegisterType(typeof(IDataContext), typeof(TpContext), new HierarchicalLifetimeManager());
            container.RegisterType(typeof(IConflictResolver), typeof(MergeConcurrentEditsConflictResolver), new HierarchicalLifetimeManager());
            container.RegisterType(typeof(IMergeProvider), typeof(MergeOperation), new HierarchicalLifetimeManager());

            // e.g. container.RegisterType<ITestService, TestService>();

            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);
        }
    }
}