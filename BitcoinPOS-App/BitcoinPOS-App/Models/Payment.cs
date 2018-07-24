using System;
using System.Diagnostics;
using BitcoinPOS_App.ViewModels;

namespace BitcoinPOS_App.Models
{
    [DebuggerDisplay("#{Id} - {Address} - {Value:n2}", Name = "#{Id}")]
    public class Payment
    {
        private decimal _value;

        public bool Done { get; set; }

        public long Id { get; set; }

        public decimal ValueFiat
        {
            get => _value;
            set
            {
                if (!string.IsNullOrWhiteSpace(Address))
                    throw new InvalidOperationException("Não é possível alterar o valor após iniciado o pagamento");

                _value = Math.Round(value, 2, MidpointRounding.AwayFromZero);
                Debug.WriteLine(
                    $"[{nameof(Payment)}.{nameof(ValueFiat)}.Set] Valor arredondado de {value} => {_value}"
                );
            }
        }

        public decimal ValueBitcoin => ExchangeRate?.GetExchangedValue(ValueFiat) ?? 0M;

        public ExchangeRate ExchangeRate { get; set; }

        public string Address { get; set; }

        public Payment()
        {
        }

        public Payment(MainPageViewModel viewModel)
        {
            ValueFiat = viewModel.TransactionValue;
        }
    }
}