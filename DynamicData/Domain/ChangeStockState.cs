using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class ChangeStockState : CommandBase<Stock>
    {
        public StockState State { get; set; }

        public override void Apply(Stock aggregate)
        {
            aggregate.State = State;
        }
    }
}
