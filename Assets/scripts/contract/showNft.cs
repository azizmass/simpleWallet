using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using codebase.utility;
using System.Threading.Tasks;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class showNft : SimpleScreen
    {

        [SerializeField]
        private Button refreshBtn;
        [SerializeField]
        private Button logoutBtn;

        [SerializeField]
        private GameObject tokenItem;
        [SerializeField]
        private Transform tokenContainer;
        [SerializeField]
        private TextMeshProUGUI lamports;

        private CancellationTokenSource _stopTask;
        private List<GameObject> _instantiatedTokens;

        public void Start()
        {


            _instantiatedTokens = new List<GameObject>();
            WebSocketActions.WebSocketAccountSubscriptionAction += (bool istrue) =>
            {
                MainThreadDispatcher.Instance().Enqueue(UpdateWalletBalanceDisplay);
            };
            WebSocketActions.CloseWebSocketConnectionAction += DisconnectToWebSocket;
            refreshBtn.onClick.AddListener(async () =>
            {
                UpdateWalletBalanceDisplay();
                await GetOwnedTokenAccounts();
            });


            logoutBtn.onClick.AddListener(() =>
            {
                SimpleWallet.Instance.Logout();
                print("disconnected");
            });

 

            _stopTask = new CancellationTokenSource();
        }

     


        private async void UpdateWalletBalanceDisplay()
        {
            if (SimpleWallet.Instance.Wallet.Account is null) return;
            double sol = await SimpleWallet.Instance.Wallet.GetBalance();
            MainThreadDispatcher.Instance().Enqueue(() => { lamports.text = $"{sol}"; });
        }

        private void DisconnectToWebSocket()
        {
            MainThreadDispatcher.Instance().Enqueue(() => { manager.ShowScreen(this, "login_screen"); });
            MainThreadDispatcher.Instance().Enqueue(() => { SimpleWallet.Instance.Wallet.Logout(); });
        }

        private async Task GetOwnedTokenAccounts()
        {
            var objs = GameObject.FindGameObjectsWithTag("NFT");
            var result = await SimpleWallet.Instance.Wallet.GetTokenAccounts();
            int i = 0;
            if (result is { Length: > 0 })
            {
                foreach (var item in result)
                {
                    if (i < objs.Length)
                    {
                        if (!(float.Parse(item.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)) continue;
                        var nft = await Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, SimpleWallet.Instance.Wallet.ActiveRpcClient);

                        objs[i].transform.localScale = Vector3.one;
                        _instantiatedTokens.Add(objs[i]);
                        objs[i].SetActive(true);
                        objs[i].GetComponent<oneNft>().InitializeData(item, this, nft);

                    }
                    i++;
                }
            }
        }

        private void DisableTokenItems()
        {
            if (_instantiatedTokens == null) return;
            foreach (GameObject token in _instantiatedTokens)
            {
                Destroy(token);
            }
            _instantiatedTokens.Clear();
        }

        public override void ShowScreen(object data = null)
        {
            base.ShowScreen();
            gameObject.SetActive(true);
            UpdateWalletBalanceDisplay();
#pragma warning disable CS4014
            GetOwnedTokenAccounts();
#pragma warning restore CS4014
        }

    

      




    }
}










