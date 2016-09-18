using System;
using SQLite.Net.Attributes;


namespace Samples.Models
{
    public class BleRecord
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public string Description { get; set; }
        public DateTime TimestampUtc { get; set; }
    }
}
