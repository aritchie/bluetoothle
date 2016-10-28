using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;
using Acr;
using Acr.Ble;
using Acr.UserDialogs;
using ReactiveUI.Fody.Helpers;


namespace Samples.ViewModels.Le
{

    public class GattCharacteristicViewModel : AbstractViewModel
    {
        readonly IUserDialogs dialogs;
        IDisposable watcher;


        public GattCharacteristicViewModel(IUserDialogs dialogs, IGattCharacteristic characteristic)
        {
            this.dialogs = dialogs;
            this.Characteristic = characteristic;
        }


        public IGattCharacteristic Characteristic { get; }
        [Reactive] public string Value { get; private set; }
        [Reactive] public bool IsNotifying { get; private set; }
        [Reactive] public bool IsValueAvailable { get; private set; }
        [Reactive] public DateTime LastValue { get; private set; }

        public Guid Uuid => this.Characteristic.Uuid;
        public Guid ServiceUuid => this.Characteristic.Service.Uuid;
        public string Description => this.Characteristic.Description;
        public string Properties => this.Characteristic.Properties.ToString();
        public bool CanNotify => this.Characteristic.CanNotify();


        public void Select()
        {
            var cfg = new ActionSheetConfig()
                .SetTitle($"{this.Description} - {this.Uuid}")
                .SetCancel();

            if (this.Characteristic.CanWriteWithResponse())
            {
                cfg.Add("Write With Response", () => this.TryWrite(true));
            }
            if (this.Characteristic.CanWrite())
            {
                cfg.Add("Write Without Response", () => this.TryWrite(false));
            }

            if (this.Characteristic.CanRead())
            {
                cfg.Add("Read", async () =>
                {
                    try
                    {
                        var value = await this.Characteristic
                            .Read()
                            .Timeout(TimeSpan.FromSeconds(3))
                            .ToTask();
                        var utf8 = await this.dialogs.ConfirmAsync("Display Value as UTF8 or HEX?", okText: "UTF8", cancelText: "HEX");
                        this.SetReadValue(value, utf8);
                    }
                    catch (Exception ex)
                    {
                        dialogs.Alert($"Error Reading {this.Characteristic.Uuid} - {ex}");
                    }
                });
            }

            if (this.Characteristic.CanNotify())
            {
                if (this.watcher == null)
                {
                    cfg.Add("Notify", async () =>
                    {
                        var utf8 = await this.dialogs.ConfirmAsync("Display Value as UTF8 or HEX?", okText: "UTF8", cancelText: "HEX");
                        this.watcher = this.Characteristic
                            .SubscribeToNotifications()
                            .Subscribe(x => this.SetReadValue(x, utf8));

                        this.IsNotifying = true;
                    });
                }
                else
                {
                    cfg.Add("Stop Notifying", () =>
                    {
                        this.watcher.Dispose();
                        this.watcher = null;
                        this.IsNotifying = false;
                    });
                }
            }
            if (cfg.Options.Any())
                this.dialogs.ActionSheet(cfg.SetCancel());
        }


        Task TryWrite(bool withResponse)
        {
            return this.Wrap(async () =>
            {
                var utf8 = await this.dialogs.ConfirmAsync("Write value from UTF8 or HEX?", okText: "UTF8", cancelText: "HEX");
                var result = await this.dialogs.PromptAsync("Please enter a write value", this.Description);

                if (result.Ok && !result.Text.IsEmpty())
                {
                    try
                    {
                        using (this.dialogs.Loading("Writing Value..."))
                        {
                            var value = result.Text.Trim();
                            var bytes = utf8 ? Encoding.UTF8.GetBytes(value) : value.FromHexString();
                            if (withResponse)
                            {
                                await this.Characteristic
                                    .Write(bytes)
                                    .Timeout(TimeSpan.FromSeconds(3))
                                    .ToTask();
                            }
                            else
                            {
                                this.Characteristic.WriteWithoutResponse(bytes);
                            }
                            this.Value = value;
                        }
                    }
                    catch (Exception ex)
                    {
                        this.dialogs.Alert($"Error Writing {this.Characteristic.Uuid} - {ex}");
                    }
                }
            });
        }


        async Task Wrap(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                this.dialogs.Alert(ex.ToString());
            }
        }


        void SetReadValue(byte[] value, bool fromUtf8)
        {
            this.IsValueAvailable = true;
            this.LastValue = DateTime.Now;

            if (value == null)
                this.Value = "EMPTY";
            else
                this.Value = fromUtf8 ? Encoding.UTF8.GetString(value, 0, value.Length) : BitConverter.ToString(value);
        }
    }
}
