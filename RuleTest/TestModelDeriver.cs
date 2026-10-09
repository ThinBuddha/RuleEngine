using System;
using Rules.Model;
using Xunit;

namespace RuleTest
{
    public class TestModelDeriver
    {
        [Fact]
        public void BuildsNestedClassesFromConditionsAndConclusions()
        {
            var containers = TestHelper.ParseRules(
                "RuleContainer Dispatch Rule route If Parcel.Drone.status = \"allowed\" and Parcel.Distance < 5 " +
                "Then Delivery.Route := \"air\" and Delivery.Ready " +
                "Rule ground If Parcel.Drone.status = \"blocked\" Then Delivery.Route := \"ground\"");

            var types = DerivedModelTypes.Derive(containers);
            var model = ModelDeriver.Derive(types);

            Assert.Empty(model.Name);
            Assert.Empty(model.Properties);
            var parcel = model.Classes["Parcel"];
            Assert.Equal("Parcel", parcel.FullName);
            var status = parcel.Classes["Drone"].Properties["status"];
            Assert.Equal("Parcel.Drone.status", status.FullName);
            Assert.Equal("string", status.Kind);
            Assert.Equal(new[] { "allowed", "blocked" }, status.StringValues);
            Assert.Equal("numeric", parcel.Properties["Distance"].Kind);

            var delivery = model.Classes["Delivery"];
            Assert.Equal("Boolean", delivery.Properties["Ready"].Kind);
            Assert.Equal("string", delivery.Properties["Route"].Kind);
            Assert.Equal(new[] { "air", "ground" }, delivery.Properties["Route"].StringValues);
            Assert.Equal(types.GetStringValues("Delivery.Route"), delivery.Properties["Route"].StringValues);
        }

        [Fact]
        public void KeepsSingleSegmentIdsAtTheRootAndDoesNotMutateTheRules()
        {
            var containers = TestHelper.ParseRules(
                "RuleContainer Demo Rule result If Input Then Output := Input");

            var model = ModelDeriver.Derive(containers);

            Assert.Equal("Boolean", model.Properties["Input"].Kind);
            Assert.Equal("Boolean", model.Properties["Output"].Kind);
            Assert.Empty(model.Classes);
            Assert.Single(containers.Containers[0].Rules);
        }
    }
}
