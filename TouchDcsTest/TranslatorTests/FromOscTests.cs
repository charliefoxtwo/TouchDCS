using BiosConfiguration;
using NUnit.Framework;
using TouchDcsTest.TranslatorTests.Keys;

namespace TouchDcsTest.TranslatorTests
{
    public class FromOscTests : TranslatorTestBase<ButtonTranslatorKeys>
    {
        public FromOscTests() : base("ButtonTests") { }

        [Theory]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(65535)]
        [TestCase(-1)]
        public void Button_FromOsc(int value)
        {
            BiosVerifier.Exactly(Keys.SetStateButton, value);
        }

        [TestCase(InputFixedStep.Increment, 1)]
        [TestCase(InputFixedStep.Decrement, -1)]
        public void Button_FromOsc_FixedStepRotary_IntIn(string expected, int valueIn)
        {
            BiosVerifier.Exactly(Keys.FixedStepRotary, expected, valueIn);
        }

        [TestCase(0)]
        [TestCase(2)]
        public void Button_FromOsc_SetStateRotary(int value)
        {
            BiosVerifier.Exactly(Keys.SetStateRotary, value);
        }

        [TestCase(InputFixedStep.Increment)]
        [TestCase(InputFixedStep.Decrement)]
        public void Button_FromOsc_FixedStepSetStateRotary_StringIn(string value)
        {
            BiosVerifier.Exactly(Keys.FixedStepSetStateRotary, value);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void Button_FromOsc_FixedStepSetStateRotary_IntIn(int value)
        {
            BiosVerifier.Exactly(Keys.FixedStepSetStateRotary, value);
        }

        [TestCase("+3200", InputFixedStep.Increment)]
        [TestCase("-3200", InputFixedStep.Decrement)]
        public void Button_FromOsc_VariableStepRotary(string expected, string valueIn)
        {
            BiosVerifier.Exactly(Keys.VariableStepRotary, expected, valueIn);
        }
    }
}