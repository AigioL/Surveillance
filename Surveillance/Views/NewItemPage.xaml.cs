using Surveillance.Models;
using Surveillance.ViewModels;
using Xamarin.Forms;

namespace Surveillance.Views
{
    public partial class NewItemPage : ContentPage
    {
        public Item Item { get; set; }

        public NewItemPage()
        {
            InitializeComponent();
            BindingContext = new NewItemViewModel();
        }
    }
}