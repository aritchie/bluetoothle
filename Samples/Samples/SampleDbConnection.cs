using System;
using System.IO;
using Plugin.BluetoothLE;
using Samples.Models;
using SQLite;


namespace Samples
{

    public class SampleDbConnection : SQLiteConnection
    {

        public SampleDbConnection(string databasePath) : base(Path.Combine(databasePath, "ble.db"))
        {
            this.CreateTable<BleRecord>();
            Log.Out = log => { };
        }


        public TableQuery<BleRecord> BleRecords => this.Table<BleRecord>();
    }
}