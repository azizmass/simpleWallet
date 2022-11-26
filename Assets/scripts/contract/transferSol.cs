using System;
using System.Diagnostics;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class transferSol : SimpleScreen
    {
        public TextMeshProUGUI errorTxt;
        public TMP_InputField toPublicTxt;
        public TMP_InputField amountTxt;
        public Button transferBtn;
        public Button transferNFT;
        public TMP_InputField index_nft;

        private TokenAccount _transferTokenAccount;
        private Nft.Nft _nft;
        private double _ownedSolAmount;

        private const long SolLamports = 1000000000;

        private void Start()
        {
            transferBtn.onClick.AddListener(TryTransferSol);
            transferNFT.onClick.AddListener(TransferNft);

        }

        private void TryTransferSol()
        {
                if (CheckInput())
                    TransferSol();
          
        }


        private async void TransferSol()
        {       
            RequestResult<string> result = await SimpleWallet.Instance.Wallet.Transfer(
                new PublicKey(toPublicTxt.text),
                Convert.ToUInt64(float.Parse(amountTxt.text) * SolLamports));
            UnityEngine.Debug.Log(result);
            
        }

        private async void TransferNft()
        {
            var NFTs = await SimpleWallet.Instance.Wallet.GetTokenAccounts();
            int index = int.Parse(index_nft.text);
            var nft = await Nft.Nft.TryGetNftData(NFTs[index].Account.Data.Parsed.Info.Mint, SimpleWallet.Instance.Wallet.ActiveRpcClient);
            RequestResult<string> result = await SimpleWallet.Instance.Wallet.Transfer(
                new PublicKey(toPublicTxt.text),
                new PublicKey(nft.metaplexData.mint),
                1);
            UnityEngine.Debug.Log(result);

        }

        bool CheckInput()
        {
            if (string.IsNullOrEmpty(amountTxt.text))
            {
                errorTxt.text = "Please input transfer amount";
                return false;
            }

            if (string.IsNullOrEmpty(toPublicTxt.text))
            {
                errorTxt.text = "Please enter receiver public key";
                return false;
            }

            

            errorTxt.text = "";
            return true;
        }




        public override async void ShowScreen(object data = null)
        {
            base.ShowScreen();

            ResetInputFields();
            gameObject.SetActive(true);
        }

        public void OnClose()
        {
            var wallet = GameObject.Find("wallet");
            wallet.SetActive(false);
        }



        private void ResetInputFields()
        {
            errorTxt.text = "";
            amountTxt.text = "";
            toPublicTxt.text = "";
            amountTxt.interactable = true;
        }

        public override void HideScreen()
        {
            base.HideScreen();
            _transferTokenAccount = null;
            gameObject.SetActive(false);
        }
    }

}


