using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace Orleans.EventSourcing.AzureStorage
{
    public class EventTableEntity : TableEntity
    {
        public string Payload { get; set; }
        public string Type { get; set; }
    }
}
