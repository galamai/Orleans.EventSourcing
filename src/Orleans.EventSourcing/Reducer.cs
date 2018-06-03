using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public delegate TState Reducer<TState>(TState previousState, IEvent evt);

    public static class Reducer
    {
        public static TState Reduce<TState>(TState state, IEvent evt)
        {
            ((dynamic)state).Apply((dynamic)evt);
            return state;
        }
    }
}
