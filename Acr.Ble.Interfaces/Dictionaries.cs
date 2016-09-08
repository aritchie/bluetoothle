using System;
using System.Collections.Generic;


namespace Acr.Ble
{

    public static class Dictionaries
    {
        public static string GetCharacteristicDescription(string uuid)
        {
            if (uuid == null)
                return null;

            return KnownCharacteristics.ContainsKey(uuid) ? KnownCharacteristics[uuid] : null;
        }


        public static string GetServiceDescription(string uuid)
        {
            if (uuid == null)
                return null;

            return KnownServices.ContainsKey(uuid) ? KnownServices[uuid] : null;
        }


        public static string GetDescriptorDescription(string uuid)
        {
            if (uuid == null)
                return null;

            return KnownDescriptors.ContainsKey(uuid) ? KnownDescriptors[uuid] : null;
        }


        public static IDictionary<string, string> KnownServices = new Dictionary<string, string>
        {
            { "00001811-0000-1000-8000-00805f9b34fb", "Alert Notification Service" },
            { "0000180f-0000-1000-8000-00805f9b34fb", "Battery Service" },
            { "00001810-0000-1000-8000-00805f9b34fb", "Blood Pressure" },
            { "00001805-0000-1000-8000-00805f9b34fb", "Current Time Service" },
            { "00001818-0000-1000-8000-00805f9b34fb", "Cycling Power" },
            { "00001816-0000-1000-8000-00805f9b34fb", "Cycling Speed and Cadence" },
            { "0000180a-0000-1000-8000-00805f9b34fb", "Device Information" },
            { "00001800-0000-1000-8000-00805f9b34fb", "Generic Access" },
            { "00001801-0000-1000-8000-00805f9b34fb", "Generic Attribute" },
            { "00001808-0000-1000-8000-00805f9b34fb", "Glucose" },
            { "00001809-0000-1000-8000-00805f9b34fb", "Health Thermometer" },
            { "0000180d-0000-1000-8000-00805f9b34fb", "Heart Rate" },
            { "00001812-0000-1000-8000-00805f9b34fb", "Human Interface Device" },
            { "00001802-0000-1000-8000-00805f9b34fb", "Immediate Alert" },
            { "00001803-0000-1000-8000-00805f9b34fb", "Link Loss" },
            { "00001819-0000-1000-8000-00805f9b34fb", "Location and Navigation" },
            { "00001807-0000-1000-8000-00805f9b34fb", "Next DST Change Service" },
            { "0000180e-0000-1000-8000-00805f9b34fb", "Phone Alert Status Service" },
            { "00001806-0000-1000-8000-00805f9b34fb", "Reference Time Update Service" },
            { "00001814-0000-1000-8000-00805f9b34fb", "Running Speed and Cadence" },
            { "00001813-0000-1000-8000-00805f9b34fb", "Scan Parameters" },
            { "00001804-0000-1000-8000-00805f9b34fb", "TX Power" },
            { "0000ffe0-0000-1000-8000-00805f9b34fb", "TI SensorTag Smart Keys" },
            { "f000aa00-0451-4000-b000-000000000000", "TI SensorTag Infrared Thermometer" },
            { "f000aa10-0451-4000-b000-000000000000", "TI SensorTag Accelerometer" },
            { "f000aa20-0451-4000-b000-000000000000", "TI SensorTag Humidity" },
            { "f000aa30-0451-4000-b000-000000000000", "TI SensorTag Magnometer" },
            { "f000aa40-0451-4000-b000-000000000000", "TI SensorTag Barometer" },
            { "f000aa50-0451-4000-b000-000000000000", "TI SensorTag Gyroscope" },
            { "f000aa60-0451-4000-b000-000000000000", "TI SensorTag Test" },
            { "f000ccc0-0451-4000-b000-000000000000", "TI SensorTag Connection Control" },
            { "f000ffc0-0451-4000-b000-000000000000", "TI SensorTag OvertheAir Download" },
            { "713d0000-503e-4c75-ba94-3148f18d941e", "TXRX_SERV_UUID RedBearLabs Biscuit Service" }
        };


        public static IDictionary<string, string> KnownCharacteristics = new Dictionary<string, string>
        {
            { "00002900-0000-1000-8000-00805f9b34fb", "Characteristic Extended Properties" },
            { "00002901-0000-1000-8000-00805f9b34fb", "Characteristic User Description" },
            { "00002902-0000-1000-8000-00805f9b34fb", "Client Characteristic Configuration" },
            { "00002903-0000-1000-8000-00805f9b34fb", "Server Characteristic Configuration" },
            { "00002904-0000-1000-8000-00805f9b34fb", "Characteristic Presentation Format" },
            { "00002905-0000-1000-8000-00805f9b34fb", "Characteristic Aggregate Format" },
            { "00002906-0000-1000-8000-00805f9b34fb", "Valid Range" },
            { "00002907-0000-1000-8000-00805f9b34fb", "External Report Reference" },
            { "00002908-0000-1000-8000-00805f9b34fb", "Export Reference" }
        };


        public static IDictionary<string, string> KnownDescriptors = new Dictionary<string, string>
        {
            { "00002900-0000-1000-8000-00805f9b34fb", "Characteristic Extended Properties" },
            { "00002901-0000-1000-8000-00805f9b34fb", "Characteristic User Description" },
            { "00002902-0000-1000-8000-00805f9b34fb", "Client Characteristic Configuration" },
            { "00002903-0000-1000-8000-00805f9b34fb", "Server Characteristic Configuration" },
            { "00002904-0000-1000-8000-00805f9b34fb", "Characteristic Presentation Format" },
            { "00002905-0000-1000-8000-00805f9b34fb", "Characteristic Aggregate Format" },
            { "00002906-0000-1000-8000-00805f9b34fb", "Valid Range" },
            { "00002907-0000-1000-8000-00805f9b34fb", "External Report Reference" },
            { "00002908-0000-1000-8000-00805f9b34fb", "Export Reference" }
        };
    }
}
