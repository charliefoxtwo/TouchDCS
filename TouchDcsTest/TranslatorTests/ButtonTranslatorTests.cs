using BiosConfiguration;
using NUnit.Framework;
using TouchDcsTest.TranslatorTests.Keys;

namespace TouchDcsTest.TranslatorTests
{
    public class ButtonTranslatorTests : TranslatorTestBase<ButtonTranslatorKeys>
    {
        public ButtonTranslatorTests() : base("ButtonTests") { }

        #region From Bios

        [Theory]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        public void Button_FromBios_NoConfig_Binary(float expected, int valueIn)
        {
            OscVerifier.Exactly(Keys.NoConfig, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        public void Button_FromBios_Config_Binary(float expected, int valueIn)
        {
            OscVerifier.Exactly(Keys.Config, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 65535)]
        [TestCase(.5f, 32768)]
        [TestCase(0, 0)]
        public void Button_FromBios_NoConfig_NeedsClamp(float expected, int valueIn)
        {
            OscVerifier.Approximate(Keys.NoConfigNeedsClamp, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 65535)]
        [TestCase(.5f, 32768)]
        [TestCase(0, 0)]
        public void Button_FromBios_Config_NeedsClamp(float expected, int valueIn)
        {
            OscVerifier.Approximate(Keys.ConfigNeedsClamp, expected,valueIn);
        }

        #endregion

        #region From OSC

        [Theory]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        public void Button_FromOsc_NoConfig_Binary(int expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.NoConfig, expected, valueIn);
        }

        [Theory]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        public void Button_FromOsc_Config_Binary(int expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.Config, expected, valueIn);
        }

        [Theory]
        [TestCase(65535, 1)]
        [TestCase(32768, .5f)]
        [TestCase(0, 0)]
        public void Button_FromOsc_NoConfig_NeedsClamp(int expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.NoConfigNeedsClamp, expected, valueIn);
        }

        [Theory]
        [TestCase(65535, 1)]
        [TestCase(32768, .5f)]
        [TestCase(0, 0)]
        public void Button_FromOsc_Config_NeedsClamp(int expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.ConfigNeedsClamp, expected,valueIn);
        }

        [TestCase(InputFixedStep.Increment, 1)]
        public void Button_FromOsc_LimitedRotary_Increment(string expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.LimitedRotary.OscUp, expected, valueIn, Keys.LimitedRotary.BiosCode);
        }

        [TestCase(InputFixedStep.Decrement, 1)]
        public void Button_FromOsc_LimitedRotary_Decrement(string expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.LimitedRotary.OscDown, expected, valueIn, Keys.LimitedRotary.BiosCode);
        }

        // default config decrement isn't a thing because it doesn't know the button is supposed to decrement
        [TestCase("+3200", 1)]
        public void Button_FromOsc_VariableStep_DefaultConfig_Increment(string expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.VariableStepDefault.OscUp, expected, valueIn, Keys.VariableStepDefault.BiosCode);
        }

        [TestCase("+1337", 1)]
        public void Button_FromOsc_VariableStep_ExtraConfig_Increment(string expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.VariableStepExtra.OscUp, expected, valueIn, Keys.VariableStepExtra.BiosCode);
        }

        [TestCase("-1337", 1)]
        public void Button_FromOsc_VariableStep_ExtraConfig_Decrement(string expected, float valueIn)
        {
            BiosVerifier.Exactly(Keys.VariableStepExtra.OscDown, expected, valueIn, Keys.VariableStepExtra.BiosCode);
        }

        #endregion
    }
}