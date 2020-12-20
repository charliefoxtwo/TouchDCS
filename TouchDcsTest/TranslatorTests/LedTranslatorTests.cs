using NUnit.Framework;

namespace TouchDcsTest.TranslatorTests
{
    public class LedTranslatorTests : TranslatorTestBase<LedTranslatorKeys>
    {
        // TODO: we actually don't need that many test types. we only need matching output types and matching input types.
        // it would be far easier to do it that way than the current full json for every single test

        #region From Bios

        [Theory]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        public void Led_FromBios_NoConfig_Binary(float expected, int valueIn)
        {
             OscVerifier.Exactly(Keys.NoConfig, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        public void Led_FromBios_Config_Binary(float expected, int valueIn)
        {
            OscVerifier.Exactly(Keys.Config, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 65535)]
        [TestCase(.5f, 32768)]
        [TestCase(0, 0)]
        public void Led_FromBios_NoConfig_NeedsClamp(float expected, int valueIn)
        {
            OscVerifier.Approximate(Keys.NoConfigNeedsClamp, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 65535)]
        [TestCase(.5f, 32768)]
        [TestCase(0, 0)]
        public void Led_FromBios_Config_NeedsClamp(float expected, int valueIn)
        {
            OscVerifier.Approximate(Keys.ConfigNeedsClamp, expected,valueIn);
        }

        #endregion
    }
}