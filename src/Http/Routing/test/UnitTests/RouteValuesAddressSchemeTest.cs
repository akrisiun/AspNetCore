// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.TestObjects;
using Microsoft.AspNetCore.Routing.Tree;
using Xunit;

namespace Microsoft.AspNetCore.Routing
{
    public class RouteValuesAddressSchemeTest
    {
        [Fact]
        public void GetOutboundMatches_GetsNamedMatchesFor_EndpointsHaving_IRouteNameMetadata()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/a");
            var endpoint2 = CreateEndpoint("/a", routeName: "named");

            // Act
            var addressScheme = CreateAddressScheme(endpoint1, endpoint2);

            // Assert
            Assert.NotNull(addressScheme.State.AllMatches);
            Assert.Equal(2, addressScheme.State.AllMatches.Count());
            Assert.NotNull(addressScheme.State.NamedMatches);
            Assert.True(addressScheme.State.NamedMatches.TryGetValue("named", out var namedMatches));
            var namedMatch = Assert.Single(namedMatches);
            var actual = Assert.IsType<RouteEndpoint>(namedMatch.Match.Entry.Data);
            Assert.Same(endpoint2, actual);
        }

        [Fact]
        public void GetOutboundMatches_GroupsMultipleEndpoints_WithSameName()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/a");
            var endpoint2 = CreateEndpoint("/a", routeName: "named");
            var endpoint3 = CreateEndpoint("/b", routeName: "named");

            // Act
            var addressScheme = CreateAddressScheme(endpoint1, endpoint2, endpoint3);

