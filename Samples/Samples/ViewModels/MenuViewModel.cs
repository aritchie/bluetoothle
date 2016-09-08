using System;
using System.Collections.Generic;
using System.Windows.Input;
using Acr;
using Samples.Services;
using Samples.ViewModels.Le;


namespace Samples.ViewModels
{
    public class MenuItem
    {
        public string Text { get; set; }
        public ICommand Command { get; set; }
    }


    public class MenuViewModel : AbstractViewModel
    {
        public MenuViewModel(IViewModelManager manager)
        {
            this.Items = new List<MenuItem>
            {
                new MenuItem
                {
                    Text = "BLE Scanner",
                    Command = new Command(() => manager.SetDetail<ScanViewModel>())
                },
                new MenuItem
                {
                    Text = "BLE Background",
                    Command = new Command(() => manager.SetDetail<BackgroundViewModel>())
                }
            };
        }


        public IList<MenuItem> Items { get; }
    }
}
