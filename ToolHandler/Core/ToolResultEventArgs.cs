using System;

namespace ToolHandler.Core
{
    public class ToolResultEventArgs<T> : EventArgs
    {
        public T Result { get; }

        public ToolResultEventArgs(T result)
        {
            Result = result;
        }
    }
}