using NUnit.Framework;
using TouchDcsTest.TranslatorTests.Keys;

namespace TouchDcsTest.TranslatorTests
{
    public class StringOutTranslatorTests : TranslatorTestBase<StringOutTranslatorKeys>
    {
        public StringOutTranslatorTests() : base("StringOutTests") { }

        #region From Bios

        [Theory]
        [TestCase("foo", "foo")]
        [TestCase("bar", "bar")]
        [TestCase("", "")]
        public void StringOut_FromBios_NoConfig(string expected, string valueIn)
        {
             OscVerifier.Exactly(Keys.NoConfig, expected, valueIn);
        }

        [Theory]
        [TestCase("foo", "foo")]
        [TestCase("bar", "bar")]
        [TestCase("", "")]
        public void StringOut_FromBios_Config(string expected, string valueIn)
        {
            OscVerifier.Exactly(Keys.Config, expected, valueIn);
        }

        [Theory]
        [TestCase("B", "A")]
        [TestCase("a", "a")]
        [TestCase("B", "B")]
        [TestCase("C", "C")]
        [TestCase("D", "D")]
        [TestCase("E", "E")]
        [TestCase("E", "CD")]
        public void StringOut_FromBios_ReMap(string expected, string valueIn)
        {
            OscVerifier.Exactly(Keys.ReMap, expected, valueIn);
        }

        #endregion
    }
}