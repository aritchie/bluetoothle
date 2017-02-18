using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Acr;
using Plugin.BluetoothLE;
using Acr.UserDialogs;
using ReactiveUI.Fody.Helpers;
using Xamarin.Forms;

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
            if (this.Characteristic.CanWriteWithoutResponse())
            {
                cfg.Add("Write Without Response", () => this.TryWrite(false));
            }
            if (this.Characteristic.CanWrite())
            {
                cfg.Add("Send Test BLOB", () => this.SendBlob());
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
                        this.dialogs.Alert($"Error Reading {this.Characteristic.Uuid} - {ex}");
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


        async void SendBlob()
        {
            var useReliableWrite = await this.dialogs.ConfirmAsync(new ConfirmConfig()
                .UseYesNo()
                .SetTitle("Confirm")
                .SetMessage("Use reliable write transaction?")
            );
            var value = RandomString(5000);
            var cts = new CancellationTokenSource();
            var bytes = Encoding.UTF8.GetBytes(value);
            var dlg = this.dialogs.Loading("Sending Blob", () => cts.Cancel(), "Cancel");
            var sw = new Stopwatch();
            sw.Start();

            var sub = this.Characteristic
                .BlobWrite(bytes, useReliableWrite)
                .Subscribe(
                    s => dlg.Title = $"Sending Blob - Sent {s.Position} of {s.TotalLength} bytes",
                    ex =>
                    {
                        dlg.Dispose();
                        this.dialogs.Alert("Failed writing blob - " + ex);
                        sw.Stop();
                    },
                    () =>
                    {
                        dlg.Dispose();
                        sw.Stop();

                        var pre = useReliableWrite ? "reliable write" : "write";
                        this.dialogs.Alert($"BLOB {pre} took " + sw.Elapsed);
                    }
                );

            cts.Token.Register(() => sub.Dispose());
        }


        static Random random = new Random();
        static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                      .Select(s => s[random.Next(s.Length)]).ToArray());
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


        void SetReadValue(CharacteristicResult result, bool fromUtf8)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                this.IsValueAvailable = true;
                this.LastValue = DateTime.Now;

                if (result.Data == null)
                    this.Value = "EMPTY";
                else
                    this.Value = fromUtf8 ? Encoding.UTF8.GetString(result.Data, 0, result.Data.Length) : BitConverter.ToString(result.Data);
            });
        }
    }
}
