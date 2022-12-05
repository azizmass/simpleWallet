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


namespace Solana.Unity.SDK.Example {
    public class mintNFT : SimpleWallet
    {


        public Button MintBtn;
        // Start is called before the first frame update
        void Start()
        {
            MintBtn.onClick.AddListener(OnMintInAppButtonClicked);
        }


        private async void OnMintInAppButtonClicked()
        {
            // Mint a baloon beaver
            var signature = await MintNftWithMetaData(
                    "https://arweave.net/ek7PwinRZ9EJ-XpjuhltlnxiMoZ5ZBdCrRLjsbF3VCI",
                    "Balloon Beaver", "Beaver");
            UnityEngine.Debug.Log(signature);
        }
    public async Task<string> MintNFTFromCandyMachineV2(PublicKey candyMachineKey) {
            var baseWallet = SimpleWallet.Instance.Wallet;

            var account = baseWallet.Account;

            Account mint = new Account();

            PublicKey associatedTokenAccount =
                AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(account, mint.PublicKey);

            var candyMachineClient = new CandyMachineClient(baseWallet.ActiveRpcClient, null);
            var candyMachineWrap = await candyMachineClient.GetCandyMachineAsync(candyMachineKey);
            var candyMachine = candyMachineWrap.ParsedResult;


            var (candyMachineCreator, creatorBump) = CandyMachineUtils.getCandyMachineCreator(candyMachineKey);

            MintNftAccounts mintNftAccounts = new MintNftAccounts
            {
                CandyMachine = candyMachineKey,
                CandyMachineCreator = candyMachineCreator,
                Clock = SysVars.ClockKey,
                InstructionSysvarAccount = CandyMachineUtils.instructionSysVarAccount,
                MasterEdition = CandyMachineUtils.getMasterEdition(mint.PublicKey),
                Metadata = CandyMachineUtils.getMetadata(mint.PublicKey),
                Mint = mint.PublicKey,
                MintAuthority = account,
                Payer = account,
                RecentBlockhashes = SysVars.RecentBlockHashesKey,
                Rent = SysVars.RentKey,
                SystemProgram = SystemProgram.ProgramIdKey,
                TokenMetadataProgram = CandyMachineUtils.TokenMetadataProgramId,
                TokenProgram = TokenProgram.ProgramIdKey,
                UpdateAuthority = account,
                Wallet = candyMachine.Wallet
            };

            var candyMachineInstruction = CandyMachineProgram.MintNft(mintNftAccounts, creatorBump);

            var blockHash = await baseWallet.ActiveRpcClient.GetRecentBlockHashAsync();
            var minimumRent =
                await baseWallet.ActiveRpcClient.GetMinimumBalanceForRentExemptionAsync(
                    TokenProgram.MintAccountDataSize);

            var serializedTransaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(account)
                .AddInstruction(
                    SystemProgram.CreateAccount(
                        account,
                        mint.PublicKey,
                        minimumRent.Result,
                        TokenProgram.MintAccountDataSize,
                        TokenProgram.ProgramIdKey))
                .AddInstruction(
                    TokenProgram.InitializeMint(
                        mint.PublicKey,
                        0,
                        account,
                        account))
                .AddInstruction(
                    AssociatedTokenAccountProgram.CreateAssociatedTokenAccount(
                        account,
                        account,
                        mint.PublicKey))
                .AddInstruction(
                    TokenProgram.MintTo(
                        mint.PublicKey,
                        associatedTokenAccount,
                        1,
                        account))
                .AddInstruction(candyMachineInstruction)
                .Build(new List<Account>()
                {
                    account,
                    mint
                });

            Transaction deserializedTransaction = Transaction.Deserialize(serializedTransaction);

            UnityEngine.Debug.Log($"mint transaction length {serializedTransaction.Length}");

            var signedTransaction = await baseWallet.SignTransaction(deserializedTransaction);

            // This is a bit hacky, but in case of phantom wallet we need to replace the signature with the one that 
            // phantom produces
            signedTransaction.Signatures[0] = signedTransaction.Signatures[2];
            signedTransaction.Signatures.RemoveAt(2);
            // var simulation = await baseWallet.ActiveRpcClient.SimulateTransactionAsync(Convert.ToBase64String(signedTransaction.Serialize()));

            //return simulation.Reason;
            var transactionSignature =
                await baseWallet.ActiveRpcClient.SendTransactionAsync(
                    Convert.ToBase64String(signedTransaction.Serialize()), true, Commitment.Confirmed);

            if (!transactionSignature.WasSuccessful)
            {
                UnityEngine.Debug.Log("dont work!");
            }
            else
            {
                UnityEngine.Debug.Log("work!");
            }

            UnityEngine.Debug.Log(transactionSignature.Reason);
            return transactionSignature.Result;
        }


