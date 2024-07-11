using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace InuLogs.src.Models
{
    internal class Sequence
    {
        [BsonId]
        public string _Id { get; set; }

        public int Value { get; set; }
    }
}
