using System;
using System.Collections.Generic;
using System.Text;
using ZeroMQPlayground.ZeroMQPatterns.Majordomo.Actions;

namespace ZeroMQPlayground.ZeroMQPatterns.Majordomo.Domain
{
    public class BrewBeer : ICommand
    {
        public TeaType Type { get; set; }
        public double Quantity { get; set; }
    }
}
