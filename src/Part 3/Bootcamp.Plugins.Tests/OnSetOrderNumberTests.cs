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
        public void OnSetOrderNumber_Autonumber_Created_V2()
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
        public void OnSetOrderNumber_Autonumber_Updated_V2()
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
