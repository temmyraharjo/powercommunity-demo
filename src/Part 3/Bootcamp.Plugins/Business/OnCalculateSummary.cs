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
