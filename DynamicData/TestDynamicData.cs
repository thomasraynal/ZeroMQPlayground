using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQPlayground.DynamicData.Domain;
using ZeroMQPlayground.DynamicData.Dto;
using ZeroMQPlayground.DynamicData.Shared;
using ZeroMQPlayground.Shared;

namespace ZeroMQPlayground.DynamicData
{
    [TestFixture]
    public class TestDynamicData
    {
        [Test]
        public void TestEventSubjectSerialization()
        {
            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);

            var changeCcyPairState = new ChangeCcyPairState()
            {
                EventStreamId = "test",
                State = CcyPairState.Active,
                Market = "FxConnect"
            };

            var subject = eventSerializer.GetSubject(changeCcyPairState);
            Assert.AreEqual("test.Active.FxConnect", subject);

            changeCcyPairState = new ChangeCcyPairState()
            {
                EventStreamId = "test",
                State = CcyPairState.Passive,
            };

            subject = eventSerializer.GetSubject(changeCcyPairState);
            Assert.AreEqual("test.Passive.*", subject);

            var changeCcyPairPrice = new ChangeCcyPairPrice(
                 ccyPairId: "test",
                 market: "market",
                 ask: 0.1,
                 bid: 0.1,
                 mid: 0.1,
                 spread: 0.02
             );

            subject = eventSerializer.GetSubject(changeCcyPairPrice);
            Assert.AreEqual("test.market", subject);


        }

        [Test]
        public void TestEventApply()
        {
            var ccyPair = new CurrencyPair()
            {
                Ask = 0.1,
                Bid = 0.1,
                Mid = 0.1,
                Spread = 0.02,
                State = CcyPairState.Active,
                Id = "EUR/USD"
            };

            var changeStateClose = new ChangeCcyPairState()
            {
                State = CcyPairState.Passive
            };

            ccyPair.Apply(changeStateClose);

            Assert.AreEqual(CcyPairState.Passive, ccyPair.State);
            Assert.AreEqual(1, ccyPair.AppliedEvents.Count());

            var changeStateOpen = new ChangeCcyPairState()
            {
                State = CcyPairState.Active
            };

            ccyPair.Apply(changeStateOpen as IEvent);

            Assert.AreEqual(CcyPairState.Active, ccyPair.State);
            Assert.AreEqual(2, ccyPair.AppliedEvents.Count());
        }
    }
}
