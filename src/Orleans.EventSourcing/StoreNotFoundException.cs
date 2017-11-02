using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    [Serializable]
    public class StoreNotFoundException : Exception
    {
        public StoreNotFoundException() { }
        public StoreNotFoundException(string message) : base(message) { }
        public StoreNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected StoreNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
