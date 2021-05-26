using Bootcamp.Plugins.Business;
using Demo.Entities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Niam.XRM.Framework.Interfaces.Plugin;
using Niam.XRM.Framework.Interfaces.Plugin.Configurations;
using Niam.XRM.Framework.Plugin;

namespace Bootcamp.Plugins
{
    public class PreOrderCreate : PluginBase<new_order>, IPlugin
    {
        public PreOrderCreate(string unsecure, string secure) : base(unsecure, secure)
        {
        }

        protected override void Configure(IPluginConfiguration<new_order> config)
        {
            config.ColumnSet = new ColumnSet(true);
        }

        protected override void ExecuteCrmPlugin(IPluginContext<new_order> context)
        {
            new OnSetOrderNumber(context).Execute();
        }
    }
}