using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using CandyMachineV2;
using CandyMachineV2.Program;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Solnet.Metaplex;
using UnityEngine;
using System;
using System.Diagnostics;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Solana.Unity.SDK.Example
{

    public class testBlock : SimpleWallet
    {
        public Button active;
        public string tokenUri = "https://gateway.pinata.cloud/ipfs/QmbEjgeaDbgKBhdVMiw2akMDJjn8vEvLaBLzr74r6wPv4L";
        // Start is called before the first frame update
        void Start()
        {
            active.onClick.AddListener(ButtonClicked);
        }




        private async void ButtonClicked()
        {
            var signature = await activeTest();
            UnityEngine.Debug.Log("End!");
        }


        public async Task<string> activeTest()
        {


            Account tokenAccount = new Account();
            Account mintAccount = new Account();



            var walletHolderService = SimpleWallet.Instance;
            var wallet = walletHolderService.Wallet;
            var rpcClient = walletHolderService.Wallet.ActiveRpcClient;
            var fromAccount = new Account(new Account().PrivateKey.KeyBytes,
                walletHolderService.Wallet.Account.PublicKey.KeyBytes);
            RequestResult<ResponseValue<ulong>> balance =
            await rpcClient.GetBalanceAsync(wallet.Account.PublicKey, Commitment.Confirmed);
            var blockHash = await rpcClient.GetRecentBlockHashAsync();
            var rentMint = await rpcClient.GetMinimumBalanceForRentExemptionAsync(
    TokenProgram.MintAccountDataSize,
    Commitment.Confirmed
);
     
            var createMintAccount = SystemProgram.CreateAccount(
                fromAccount,
                mintAccount,
                rentMint.Result,
                TokenProgram.MintAccountDataSize,
                TokenProgram.ProgramIdKey
            );


            var initializeMint = TokenProgram.InitializeMint(
                mintAccount.PublicKey,
                9,
                fromAccount.PublicKey
            );

            var initializeMintAccount = TokenProgram.InitializeAccount(
            tokenAccount.PublicKey,
                mintAccount.PublicKey,
                fromAccount.PublicKey
            );

            var mintTo = TokenProgram.MintTo(
                mintAccount.PublicKey,
            tokenAccount,
                100,
                fromAccount.PublicKey
            );
            // PDA Metadata
            PublicKey metadataAddressPDA;
            byte nonce;
            PublicKey.TryFindProgramAddress(
                new List<byte[]>()
                {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount.PublicKey
                },
                MetadataProgram.ProgramIdKey,
                out metadataAddressPDA,
                out nonce
            );
            Console.WriteLine($"PDA METADATA: {metadataAddressPDA}");

            PublicKey masterEditionAddress;

            PublicKey.TryFindProgramAddress(
                new List<byte[]>()
                {
                    Encoding.UTF8.GetBytes("metadata"),
                    MetadataProgram.ProgramIdKey,
                    mintAccount.PublicKey,
                    Encoding.UTF8.GetBytes("edition")
                },
                MetadataProgram.ProgramIdKey,
                out masterEditionAddress,
                out nonce
            );
            Console.WriteLine($"PDA MASTER: {masterEditionAddress}");


            var creator1 = new Creator(fromAccount.PublicKey, 100, true);
            var data = new MetadataV1()
            {
                name = "NFT Asset Name #8",
                symbol = "NFT",
                uri = tokenUri,
                creators = new List<Creator>() { creator1 },
                sellerFeeBasisPoints = 77,
            };
            var signers = new List<Account> { fromAccount, mintAccount, tokenAccount };


            var transactionBuilder= new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(createMintAccount)
                .AddInstruction(initializeMint)
                .AddInstruction(initializeMintAccount)
                .AddInstruction(mintTo)
                .AddInstruction(
                    MetadataProgram.CreateMetadataAccount(
                        metadataAddressPDA, // PDA
                        mintAccount,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey, // update Authority 
                        data, // DATA
                        true,
                        true // ISMUTABLE
                    )
                )
                .AddInstruction(
                    MetadataProgram.SignMetada(
                        metadataAddressPDA,
                        creator1.key
                    )
                )
                .AddInstruction(
                    MetadataProgram.PuffMetada(
                        metadataAddressPDA
                    )
                )
                .AddInstruction(
                    MetadataProgram.CreateMasterEdition(
                        1,
                        masterEditionAddress,
                        mintAccount,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        fromAccount.PublicKey,
                        metadataAddressPDA
                    )
                );
            byte[] transaction = transactionBuilder.Build(signers);
            Transaction deserializedTransaction = Transaction.Deserialize(transaction);
            Transaction signedTransaction =
                await walletHolderService.Wallet.SignTransaction(deserializedTransaction);

            // This is a bit hacky, but in case of phantom wallet we need to replace the signature with the one that 
            // phantom produces
            signedTransaction.Signatures[0] = signedTransaction.Signatures[3];
            signedTransaction.Signatures.RemoveAt(3);

            var transactionSignature =
                await walletHolderService.Wallet.ActiveRpcClient.SendTransactionAsync(
                    Convert.ToBase64String(signedTransaction.Serialize()), true, Commitment.Confirmed);

            if (!transactionSignature.WasSuccessful)
            {
                UnityEngine.Debug.Log("not successfull: " + transactionSignature.Reason);
            }
            else
            {

                UnityEngine.Debug.Log("Successfull! Woop woop!");

            }

            UnityEngine.Debug.Log(transactionSignature.Result);
            return transactionSignature.Result;

        }


        // Update is called once per frame
        void Update()
        {

        }
    }

}

