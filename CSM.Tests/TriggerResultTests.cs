using CSM.Core;
using NUnit.Framework;

namespace CSM.Tests
{
    [TestFixture]
    public class TriggerResultTests
    {
        [Test]
        public void TriggerResult_HasExpectedValues()
        {
            // Verify all expected enum values exist
            Assert.That(TriggerResult.Success, Is.EqualTo((TriggerResult)0));
            Assert.That(TriggerResult.ModDisabled, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.DOTKillDisabled, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.ThrownWeaponDisabled, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.DamageTypeDisabled, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.TriggerDisabled, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.GlobalCooldown, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.TriggerCooldown, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.AlreadyActive, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.EasingOut, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.ChanceFailed, Is.Not.EqualTo(TriggerResult.Success));
            Assert.That(TriggerResult.Error, Is.Not.EqualTo(TriggerResult.Success));
        }

        [Test]
        public void TriggerResult_AllValuesAreUnique()
        {
            var values = System.Enum.GetValues(typeof(TriggerResult));
            var uniqueValues = new System.Collections.Generic.HashSet<int>();

            foreach (TriggerResult value in values)
            {
                Assert.That(uniqueValues.Add((int)value), Is.True,
                    $"Duplicate enum value found: {value}");
            }
        }
    }
}
