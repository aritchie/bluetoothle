using System;
using SQLite;


namespace Samples.Models
{
    public class BleRecord
    {
        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public string Description { get; set; }
        public DateTime TimestampLocal { get; set; }
    }
}
