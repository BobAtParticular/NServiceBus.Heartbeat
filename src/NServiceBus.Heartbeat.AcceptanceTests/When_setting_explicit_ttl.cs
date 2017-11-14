﻿namespace NServiceBus.Heartbeat.AcceptanceTests
{
    using System;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Config;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Satellites;
    using Transports;

    public class When_setting_explicit_ttl : NServiceBusAcceptanceTest
    {
        static string DetectorAddress => Conventions.EndpointNamingConvention(typeof(Sender)) + ".Detector";

        [Test]
        public void Should_use_it_for_check_messages()
        {
            var result = Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .Done(c => c.DetectedMessage != null)
                .Run();

            //We can't have an exact assertion because TTBR on received message show only the remaining value, not the original one
            Assert.Less(TimeSpan.FromSeconds(15), result.DetectedMessage.TimeToBeReceived);
            Assert.Greater(TimeSpan.FromSeconds(25), result.DetectedMessage.TimeToBeReceived);
        }

        class Context : ScenarioContext
        {
            public TransportMessage DetectedMessage { get; set; }
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.SendHeartbeatTo(DetectorAddress, TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(20));
                });
            }

            class Detector : ISatellite, IWantToRunWhenConfigurationIsComplete
            {
                public Context Context { get; set; }

                public bool Handle(TransportMessage message)
                {
                    Context.DetectedMessage = message;
                    return true;
                }

                public void Start()
                {
                }

                public void Stop()
                {
                }

                public Address InputAddress => Address.Parse(DetectorAddress);

                public bool Disabled => false;

                public void Run(Configure config)
                {
                    var queueCreator = config.Builder.Build<ICreateQueues>();
                    queueCreator.CreateQueueIfNecessary(InputAddress, null);
                }
            }
        }
    }
}