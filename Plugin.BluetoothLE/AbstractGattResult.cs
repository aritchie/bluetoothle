using System;


namespace Plugin.BluetoothLE
{
    public abstract class AbstractGattResult : IGattResult
    {
        protected AbstractGattResult(GattEvent gattEvent, byte[] data)
        {
            this.Event = gattEvent;
            this.Data = data;
        }


        protected AbstractGattResult(GattEvent gattEvent, string errorMessage)
        {
            this.Event = gattEvent;
            this.ErrorMessage = errorMessage;
        }


        public bool Success
        {
            get
            {
                switch (this.Event)
                {
                    case GattEvent.NotificationError:
                    case GattEvent.ReadError:
                    case GattEvent.WriteError:
                        return false;

                    default:
                        return true;
                }
            }
        }


        public string ErrorMessage { get; }
        public GattEvent Event { get; }
        public byte[] Data { get; }
    }
}
