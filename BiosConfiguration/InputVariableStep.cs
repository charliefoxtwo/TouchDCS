namespace BiosConfiguration
{
    public class InputVariableStep : InputSetState
    {
        public new const string InterfaceType = "variable_step";

        public int SuggestedStep { get; set; }
    }
}