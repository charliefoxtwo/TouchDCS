using NUnit.Framework;
using TouchDcsTest.TranslatorTests.Keys;

namespace TouchDcsTest.TranslatorTests
{
    public class FromBiosTests : TranslatorTestBase<ButtonTranslatorKeys>
    {
        public FromBiosTests() : base("ButtonTests") { }

        // bios will only ever send integers and strings
        [Theory]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(65535)]
        [TestCase(-1)]
        public void Integer_FromBios(int value)
        {
            OscVerifier.Exactly(Keys.SetStateButton, value);
        }

        [Theory]
        [TestCase("")]
        [TestCase("f")]
        [TestCase("MULTIPLE CHARACTERS")]
        [TestCase("42")]
        public void String_FromBios(string value)
        {
            OscVerifier.Exactly(Keys.SetStateButton, value);
        }
    }
}