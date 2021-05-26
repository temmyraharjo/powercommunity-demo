# Creating Unit Tests For The Existing Function

On the Solution Project, Right Click > Add New Project > Test > Unit Test Project (.NET Framework).
Choose the .NET Framework 4.6.2 > Name: Bootcamp.Plugins.Tests > Location in the same level as Bootcamp.Plugins folder > Ok.

![Create Unit Test Project](../../Resources/Images/Create%20Unit%20Test%20Project.png)

Right Click on the Bootcamp.Plugins.Tests project > Manage Nuget Packages > Add Niam.Xrm.Framework.TestHelper > Install.

On the References > Right Click > Add Reference > Projects > Choose your Bootcamp.Plugin Project.

Remove the generated UnitTest1.cs

Add new class > Name it as OnSetOrderNumberTests.cs. Here is our first unit testing:

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
    public class OnSetOrderNumberTests
    {
        [TestMethod]
        public void OnSetOrderNumber_Autonumber_Created()
        {
            var account = new Account { Id = Guid.NewGuid() }
                .Set(e => e.Name, "Temmy Wahyu Raharjo");
            var date = DateTime.Now;

            var target = new new_order { Id = Guid.NewGuid() }
                .Set(e => e.new_customerid, account.ToEntityReference())
                .Set(e => e.new_date, date);

            var testContext = new TestEvent<new_order>(account);
            testContext.CreateEventCommand<OnSetOrderNumber>(target);

            var orderNumber = target.Get(e => e.new_ordernumber);
            Assert.IsNotNull(orderNumber);
            Assert.AreEqual($"Temmy Wahyu Raharjo/{date.Year}/{date.Month}/00001", orderNumber);

            var autonumber = testContext.Db.Event.Created[0].ToEntity<new_autonumber>();
            Assert.AreNotEqual(Guid.Empty, autonumber.Id);
            Assert.AreEqual(date.Year, autonumber.GetValue(e => e.new_year));
            Assert.AreEqual(date.Month, autonumber.GetValue(e => e.new_month));
            Assert.AreEqual(1, autonumber.GetValue(e => e.new_index));
        }

        [TestMethod]
        public void OnSetOrderNumber_Autonumber_Updated()
        {
            var account = new Account { Id = Guid.NewGuid() }
                .Set(e => e.Name, "Temmy Wahyu Raharjo");
            var date = DateTime.Now;

            var target = new new_order { Id = Guid.NewGuid() }
                .Set(e => e.new_customerid, account.ToEntityReference())
                .Set(e => e.new_date, date);

            var originalAutonumber = new new_autonumber { Id = Guid.NewGuid() }
                .Set(e => e.new_name, "Temmy Wahyu Raharjo")
                .Set(e => e.new_year, date.Year)
                .Set(e => e.new_month, date.Month)
                .Set(e => e.new_index, 99);


            var testContext = new TestEvent<new_order>(account, originalAutonumber);
            testContext.CreateEventCommand<OnSetOrderNumber>(target);

            var orderNumber = target.Get(e => e.new_ordernumber);
            Assert.IsNotNull(orderNumber);
            Assert.AreEqual($"Temmy Wahyu Raharjo/{date.Year}/{date.Month}/00100", orderNumber);

            var autonumber = testContext.Db.Event.Updated[0].ToEntity<new_autonumber>();
            Assert.AreEqual(originalAutonumber.Id, autonumber.Id);
            Assert.AreEqual(100, autonumber.GetValue(e => e.new_index));
        }
    }
}
```

If you run the test, then you will get error. Because when applying the testing, We found that the index of the autonumber not being fixed. Hence we need to fix the implementation code:
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
            autonumber.Set(e => e.new_index, index);

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

If you running again, the two tests will passed.


# Refactoring
Instead of using Service.Create or Service.Update, why not we just use UpsertRequest? If you know the behavior of the UpsertRequest, then you can change the Unit Tests like below code:

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
    public class OnSetOrderNumberTests
    {
        [TestMethod]
        public void OnSetOrderNumber_Autonumber_Created()
        {
            var account = new Account { Id = Guid.NewGuid() }
                .Set(e => e.Name, "Temmy Wahyu Raharjo");
            var date = DateTime.Now;

            var target = new new_order { Id = Guid.NewGuid() }
                .Set(e => e.new_customerid, account.ToEntityReference())
                .Set(e => e.new_date, date);

            var testContext = new TestEvent<new_order>(account, 
                new new_autonumber { Id = Guid.NewGuid() });
            testContext.CreateEventCommand<OnSetOrderNumber>(target);

            var orderNumber = target.Get(e => e.new_ordernumber);
            Assert.IsNotNull(orderNumber);
            Assert.AreEqual($"Temmy Wahyu Raharjo/{date.Year}/{date.Month}/00001", orderNumber);

            var autonumber = testContext.Db.Event.Created[0].ToEntity<new_autonumber>();
            Assert.AreNotEqual(Guid.Empty, autonumber.Id);
            Assert.AreEqual(date.Year, autonumber.GetValue(e => e.new_year));
            Assert.AreEqual(date.Month, autonumber.GetValue(e => e.new_month));
            Assert.AreEqual(1, autonumber.GetValue(e => e.new_index));
        }

        [TestMethod]
        public void OnSetOrderNumber_Autonumber_Updated()
        {
            var account = new Account { Id = Guid.NewGuid() }
                .Set(e => e.Name, "Temmy Wahyu Raharjo");
            var date = DateTime.Now;

            var target = new new_order { Id = Guid.NewGuid() }
                .Set(e => e.new_customerid, account.ToEntityReference())
                .Set(e => e.new_date, date);

            var originalAutonumber = new new_autonumber { Id = Guid.NewGuid() }
                .Set(e => e.new_name, "Temmy Wahyu Raharjo")
                .Set(e => e.new_year, date.Year)
                .Set(e => e.new_month, date.Month)
                .Set(e => e.new_index, 99);


            var testContext = new TestEvent<new_order>(account, originalAutonumber);
            testContext.CreateEventCommand<OnSetOrderNumber>(target);

            var orderNumber = target.Get(e => e.new_ordernumber);
            Assert.IsNotNull(orderNumber);
            Assert.AreEqual($"Temmy Wahyu Raharjo/{date.Year}/{date.Month}/00100", orderNumber);

            var autonumber = testContext.Db.Event.Updated[0].ToEntity<new_autonumber>();
            Assert.AreEqual(originalAutonumber.Id, autonumber.Id);
            Assert.AreEqual(100, autonumber.GetValue(e => e.new_index));
        }
    }
}
```

Then the business logic changed to:

```csharp
using Demo.Entities;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Niam.XRM.Framework;
using Niam.XRM.Framework.Data;
using Niam.XRM.Framework.Interfaces.Plugin;
using Niam.XRM.Framework.Plugin;
using System.Linq;

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
            autonumber.Set(e => e.new_index, index);

            var format = $"{customerName}/{year}/{month}/{index.ToString().PadLeft(5, '0')}";
            Set(e => e.new_ordernumber, format);

            var alternateKeys = new KeyAttributeCollection
            {
                {Helper.Name<new_autonumber>(e => e.new_name), customerName},
                {Helper.Name<new_autonumber>(e => e.new_year), year},
                {Helper.Name<new_autonumber>(e => e.new_month), month}
            };
            autonumber.KeyAttributes = alternateKeys;

            Service.Execute(new UpsertRequest { Target = autonumber.ToEntity<Entity>() });
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

