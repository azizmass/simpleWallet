using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Diagnostics;

namespace Solana.Unity.SDK.Example
{
    public class connectionwallet : SimpleScreen

    {
        [SerializeField]
        private TMP_InputField mnemonicTxt;   
        [SerializeField]
        private TMP_InputField passwordInputField;
        [SerializeField]
        private Button generateBtn;
        [SerializeField]
        private Button loginBtnPhantom;

        // Start is called before the first frame update
        void Start()
        {
            generateBtn.onClick.AddListener(GenerateNewAccount);
            loginBtnPhantom.onClick.AddListener(LoginCheckerPhantom);

        }

        private async void LoginCheckerPhantom()
        {
            var account = await SimpleWallet.Instance.LoginPhantom();
        }

        private async void GenerateNewAccount()
        {
            var password = passwordInputField.text;
            var mnemonic = mnemonicTxt.text;

            var account = await SimpleWallet.Instance.CreateAccount(mnemonic, password);
            if (account != null)
            {
                print(account);
            }
            else
            {
                print("can not connect");
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}