            // Assert
            Assert.NotNull(addressScheme.State.AllMatches);
            Assert.Equal(3, addressScheme.State.AllMatches.Count());
            Assert.NotNull(addressScheme.State.NamedMatches);
            Assert.True(addressScheme.State.NamedMatches.TryGetValue("named", out var namedMatches));
            Assert.Equal(2, namedMatches.Count);
            Assert.Same(endpoint2, Assert.IsType<RouteEndpoint>(namedMatches[0].Match.Entry.Data));
            Assert.Same(endpoint3, Assert.IsType<RouteEndpoint>(namedMatches[1].Match.Entry.Data));
        }

        [Fact]
        public void GetOutboundMatches_GroupsMultipleEndpoints_WithSameName_IgnoringCase()
        {
            // Arrange
            var endpoint1 = CreateEndpoint("/a");
            var endpoint2 = CreateEndpoint("/a", routeName: "named");
            var endpoint3 = CreateEndpoint("/b", routeName: "NaMed");

            // Act
            var addressScheme = CreateAddressScheme(endpoint1, endpoint2, endpoint3);

            // Assert
            Assert.NotNull(addressScheme.State.AllMatches);
            Assert.Equal(3, addressScheme.State.AllMatches.Count());
            Assert.NotNull(addressScheme.State.NamedMatches);
            Assert.True(addressScheme.State.NamedMatches.TryGetValue("named", out var namedMatches));
            Assert.Equal(2, namedMatches.Count);
            Assert.Same(endpoint2, Assert.IsType<RouteEndpoint>(namedMatches[0].Match.Entry.Data));
            Assert.Same(endpoint3, Assert.IsType<RouteEndpoint>(namedMatches[1].Match.Entry.Data));
        }

        [Fact]
        public void EndpointDataSource_ChangeCallback_Refreshes_OutboundMatches()
        {
            // Arrange 1
            var endpoint1 = CreateEndpoint("/a");
            var dynamicDataSource = new DynamicEndpointDataSource(new[] { endpoint1 });

            // Act 1
            var addressScheme = new RouteValuesAddressScheme(new CompositeEndpointDataSource(new[] { dynamicDataSource }));

            // Assert 1
            var state = addressScheme.State;
            Assert.NotNull(state.AllMatches);
            var match = Assert.Single(state.AllMatches);
            var actual = Assert.IsType<RouteEndpoint>(match.Entry.Data);
            Assert.Same(endpoint1, actual);

            // Arrange 2
            var endpoint2 = CreateEndpoint("/b");

            // Act 2
            // Trigger change
            dynamicDataSource.AddEndpoint(endpoint2);

            // Arrange 2
            var endpoint3 = CreateEndpoint("/c");

            // Act 3
            // Trigger change
            dynamicDataSource.AddEndpoint(endpoint3);

            // Arrange 3
            var endpoint4 = CreateEndpoint("/d");

            // Act 4
            // Trigger change
            dynamicDataSource.AddEndpoint(endpoint4);

            // Assert 4
            Assert.NotSame(state, addressScheme.State);
            state = addressScheme.State;

            Assert.NotNull(state.AllMatches);
            Assert.Collection(
                state.AllMatches,
                (m) =>
                {
                    actual = Assert.IsType<RouteEndpoint>(m.Entry.Data);
                    Assert.Same(endpoint1, actual);
                },
                (m) =>
                {
                    actual = Assert.IsType<RouteEndpoint>(m.Entry.Data);
                    Assert.Same(endpoint2, actual);
                },
                (m) =>
                {
                    actual = Assert.IsType<RouteEndpoint>(m.Entry.Data);
                    Assert.Same(endpoint3, actual);
                },
                (m) =>
                {
                    actual = Assert.IsType<RouteEndpoint>(m.Entry.Data);
                    Assert.Same(endpoint4, actual);
                });
        }

        [Fact]
        public void FindEndpoints_LookedUpByCriteria_NoMatch()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent=true}/{zipCode}",
                defaults: new { zipCode = 3510 },
                requiredValues: new { id = 7 },
                routeName: "OrdersApi");
            var endpoint2 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent=true}/{zipCode}",
                defaults: new { id = 12 },
                requiredValues: new { zipCode = 3510 },
                routeName: "OrdersApi");
            var addressScheme = CreateAddressScheme(endpoint1, endpoint2);

            // Act
            var foundEndpoints = addressScheme.FindEndpoints(
                new RouteValuesAddress
                {
                    ExplicitValues = new RouteValueDictionary(new { id = 8 }),
                    AmbientValues = new RouteValueDictionary(new { urgent = false }),
                });

            // Assert
            Assert.Empty(foundEndpoints);
        }

        [Fact]
        public void FindEndpoints_LookedUpByCriteria_OneMatch()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent=true}/{zipCode}",
                defaults: new { zipCode = 3510 },
                requiredValues: new { id = 7 },
                routeName: "OrdersApi");
            var endpoint2 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent=true}/{zipCode}",
                defaults: new { id = 12 },
                routeName: "OrdersApi");
            var addressScheme = CreateAddressScheme(endpoint1, endpoint2);

            // Act
            var foundEndpoints = addressScheme.FindEndpoints(
                new RouteValuesAddress
                {
                    ExplicitValues = new RouteValueDictionary(new { id = 13 }),
                    AmbientValues = new RouteValueDictionary(new { zipCode = 3500 }),
                });

            // Assert
            var actual = Assert.Single(foundEndpoints);
            Assert.Same(endpoint2, actual);
        }

        [Fact]
        public void FindEndpoints_LookedUpByCriteria_MultipleMatches()
        {
            // Arrange
            var endpoint1 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent=true}/{zipCode}",
                defaults: new { zipCode = 3510 },
                requiredValues: new { id = 7 },
                routeName: "OrdersApi");
            var endpoint2 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent}/{zipCode}",
                defaults: new { id = 12 },
                routeName: "OrdersApi");
            var endpoint3 = CreateEndpoint(
                "api/orders/{id}/{name?}/{urgent=true}/{zipCode}",
                defaults: new { id = 12 },
                routeName: "OrdersApi");
            var addressScheme = CreateAddressScheme(endpoint1, endpoint2, endpoint3);

            // Act
            var foundEndpoints = addressScheme.FindEndpoints(
                new RouteValuesAddress
                {
                    ExplicitValues = new RouteValueDictionary(new { id = 7 }),
                    AmbientValues = new RouteValueDictionary(new { zipCode = 3500 }),
                });

            // Assert
            Assert.Contains(endpoint1, foundEndpoints);
            Assert.Contains(endpoint1, foundEndpoints);
        }

        [Fact]
        public void FindEndpoints_ReturnsEndpoint_WhenLookedUpByRouteName()
        {
            // Arrange
            var expected = CreateEndpoint(
                "api/orders/{id}",
                defaults: new { controller = "Orders", action = "GetById" },
                requiredValues: new { controller = "Orders", action = "GetById" },
                routeName: "OrdersApi");
            var addressScheme = CreateAddressScheme(expected);

            // Act
            var foundEndpoints = addressScheme.FindEndpoints(
                new RouteValuesAddress
                {
                    ExplicitValues = new RouteValueDictionary(new { id = 10 }),
                    AmbientValues = new RouteValueDictionary(new { controller = "Home", action = "Index" }),
                    RouteName = "OrdersApi"
                });

            // Assert
            var actual = Assert.Single(foundEndpoints);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void FindEndpoints_AlwaysReturnsEndpointsByRouteName_IgnoringMissingRequiredParameterValues()
        {
            // Here 'id' is the required value. The endpoint addressScheme would always return an endpoint by looking up
            // name only. Its the link generator which uses these endpoints finally to generate a link or not
            // based on the required parameter values being present or not.

            // Arrange
            var expected = CreateEndpoint(
                "api/orders/{id}",
                defaults: new { controller = "Orders", action = "GetById" },
                requiredValues: new { controller = "Orders", action = "GetById" },
                routeName: "OrdersApi");
            var addressScheme = CreateAddressScheme(expected);

            // Act
            var foundEndpoints = addressScheme.FindEndpoints(
                new RouteValuesAddress
                {
                    ExplicitValues = new RouteValueDictionary(),
                    AmbientValues = new RouteValueDictionary(),
                    RouteName = "OrdersApi"
                });

            // Assert
            var actual = Assert.Single(foundEndpoints);
            Assert.Same(expected, actual);
        }

        [Fact]
        public void GetOutboundMatches_DoesNotInclude_EndpointsWithSuppressLinkGenerationMetadata()
        {
            // Arrange
            var endpoint = CreateEndpoint(
                "/a",
                metadataCollection: new EndpointMetadataCollection(new[] { new SuppressLinkGenerationMetadata() }));

            // Act
            var addressScheme = CreateAddressScheme(endpoint);

            // Assert
            Assert.Empty(addressScheme.State.AllMatches);
        }

        [Fact]
        public void AddressScheme_UnsuppressedEndpoint_IsUsed()
        {
            // Arrange
            var endpoint = EndpointFactory.CreateRouteEndpoint(
                "/a",
                metadata: new object[] { new SuppressLinkGenerationMetadata(), new EncourageLinkGenerationMetadata(), });

            // Act
            var addressScheme = CreateAddressScheme(endpoint);

            // Assert
            Assert.Same(endpoint, Assert.Single(addressScheme.State.AllMatches).Entry.Data);
        }

        private RouteValuesAddressScheme CreateAddressScheme(params Endpoint[] endpoints)
        {
            return CreateAddressScheme(new DefaultEndpointDataSource(endpoints));
        }

        private RouteValuesAddressScheme CreateAddressScheme(params EndpointDataSource[] dataSources)
        {
            return new RouteValuesAddressScheme(new CompositeEndpointDataSource(dataSources));
        }

        private RouteEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object requiredValues = null,
            int order = 0,
            string routeName = null,
            EndpointMetadataCollection metadataCollection = null)
        {
            if (metadataCollection == null)
            {
                var metadata = new List<object>();
                if (!string.IsNullOrEmpty(routeName) || requiredValues != null)
                {
                    metadata.Add(new RouteValuesAddressMetadata(routeName, new RouteValueDictionary(requiredValues)));
                }
                metadataCollection = new EndpointMetadataCollection(metadata);
            }

            return new RouteEndpoint(
                TestConstants.EmptyRequestDelegate,
                RoutePatternFactory.Parse(template, defaults, parameterPolicies: null),
                order,
                metadataCollection,
                null);
        }

        private class EncourageLinkGenerationMetadata : ISuppressLinkGenerationMetadata
        {
            public bool SuppressLinkGeneration => false;
        }
    }
}
