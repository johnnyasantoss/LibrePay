﻿using System;
using BitcoinPOS_App.ViewModels;
using Xamarin.Forms.Xaml;

namespace BitcoinPOS_App.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PaymentFinalizationPage
    {
        private readonly PaymentFinalizationViewModel _viewModel;

        public PaymentFinalizationPage()
        {
            InitializeComponent();
            BindingContext = _viewModel = new PaymentFinalizationViewModel();
        }

        public PaymentFinalizationPage(PaymentFinalizationViewModel viewModel) : this()
        {
            BindingContext = _viewModel = viewModel;
        }

        private void Cancel_Clicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Ok_Clicked(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}