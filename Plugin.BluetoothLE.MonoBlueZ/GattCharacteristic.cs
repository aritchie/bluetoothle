using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Mono.BlueZ.DBus;


namespace Plugin.BluetoothLE
{
    public class GattCharacteristic : AbstractGattCharacteristic
    {
        readonly GattCharacteristic1 native;


        public GattCharacteristic(GattCharacteristic1 native, IGattService service, CharacteristicProperties properties)
            : base(service, Guid.Parse(native.UUID), properties)
        {
            this.native = native;
        }


        public override IObservable<bool> SetNotificationValue(CharacteristicConfigDescriptorValue value) => Observable.Create<bool>(ob =>
        {
            switch (value)
            {
                case CharacteristicConfigDescriptorValue.None:
                    this.native.StopNotify();
                    break;

                default:
                    this.native.StartNotify();
                    break;
            }
            return Disposable.Empty;
        });


        public override IObservable<CharacteristicResult> WhenNotificationReceived()
        {
            throw new NotImplementedException();
        }


        public override IObservable<IGattDescriptor> WhenDescriptorDiscovered()
        {
            throw new NotImplementedException();
        }


        public override void WriteWithoutResponse(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicResult> Write(byte[] value)
        {
            throw new NotImplementedException();
        }


        public override IObservable<CharacteristicResult> Read()
        {
            throw new NotImplementedException();
        }
    }
}
/*
var readCharPath = new ObjectPath("/org/bluez/hci0/dev_F6_58_7F_09_5D_E6/service000c/char000f");
				var  readChar= GetObject<GattCharacteristic1>(Service,readCharPath);
				var properties = GetObject<Properties>(Service,readCharPath);

				properties.PropertiesChanged += new PropertiesChangedHandler(
					new Action<string,IDictionary<string,object>,string[]>((@interface,changed,invalidated)=>{
						System.Console.WriteLine("Properties Changed on "+@interface);
						if(changed!=null)
						{
							foreach(var prop in changed.Keys)
							{
								if(changed[prop] is byte[])
								{
									foreach(var b in ((byte[])changed[prop]))
									{
										System.Console.Write(b+",");
									}
									System.Console.WriteLine("");
								}
								else
								{
									System.Console.WriteLine("{0}={1}",prop,changed[prop]);
								}
							}
						}

						if(invalidated!=null)
						{
							foreach(var prop in invalidated)
							{
								System.Console.WriteLine(prop+" Invalidated");
							}
						}
					}));

				foreach(var device in devices)
				{
					System.Console.WriteLine("Connecting to "+device.Name);
					device.Connect();
					System.Console.WriteLine("\tConnected");
				}

				readChar.StartNotify();

				System.Threading.Thread.Sleep(10000);

				readChar.StopNotify();
				System.Threading.Thread.Sleep(500);
			}
			finally
			{
				if (devices != null) {
					foreach(var device in devices)
					{
						System.Console.WriteLine("Disconnecting "+device.Name);
						device.Disconnect();
						System.Console.WriteLine("\tDisconnected");
					}
				}
				agentManager.UnregisterAgent (agentPath);
				gattManager.UnregisterProfile (gattProfilePath);
			}
     */