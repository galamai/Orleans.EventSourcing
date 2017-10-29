using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public delegate TState Reducer<TState>(TState previousState, IEvent evt);
}
