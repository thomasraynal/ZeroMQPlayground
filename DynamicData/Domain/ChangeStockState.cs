using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class ChangeStockState : CommandBase<String,CurrencyPair>
    {
        public StockState State { get; set; }

        public override void Apply(CurrencyPair aggregate)
        {
            aggregate.State = State;
        }
    }
}
