namespace BiosConfiguration
{
    public class InputSetState : BiosInput
    {
        public const string InterfaceType = "set_state";

        public int MaxValue { get; set; }
    }
}