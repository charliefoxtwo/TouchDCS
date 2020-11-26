using System.Collections.Generic;

namespace OscConfiguration
{
    public class OscControl
    {
        public ControlType ControlType { get; set; }

        /// <summary>
        /// Use this if you want to invert the value of a control (e.g. if you want a toggle to send 0 when on and 1 when off).
        /// </summary>
        public bool Inverted { get; set; }

        /// <summary>
        /// Must be specified for multi-toggle controls if you want anything to work right.
        /// </summary>
        public Orientation Orientation { get; set; }

        /// <summary>
        /// Use this if you have a row of toggle buttons and they are mirrored in effect (e.g. the top one comes on when the bottom one should).
        /// </summary>
        public bool InvertOrientation { get; set; }

        public bool IgnoreSetState { get; set; }

        public int FixedStepOverride { get; set; }

        /// <summary>
        /// If null, nothing to see here. Otherwise, says that this osc _actually_ maps to a specific property and not its osc name
        /// </summary>
        public string? BiosProperty { get; set; }

        /// <summary>
        /// Some modules output weird strings. Here, you can remap text from one string to another.
        /// </summary>
        public Dictionary<string, string>? ReMap { get; set; }

        public IntegerTransform? Transform { get; set; }
    }

    public enum Orientation
    {
        Unknown,
        Horizontal,
        Vertical
    }

    public enum ControlType
    {
        Unknown,
        MultiToggle,
        MultiToggleExclusive,
        Button,
        Toggle,
    }
}