using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.EventSourcing
{
    public interface IGrainWithState<TState>
    {
        Task<TState> GetStateAsync();
    }
}
