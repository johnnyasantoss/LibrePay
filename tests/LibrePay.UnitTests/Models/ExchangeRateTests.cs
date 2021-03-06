using System;
using System.Globalization;
using System.Linq;
using LibrePay.Models;
using Xunit;

namespace LibrePay.UnitTests.Models
{
    public class ExchangeRateTests
    {
        [Fact]
        public void ToStringIsOverriden()
        {
            var er = new ExchangeRate(0.5M, "R$/BTC", DateTime.Now, CultureInfo.InvariantCulture);

            Assert.Equal($"Rate: {er.DisplayRate}\n" + $"Date: {er.Date:d}", er.ToString());
        }

        [Fact]
        public void GetExchangedValueReturnsANumberRoundedBy8()
        {
            var er = new ExchangeRate(5999999M, "R$/BTC", DateTime.Now, CultureInfo.InvariantCulture);
            var valueFiat = 15M;

            var result = er.ExchangeValueTo(valueFiat);

            var numberOfDecimals = result.ToString(CultureInfo.InvariantCulture).Split('.').Last().Length;
            Assert.Equal(numberOfDecimals, Constants.BitcoinDecimals);

            var roundValue = Math.Round(valueFiat / er.Rate, Constants.BitcoinDecimals, MidpointRounding.AwayFromZero);
            Assert.Equal(result, roundValue);
        }
    }
}
