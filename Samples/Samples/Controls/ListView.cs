using System;
using Xamarin.Forms;
using System.Windows.Input;
using Samples.ViewModels;


namespace Samples.Controls
{

    public class ListView : Xamarin.Forms.ListView
    {
        public static readonly BindableProperty ItemClickCommandProperty = BindableProperty.Create(
            nameof(Command),
            typeof(ICommand),
            typeof(ListView)
        );

        public ListView()
        {
            this.ItemTapped += this.OnItemTapped;
            this.ItemAppearing += this.OnItemAppearing;
            this.ItemDisappearing += this.OnItemDisappearing;
        }


        protected virtual void OnItemAppearing(object sender, ItemVisibilityEventArgs e)
            => (e.Item as IViewModel)?.OnActivate();


        protected virtual void OnItemDisappearing(object sender, ItemVisibilityEventArgs e)
            => (e.Item as IViewModel)?.OnDeactivate();


        public bool DisableRowSelection { get; set; } = true;


        public ICommand ItemClickCommand
        {
            get => (ICommand)this.GetValue(ItemClickCommandProperty);
            set => this.SetValue(ItemClickCommandProperty, value);
        }


        void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (this.DisableRowSelection)
                this.SelectedItem = null;

            if (e.Item != null && this.ItemClickCommand != null && this.ItemClickCommand.CanExecute(e))
            {
                this.ItemClickCommand.Execute(e.Item);
                this.SelectedItem = null;
            }
        }
    }
}