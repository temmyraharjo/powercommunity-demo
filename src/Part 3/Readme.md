# User Story #2: When Create Custom Order Summary, Set Qty + Total Amount With Following Criteria
  When Creating Custom Order:  
  
Set Order Summary:
- Set Order Summary.TotalQty = Sum OrderDetail.Qty 
- Set Order Summary.Amount = Sum OrderDetail.Qty * OrderDetail.PricePerUnit 

With conditions:
- Order.Customer = Order Summary.Customer
- Order.Month + Year = Order Summary.Month + Year

Above Business Logic will need to be run on PostOperation.

# Business Logic

```csharp
using Demo.Entities;
using Microsoft.Xrm.Sdk.Query;
using Niam.XRM.Framework;
using Niam.XRM.Framework.Data;
using Niam.XRM.Framework.Interfaces.Plugin;
using Niam.XRM.Framework.Plugin;
using System;
using System.Linq;

namespace Bootcamp.Plugins.Business
{
    public class OnCalculateSummary : OperationBase<new_ordersummary>
    {
        public OnCalculateSummary(ITransactionContext<new_ordersummary> context) : base(context)
        {
        }

        protected override void HandleExecute()
        {
            var month = Get(e => e.new_month);
            var year = Get(e => e.new_year);
            var customerRef = Get(e => e.new_customerid);

            var valid = month != null && year != null && customerRef != null;
            if (!valid) return;

            var orders = GetOrders(customerRef.Id, year.Value, month.Value);
            var orderDetails = orders.Select(order => order.GetAliasedEntity<new_orderdetail>("od")).ToArray();
            var totalQty = orderDetails.Sum(od => od.GetValue(e => e.new_qty));
            var totalAmount = orderDetails.Sum(od => od.GetValue(e => e.new_qty) * od.GetValue(e => e.new_price));

            var updated = new new_ordersummary { Id = Wrapper.Id }
                .Set(e => e.new_amount, totalAmount)
                .Set(e => e.new_totalqty, totalQty);

            Service.Update(updated);
        }

        private new_order[] GetOrders(Guid customerId, int year, int month)
        {
            var fromDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDate = DateTime.DaysInMonth(year, month);
            var toDate = new DateTime(year, month, lastDate, 23, 59, 59, DateTimeKind.Utc);

            var query = new QueryExpression(new_order.EntityLogicalName)
            {
                ColumnSet = new ColumnSet<new_order>(e => e.new_date, e => e.new_customerid),
                NoLock = true
            };
            query.Criteria.AddCondition<new_order>(e => e.new_customerid, ConditionOperator.Equal, customerId);
            query.Criteria.AddCondition<new_order>(e => e.new_date, ConditionOperator.Between, fromDate, toDate);
            query.Criteria.AddCondition<new_order>(e => e.new_status, ConditionOperator.Equal,
                (int)new_order.Options.new_status.Finished);

            var orderDetailLink = query.AddLink(new_orderdetail.EntityLogicalName, Helper.Name<new_order>(e => e.Id),
                Helper.Name<new_orderdetail>(e => e.new_orderid));
            orderDetailLink.EntityAlias = "od";
            orderDetailLink.Columns = new ColumnSet<new_orderdetail>(e => e.new_price, e => e.new_qty);

            var result = Service.RetrieveMultiple(query);

            return result.Entities.Any()
                ? result.Entities.Select(e => e.ToEntity<new_order>()).ToArray()
                : new new_order[] { };
        }
    }
}
```

# Unit Test

```csharp
using Bootcamp.Plugins.Business;
using Demo.Entities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Niam.XRM.Framework;
using Niam.XRM.Framework.TestHelper;
using System;

namespace Bootcamp.Plugins.Tests
{
    [TestClass]
    public class OnCalculateSummaryTests
    {
        [TestMethod]
        public void OnCalculateSummary_ShouldValid()
        {
            var customerRef = new Account { Id = Guid.NewGuid() }.ToEntityReference();
            var transactionDate = DateTime.Now;

            var order = new new_order { Id = Guid.NewGuid() }
                .Set(e => e.new_customerid, customerRef)
                .Set(e => e.new_date, transactionDate)
                .Set(e => e.new_status, new_order.Options.new_status.Finished);

            var orderDetail1 = new new_orderdetail { Id = Guid.NewGuid() }
                .Set(e => e.new_orderid, order.ToEntityReference())
                .Set(e => e.new_qty, 10)
                .Set(e => e.new_price, 100);

            var orderDetail2 = new new_orderdetail { Id = Guid.NewGuid() }
                .Set(e => e.new_orderid, order.ToEntityReference())
                .Set(e => e.new_qty, 10)
                .Set(e => e.new_price, 50);

            var target = new new_ordersummary { Id = Guid.NewGuid() }
                .Set(e => e.new_customerid, customerRef)
                .Set(e => e.new_month, transactionDate.Month)
                .Set(e => e.new_year, transactionDate.Year);

            var testContext = new TestEvent<new_ordersummary>(target, order, orderDetail1, orderDetail2);
            testContext.CreateEventCommand<OnCalculateSummary>(target);

            var updated = testContext.Db.Event.Updated[0].ToEntity<new_ordersummary>();
            Assert.AreEqual(updated.GetValue(e => e.new_totalqty), 20);
            Assert.AreEqual(updated.GetValue(e => e.new_amount), 1500);
        }
    }
}
```

# Create PostOrderSummaryCreate

```csharp
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
```


# Merge + Update Assembly + Register New Plugin Step
After the above code done, you can merge it + update the assembly.  
  
Then you can register the new plugin step with this value:

![Order Summary Plugin Step](../../Resources/Images/Order%20Summary%20Plugin%20Step.png)


