namespace NBG.NodeGraph
{
    public class DrawConditionAttribute : NBGAttribute
    {

    }

    public class ShowIfAttribute : DrawConditionAttribute
    {
        public string[] Conditions { get; private set; }
        public new ConditionOperator ConditionOperator { get; private set; }
        public bool Reversed { get; protected set; }

        public ShowIfAttribute(params string[] condition)
        {
            ConditionOperator = ConditionOperator.And;
            Conditions = condition;
        }

        public ShowIfAttribute(ConditionOperator conditionOperator, params string[] conditions)
        {
            ConditionOperator = conditionOperator;
            Conditions = conditions;
        }
    }

    public class HideIfAttribute : ShowIfAttribute
    {
        public HideIfAttribute(string condition)
            : base(condition)
        {
            Reversed = true;
        }

        public HideIfAttribute(ConditionOperator conditionOperator, params string[] conditions)
            : base(conditionOperator, conditions)
        {
            Reversed = true;
        }
    }
}
