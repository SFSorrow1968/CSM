using CSM.Configuration;
using NUnit.Framework;

namespace CSM.Tests
{
    [TestFixture]
    public class CSMModOptionsTests
    {
        [Test]
        public void ModOptions_DefaultsAreSensible()
        {
            // Verify reasonable defaults
            Assert.That(CSMModOptions.EnableMod, Is.True, "Mod should be enabled by default");
            Assert.That(CSMModOptions.DebugLogging, Is.False, "Debug logging should be off by default");
        }

        [Test]
        public void ModOptions_VersionIsSet()
        {
            // Version should be a valid semver-like string
            Assert.That(CSMModOptions.VERSION, Is.Not.Null.And.Not.Empty);
            Assert.That(CSMModOptions.VERSION, Does.Match(@"^\d+\.\d+\.\d+"));
        }

        [Test]
        public void ModOptions_OptionConstantsAreDefined()
        {
            // Verify option constants exist and are unique
            var constants = new[]
            {
                CSMModOptions.OptionEnableMod,
                CSMModOptions.OptionDebugLogging
            };

            var uniqueSet = new System.Collections.Generic.HashSet<string>(constants);
            Assert.That(uniqueSet.Count, Is.EqualTo(constants.Length), "Option constants should be unique");
        }

        [Test]
        public void ModOptions_CategoryConstantsAreDefined()
        {
            // Verify category constants exist
            Assert.That(CSMModOptions.CategoryPresetSelection, Is.Not.Null.And.Not.Empty);
            Assert.That(CSMModOptions.CategoryAdvanced, Is.Not.Null.And.Not.Empty);
        }
    }
}
