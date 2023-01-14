using System;

namespace NBG.LogicGraph
{
    /// <summary>
    /// Used when validating variable types during LogicGraph execution.
    /// </summary>
    public class InvalidVariableTypeException : Exception
    {
        public InvalidVariableTypeException()
        {
        }

        public InvalidVariableTypeException(string message)
            : base(message)
        {
        }

        public InvalidVariableTypeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
