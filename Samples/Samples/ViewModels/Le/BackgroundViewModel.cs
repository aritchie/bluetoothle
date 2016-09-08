using System;
using System.Reactive.Linq;
using Acr.UserDialogs;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Samples.Services;


namespace Samples.ViewModels.Le
{
    public class BackgroundViewModel : AbstractViewModel
    {
        public BackgroundViewModel(IAppSettings settings, IUserDialogs dialogs)
        {
            this.ServiceUuid = settings.BleServerServiceUuid.ToString();
            this.IsEnabled = settings.BleServerEnabled;

            this.WhenAnyValue(x => x.IsEnabled)
                .Skip(1)
                .Subscribe(x =>
                {
                    if (!x)
                        settings.BleServerEnabled = false;
                    else
                    {
                        var uuid = Guid.Empty;
                        if (!Guid.TryParse(this.ServiceUuid, out uuid))
                            dialogs.Alert("Invalid UUID");
                        else
                        {
                            settings.BleServerEnabled = true;
                            settings.BleServerServiceUuid = uuid;
                            dialogs.Alert("Background Settings Updated", "Success");
                        }
                    }
                });
        }


        [Reactive] public string ServiceUuid { get; set; }
        [Reactive] public bool IsEnabled { get; set; }
    }
}
