namespace TouchDcsTest.TranslatorTests
{
    public class ButtonTranslatorKeys : ITranslatorTest
    {
        public string ConfigSync { get; } = "ButtonAircraftSync";
        public string SyncAddress { get; } = "ButtonTests";
        public string NoConfig { get; } = "ButtonNoConfig";
        public string Config { get; } = "ButtonConfig";
        public string NoConfigNeedsClamp { get; } = "ButtonNoConfigNeedsClamp";
        public string ConfigNeedsClamp { get; } = "ButtonConfigNeedsClamp";
        public ButtonLimitedRotary LimitedRotary { get; } = new();
        public ButtonVariableStepDefault VariableStepDefault { get; } = new();
        public ButtonVariableStepExtra VariableStepExtra { get; } = new();

        public class ButtonLimitedRotary
        {
            public string BiosCode { get; } = "ButtonLimitedRotary";
            public string OscUp { get; } = "ButtonLimitedRotaryUp";
            public string OscDown { get; } = "ButtonLimitedRotaryDown";
        }

        public class ButtonVariableStepDefault
        {
            public string BiosCode { get; } = "ButtonVariableStep";
            public string OscUp { get; } = "ButtonVariableStepDefaultUp";
        }

        public class ButtonVariableStepExtra
        {
            public string BiosCode { get; } = "ButtonVariableStep";
            public string OscUp { get; } = "ButtonVariableStepExtraUp";
            public string OscDown { get; } = "ButtonVariableStepExtraDown";
        }
    }
}