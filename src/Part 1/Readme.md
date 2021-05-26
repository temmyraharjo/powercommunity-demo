# User Story #1: When Create Custom Order, Set Order Number = AutoNumber
  When Creating Custom Order:  
  
Find Autonumber with Filter: Name = Customer.Name and Year = Transaction Date.Year, Month = Transaction Date.Month. 
* If not found, create Autonumber with Index 1 and Set Order Number = {CustName}{Year}{Month}00001. 
* If found, update Autonumber with Index = Index + 1 and Set Order Number = {CustName}{Year}{Month}{Index} (PadLeft: 5).


# Preparing Plugin Project

1. [Install PCF from this link.](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/powerapps-cli)
1. If already install, run "pac install latest".
1. Setup the Folder Path for your plugin project, name it as "Bootcamp.Plugins".
1. Open Developer Command Prompt > Change Directory to your Folder > Run Command "pac plugin init"
1. Open the .csproj > On the Plugin Project, Right Click > Manage Nuget Packages > Browse Niam.Xrm.Framework and Install it.

# Preparing Business Logic

1. Add existing file > Resources\Entities.cs to load early bound entities.
1. Delete Generated Files (Plugin1.cs and PluginBase.cs)
1. Create Business Folder (For holding all our Business/Logic Classes) > Create new OnSetOrderNumber.cs

```csharp
using System;
using System.Linq;
using Demo.Entities;
using Microsoft.Xrm.Sdk.Query;
using Niam.XRM.Framework;
using Niam.XRM.Framework.Data;
using Niam.XRM.Framework.Interfaces.Plugin;
using Niam.XRM.Framework.Plugin;

namespace Bootcamp.Plugins.Business
{
    public class OnSetOrderNumber : OperationBase<new_order>
    {
        public OnSetOrderNumber(ITransactionContext<new_order> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            var customerRef = Get(e => e.new_customerid);
            var transactionDate = Get(e => e.new_date);

            var valid = customerRef != null && transactionDate.HasValue;
            if (!valid) return;

            var customerName = customerRef.LogicalName == Account.EntityLogicalName
                ? Service.GetReferenceName<Account>(customerRef)
                : Service.GetReferenceName<Contact>(customerRef);
            var month = transactionDate.Value.Month;
            var year = transactionDate.Value.Year;

            var autonumber = GetAutonumber(customerName, year, month);
            var index = autonumber.GetValue(e => e.new_index) + 1;

            var format = $"{customerName}/{year}/{month}/{index.ToString().PadLeft(5, '0')}";
            Set(e => e.new_ordernumber, format);

            UpsertAutonumber(autonumber, index);
        }

        private void UpsertAutonumber(new_autonumber autonumber, int index)
        {
            if (autonumber.Id == Guid.Empty)
            {
                Service.Create(autonumber);
                return;
            }

            var updated = new new_autonumber { Id = autonumber.Id }.Set(e => e.new_index, index);
            Service.Update(updated);
        }

        private new_autonumber GetAutonumber(string customerName, int year, int month)
        {
            var query = new QueryExpression(new_autonumber.EntityLogicalName)
            {
                ColumnSet = new ColumnSet<new_autonumber>(e => e.new_index),
                TopCount = 1
            };
            query.Criteria.AddCondition<new_autonumber>(e => e.new_name, ConditionOperator.Equal, customerName);
            query.Criteria.AddCondition<new_autonumber>(e => e.new_year, ConditionOperator.Equal, year);
            query.Criteria.AddCondition<new_autonumber>(e => e.new_month, ConditionOperator.Equal, month);
            query.Criteria.AddCondition<new_autonumber>(e => e.statecode, ConditionOperator.Equal,
                (int)new_autonumber.Options.statecode.Active);

            var result = Service.RetrieveMultiple(query);

            return result.Entities.Any() ? result.Entities[0].ToEntity<new_autonumber>() :
                new new_autonumber()
                    .Set(e => e.new_name, customerName)
                    .Set(e => e.new_month, month)
                    .Set(e => e.new_year, year)
                    .Set(e => e.new_index, 0);
        }
    }
}
```

# Preparing PreOrderCreate Plugin Step
On the root of the Bootcamp.Plugins project, create new file > PreOrderCreate.cs

```csharp
using System;
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
```

# Deployment
Before you can deploy the result. Because the plugin got dependency to Niam.Xrm.Framework.dll, hence we need to merge the assemblies (Using ILRepack).  
  
First, you need to build your project then copy all the files into Resources/Merge folder + Bootcamp.Plugins.snk (the .snk File for signing purpose).

Inside the folder, you will find Build.bat with code:
```plaintext
SET CurrentDir=%~dp0

ILRepack.exe /keyfile:%CurrentDir%Bootcamp.Plugins.snk /parallel /out:%CurrentDir%Result/Bootcamp.Plugins.dll %CurrentDir%Bootcamp.Plugins.dll %CurrentDir%Niam.XRM.Framework.dll
```

After finished, you can take the result on Resources/Merge/Result/Bootcamp.Plugins.dll folder and deploy it into your CRM Environment.

Register New Assembly using  "Resources/Merge/Result/Bootcamp.Plugins.dll" > Sandbox > Database > Register Selected Plugins.

Select the new Assembly registered > right click > Register new step > Fill in below details:
![Register New Plugin Step](../../Resources/Images/Register%20new%20plugin%20step.png)

