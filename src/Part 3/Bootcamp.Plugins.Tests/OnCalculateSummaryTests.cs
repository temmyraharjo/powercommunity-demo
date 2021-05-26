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
