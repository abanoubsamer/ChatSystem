using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models.State
{
    [BsonIgnoreExtraElements]
    public class OrleansWrapper<T>
    {
        public string? _id { get; set; }
        public string? _etag { get; set; }
        public T? _doc { get; set; }
    }
}
