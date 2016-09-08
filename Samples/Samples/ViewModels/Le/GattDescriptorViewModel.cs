using System;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Acr.UserDialogs;
using Acr.Ble;
using ReactiveUI.Fody.Helpers;

namespace Samples.ViewModels.Le
{
    public class GattDescriptorViewModel : AbstractViewModel
    {
        readonly IUserDialogs dialogs;


        public GattDescriptorViewModel(IUserDialogs dialogs, IGattDescriptor descriptor)
        {
            this.dialogs = dialogs;
            this.Descriptor = descriptor;
        }


        public IGattDescriptor Descriptor { get; }
        public string Description => this.Descriptor.Description;
        public string Uuid => this.Descriptor.Uuid.ToString();
        [Reactive] public DateTime LastValue { get; private set; }
        [Reactive] public bool IsValueAvailable { get; private set; }
        [Reactive] public string Value { get; private set; }

        public void Select() 
        {
            this.dialogs.ActionSheet(new ActionSheetConfig()
                .SetTitle($"Description - {this.Description} - {this.Uuid}")
                .SetCancel()                                     
                .Add("Read", async () => await this.Read()) 
                .Add("Write", async () => await this.Write())
            );
        }


        async Task Read() 
        {
            var value = await this.Descriptor.Read().ToTask();

            this.LastValue = DateTime.Now;
            this.IsValueAvailable = true;
            this.Value = value == null ? "EMPTY" : Encoding.UTF8.GetString(value, 0, value.Length);
        }


        async Task Write()
        {
            //var value = await this.Descriptor.Write(
        }
    }
}

