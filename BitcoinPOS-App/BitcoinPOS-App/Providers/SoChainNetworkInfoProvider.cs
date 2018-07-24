using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using BitcoinPOS_App.Interfaces.Providers;
using BitcoinPOS_App.Models;
using BitcoinPOS_App.Providers;
using NBitcoin;
using Newtonsoft.Json.Linq;
using Polly;
using Xamarin.Forms;
using Transaction = BitcoinPOS_App.Models.Transaction;

[assembly: Dependency(typeof(SoChainNetworkInfoProvider))]

namespace BitcoinPOS_App.Providers
{
    /// <summary>
    /// Uses SoChain's API (https://chain.so/api) to get network information
    /// </summary>
    public class SoChainNetworkInfoProvider : INetworkInfoProvider
    {
        private static readonly HttpClient HttpClient;
        private static readonly Policy<HttpResponseMessage> SoChainPolicy;
        private static readonly string SoChainNetwork;

        static SoChainNetworkInfoProvider()
        {
            HttpClient = new HttpClient
            {
                BaseAddress = new Uri("https://chain.so/api/v2"),
                DefaultRequestHeaders =
                {
                    Accept = {new MediaTypeWithQualityHeaderValue("application/json")}
                },
                Timeout = TimeSpan.FromSeconds(15)
            };

            var policyBuilder = Policy.HandleResult<HttpResponseMessage>(h => !h.IsSuccessStatusCode)
                .Or<Exception>();

            SoChainPolicy = Policy.WrapAsync(
                policyBuilder.FallbackAsync(new HttpResponseMessage(HttpStatusCode.BadRequest))
                , policyBuilder.WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(1))
                // throttle
                , Policy.HandleResult<HttpResponseMessage>(_ => true)
                    .Or<Exception>()
                    .CircuitBreakerAsync(1, TimeSpan.FromSeconds(5))
                , Policy.BulkheadAsync<HttpResponseMessage>(1)
                , Policy.TimeoutAsync<HttpResponseMessage>(_ => TimeSpan.FromSeconds(5))
            );

            if (Constants.NetworkInUse == Network.Main)
            {
                SoChainNetwork = "BTC";
            }
            else if (Constants.NetworkInUse == Network.TestNet)
            {
                SoChainNetwork = "BTCTEST";
            }
            else
            {
                SoChainNetwork = null;
                Debug.WriteLine("ERRO: Network desconhecida!!!");
            }
        }

        public BackgroundJob WaitCompletePayment(Payment payment, Action<decimal> onComplete)
        {
            if (payment == null) throw new ArgumentNullException(nameof(payment));
            if (onComplete == null) throw new ArgumentNullException(nameof(onComplete));

            if (payment.Done)
                return null;

            var valueBtc = payment.ValueBitcoin;

            return NotifyTransactionsOfAAddress(payment.Address, transactions =>
            {
                var totalValue = transactions.Sum(t => t.Value);

                if (totalValue >= valueBtc)
                {
                    onComplete(totalValue);
                    return true;
                }

                return false;
            });
        }

        private BackgroundJob NotifyTransactionsOfAAddress(
            string address
            , Func<IEnumerable<Transaction>, bool> onReceiveAnyTx
        )
        {
            if (address == null) throw new ArgumentNullException(nameof(address));
            if (onReceiveAnyTx == null) throw new ArgumentNullException(nameof(onReceiveAnyTx));

            var thread = new Thread(async () =>
            {
                while (true)
                {
                    var response = await SoChainPolicy
                        .ExecuteAsync(() =>
                        {
                            Debug.WriteLine($"[API/SoChain]: Realizando chamada. Address: {address}");
                            return HttpClient.GetAsync($"/api/v2/get_tx_received/{SoChainNetwork}/{address}");
                        });

                    if (!response.IsSuccessStatusCode)
                        continue;

                    var jobj = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var txs = jobj["data"]["txs"];

                    if (txs is JArray txArr && txArr.Count > 0)
                    {
                        var shouldBreak = false;

                        try
                        {
                            shouldBreak = onReceiveAnyTx(GetTransactionsFromJArray(txArr));
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(
                                $"ERRO: Falha no callback ({nameof(NotifyTransactionsOfAAddress)}):" + e
                            );
                        }

                        if (shouldBreak)
                            break;
                    }
                }
            })
            {
                Name = $"so-chain-addr-checker: {address}",
                IsBackground = true
            };

            thread.Start();

            return new BackgroundJob(thread);
        }

        private IEnumerable<Transaction> GetTransactionsFromJArray(JArray txArr)
        {
            foreach (var tx in txArr)
            {
                yield return new Transaction
                {
                    Confirmations = tx.Value<uint>("confirmations"),
                    Value = tx.Value<decimal>("value"),
                    Id = tx.Value<string>("txid")
                };
            }
        }
    }
}