        public async Task<string> MintNftWithMetaData(string metaDataUri, string name, string symbol)
        {
            var walletHolderService = SimpleWallet.Instance ;
            var wallet = walletHolderService.Wallet;
            var rpcClient = walletHolderService.Wallet.ActiveRpcClient;

            Account mintAccount = new Account();
            Account tokenAccount = new Account();

            var fromAccount = walletHolderService.Wallet.Account;

            // To be able to sign the transaction while using the transaction builder we need to have a private key set in the signing account. 
            // TODO: I will try to make this nicer later. 
            fromAccount = new Account(new Account().PrivateKey.KeyBytes,
                walletHolderService.Wallet.Account.PublicKey.KeyBytes);

            RequestResult<ResponseValue<ulong>> balance =
                await rpcClient.GetBalanceAsync(wallet.Account.PublicKey, Commitment.Confirmed);

            // TODO: Check if there is enough sol in the wallet to mint. 
            if (balance.Result != null && balance.Result.Value < SolanaUtils.SolToLamports / 10)
            {
                UnityEngine.Debug.Log("Sol balance is low. Minting may fail");
            }

            UnityEngine.Debug.Log($"Balance: {balance.Result.Value} ");
            UnityEngine.Debug.Log($"Mint key : {mintAccount.PublicKey} ");

            var blockHash = await rpcClient.GetRecentBlockHashAsync();
            var rentMint = await rpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.MintAccountDataSize,
                Commitment.Confirmed
            );
            var rentToken = await rpcClient.GetMinimumBalanceForRentExemptionAsync(
                TokenProgram.TokenAccountDataSize,
                Commitment.Confirmed
            );

            UnityEngine.Debug.Log($"Token key : {tokenAccount.PublicKey} ");

            //2. create a mint and a token
            var createMintAccount = SystemProgram.CreateAccount(
                fromAccount,
                mintAccount,
                rentMint.Result,
                TokenProgram.MintAccountDataSize,
                TokenProgram.ProgramIdKey
            );
            var initializeMint = TokenProgram.InitializeMint(
                mintAccount.PublicKey,
                0,
                fromAccount.PublicKey,
                fromAccount.PublicKey
            );
            var createTokenAccount = SystemProgram.CreateAccount(
                fromAccount,
                tokenAccount,
                rentToken.Result,
                TokenProgram.TokenAccountDataSize,
                TokenProgram.ProgramIdKey
            );
            var initializeMintAccount = TokenProgram.InitializeAccount(
                tokenAccount.PublicKey,
                mintAccount.PublicKey,
                fromAccount.PublicKey
            );

            var mintTo = TokenProgram.MintTo(
                mintAccount.PublicKey,
                tokenAccount,
                1,
                fromAccount.PublicKey
            );

            var freezeAccount = TokenProgram.FreezeAccount(
                tokenAccount,
                mintAccount,
                fromAccount,
                TokenProgram.ProgramIdKey
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

            // PDA master edition (Makes sure there can only be one minted) 
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

            // Craetors
            var creator1 = new Creator(fromAccount.PublicKey, 100);

            // Meta Data
            var data = new MetadataV1()
            {
                name = name,
                symbol = symbol,
                uri = metaDataUri,
                creators = new List<Creator>() {creator1},
                sellerFeeBasisPoints = 77,
            };

            var signers = new List<Account> {fromAccount, mintAccount, tokenAccount};
            var transactionBuilder = new TransactionBuilder()
                .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                .SetFeePayer(fromAccount)
                .AddInstruction(createMintAccount)
                .AddInstruction(initializeMint)
                .AddInstruction(createTokenAccount)
                .AddInstruction(initializeMintAccount)
                .AddInstruction(mintTo)
                .AddInstruction(freezeAccount)
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
               UnityEngine.Debug.Log("Mint was not successfull: " + transactionSignature.Reason);
            }
            else
            {
               
                       UnityEngine.Debug.Log("Mint Successfull! Woop woop!");
                   
            }

            UnityEngine.Debug.Log(transactionSignature.Reason);
            return transactionSignature.Result;
        }
    // Update is called once per frame
    void Update()
    {
        
    }
}
}
