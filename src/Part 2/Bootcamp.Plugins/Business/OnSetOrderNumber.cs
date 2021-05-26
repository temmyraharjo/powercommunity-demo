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