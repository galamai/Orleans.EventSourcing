using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing
{
    public static class Constants
    {
        public static readonly string DefaultEventStoreName = "Default";
        public static readonly string DefaultStateStoreName = "Default";
        public static readonly string DefaultDataSerializerName = "Default";
        public static readonly string DefaultStreamProviderName = "Default";
        public static readonly string EventStreamNamespace = "Events";
    }
}
