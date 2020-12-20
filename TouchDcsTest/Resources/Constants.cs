namespace TouchDcsTest.Resources
{
    public class Constants
    {
        
    }
    
    // TODO: every control type needs a configured and non-configured test where possible.
    // TODO: how will unit tests handle the idea of multiple aircraft and switching between them?
    // TODO: definitely need bios parsing tests....
    // TODO: tests currently succeed when no active aircraft has been selected even though nothing happens.

    public static class FixedStepKeys
    {
        public const string SyncAddress = "FixedStepTests";
        public const string BiosCode = "FixedStep";
        public const string OscCodeUp = "FixedStepUp";
        public const string FixedStepDown = "FixedStepDown";
    }

    public static class MultiToggleExclusiveKeys
    {
        public const string SyncAddress = "MultiToggleExclusiveTests";
        public const string HorizontalMultiToggleExclusive = "HorizontalMultiToggleExclusive";
        public const string VerticalMultiToggleExclusive = "VerticalMultiToggleExclusive";
        public const string HorizontalInvertedMultiToggleExclusive = "HorizontalInvertedMultiToggleExclusive";
        public const string VerticalInvertedMultiToggleExclusive = "VerticalInvertedMultiToggleExclusive";
    }

    public static class ReMapKeys
    {
        public const string SyncAddress = "ReMapTests";
        public const string RemappedStringOut = "RemappedStringOut";
        public const string RemappedIntOut = "RemappedIntOut";
    }

    public static class TransformIntegerKeys
    {
        public const string SyncAddress = "TransformIntegerTests";
        public const string TransformedIntOutMultiply = "TransformedIntOutMultiply";
        public const string TransformedIntOutDivide = "TransformedIntOutDivide";
        public const string TransformedIntOutAdd = "TransformedIntOutAdd";
        public const string TransformedIntOutSubtract = "TransformedIntOutSubtract";
        public const string TransformedIntOutForceDecimal = "TransformedIntOutForceDecimal";
    }

    /*
     * Inputs:
     *  - Set state
     *  - Fixed step
     *  - Variable step
     *  - action
     */

    /*
     * Outputs:
     *  - String
     *  - Integer
     */

    /*
     * json config
     * no json config
     */

    /*
     * LED
     * Label
     * Push Button
     * Toggle Button
     * Fader
     * Rotary
     * Encoder
     */
    public static class LedKeys
    {
        public const string ConfigSync = "LedAircraftSync";
        public const string SyncAddress = "LedTests";
        // only need to test integer out, clamped 0-1
        public const string NoConfig = "LedNoConfig";
        public const string Config = "LedConfig";
        public const string NoConfigNeedsClamp = "LedNoConfigNeedsClamp";
        public const string ConfigNeedsClamp = "LedConfigNeedsClamp";
    }
}