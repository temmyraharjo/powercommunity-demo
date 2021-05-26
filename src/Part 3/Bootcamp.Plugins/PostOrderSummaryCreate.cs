using Bootcamp.Plugins.Business;
using Demo.Entities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Niam.XRM.Framework.Interfaces.Plugin;
using Niam.XRM.Framework.Interfaces.Plugin.Configurations;
using Niam.XRM.Framework.Plugin;

namespace Bootcamp.Plugins
{
    public class PostOrderSummaryCreate : PluginBase<new_ordersummary>, IPlugin
    {
        public PostOrderSummaryCreate(string unsecure, string secure) : base(unsecure, secure)
        {
        }

        protected override void Configure(IPluginConfiguration<new_ordersummary> config)
        {
            config.ColumnSet = new ColumnSet(true);
        }

        protected override void ExecuteCrmPlugin(IPluginContext<new_ordersummary> context)
        {
            new OnCalculateSummary(context).Execute();
        }
    }
}