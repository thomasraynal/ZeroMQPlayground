using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.DynamicData.Shared;

namespace ZeroMQPlayground.DynamicData.Domain
{
    public class ChangeCcyPairState : CommandBase<String,CurrencyPair>
    {
        [RoutingPosition(1)]
        public CcyPairState State { get; set; }

        [RoutingPosition(2)]
        public string Market { get; set; }

        public override void Apply(CurrencyPair aggregate)
        {
            aggregate.State = State;
        }
    }
}
