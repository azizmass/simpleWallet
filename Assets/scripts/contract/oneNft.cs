using Solana.Unity.Rpc.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace Solana.Unity.SDK.Example
{
    public class oneNft : MonoBehaviour
    {
        public TextMeshProUGUI pub_txt;
        public TextMeshProUGUI ammount_txt;

        public GameObject logo;

        TokenAccount tokenAccount;
        Nft.Nft nft;
        SimpleScreen parentScreen;

        private void Awake()
        {
            logo = GetComponentInChildren<GameObject>();
        }

        private void Start()
        {
            
        }

        public void InitializeData(TokenAccount tokenAccount, SimpleScreen screen, Solana.Unity.SDK.Nft.Nft nftData = null)
        {
    
            parentScreen = screen;
            this.tokenAccount = tokenAccount;
            if (nftData != null)
            {
                
                nft = nftData;
                ammount_txt.text = "";
                pub_txt.text = nftData.metaplexData.data.name;

                if (logo != null)
                {

                    logo.GetComponent<MeshRenderer>().material.mainTexture = nftData.metaplexData.nftImage.file;
                    
                    
                }
            }
            else
            {
                ammount_txt.text = tokenAccount.Account.Data.Parsed.Info.TokenAmount.Amount.ToString();

                if (logo is null) return;

                logo.gameObject.SetActive(false);
                pub_txt.text = tokenAccount.Account.Data.Parsed.Info.Mint;
            }
        }

    }
}



