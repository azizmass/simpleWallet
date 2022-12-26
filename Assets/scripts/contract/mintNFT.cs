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
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using static UnityEditor.Progress;
using Solana.Unity.Programs.Models.TokenProgram;
using Solana.Unity.Wallet.Bip39;


namespace Solana.Unity.SDK.Example {
    public class mintNFT : SimpleWallet
    {

        [SerializeField]
        private RpcCluster rpcCluster = RpcCluster.MainNet;
        [HideIfEnumValue("rpcCluster", HideIf.NotEqual, (int)RpcCluster.Custom)]
        public Button MintBtn;
        public Button active;
        public TextMeshProUGUI nftName;
        public string tokenUri = "https://gateway.pinata.cloud/ipfs/QmbEjgeaDbgKBhdVMiw2akMDJjn8vEvLaBLzr74r6wPv4L";
        public string name = "NFT Asset Name #1";
        public string symbol = "test";


        private const long SolLamports = 1000000000;
        // Start is called before the first frame update
        void Start()
        {
            
            MintBtn.onClick.AddListener(OnMintInAppButtonClicked);
            active.onClick.AddListener(btnBurn);
        }


        private async void OnMintInAppButtonClicked()
        {
            // Mint a baloon beaver
            var signature = await MintNftWithMetaData(
                    tokenUri,
                    name, symbol);
            UnityEngine.Debug.Log("End!");
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



        public async void btnBurn()
        {
           var res= await burnNFT();
        }


        public async Task<string> burnNFT()
        {
            string mnemonic = "loop cotton brother trial mango hire narrow bullet apart drive flavor concert";
            string password = "200228azerty000";

            var nfts = await SimpleWallet.Instance.Wallet.GetTokenAccounts();

 if (nfts is { Length: > 0 })
 {
     foreach (var item in nfts)
     {

             if (!(float.Parse(item.Account.Data.Parsed.Info.TokenAmount.Amount) > 0)) continue;
             var nft = await Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, SimpleWallet.Instance.Wallet.ActiveRpcClient);
                    UnityEngine.Debug.Log(nft.metaplexData.mint);
             if(nft.metaplexData.data.name== "NFT Asset Name #8")
         {
             RequestResult<string> res = await SimpleWallet.Instance.Wallet.Transfer(
             new PublicKey("4o7SiJ3ophJnvba1fYenENcnrNyGmJeA6D1YQf3KLEH6"),
             new PublicKey(nft.metaplexData.mint),
             1);
                        UnityEngine.Debug.Log(res.WasSuccessful);
                        UnityEngine.Debug.Log(res.Reason);
                        UnityEngine.Debug.Log(res.ServerErrorCode);
                        break;
                        
         }
     }


 }
            /*
             *             var Wallet = new InGameWallet(rpcCluster, customRpc, autoConnectOnStartup);
                        var account= await Wallet.CreateAccount(mnemonic, password);
             RequestResult<string> result = await Wallet.Transfer(SimpleWallet.Instance.Wallet.Account.PublicKey,
                 Convert.ToUInt64(float.Parse("0.0001") * SolLamports)
                 );
             UnityEngine.Debug.Log(result.WasSuccessful);
             UnityEngine.Debug.Log(result.Reason);
             UnityEngine.Debug.Log(result.ServerErrorCode);*/

            return "";
        }


        /*public async Task<string> burnNFT()
        {


          var result = await SimpleWallet.Instance.Wallet.GetTokenAccounts();


            foreach(var item in result)
            {

                

                var nft = await Nft.Nft.TryGetNftData(item.Account.Data.Parsed.Info.Mint, SimpleWallet.Instance.Wallet.ActiveRpcClient);
                UnityEngine.Debug.Log(nft.metaplexData.data.name);
                if(nft.metaplexData.data.name== "NFT Asset Name #8"){
                    

                    var walletHolderService = SimpleWallet.Instance;
                    var wallet = walletHolderService.Wallet;
                    var rpcClient = walletHolderService.Wallet.ActiveRpcClient;
                    var fromAccount = new Account(new Account().PrivateKey.KeyBytes,
                        walletHolderService.Wallet.Account.PublicKey.KeyBytes);
                    var tokenkey = new Account(fromAccount.PrivateKey.Key, item.PublicKey);
                    var blockHash = await rpcClient.GetRecentBlockHashAsync();


                    var burn = TokenProgram.Burn(fromAccount.PublicKey ,
                        tokenkey,
                     1,
                    fromAccount.PublicKey
                       );

                    var signers = new List<Account> { fromAccount,tokenkey };
                    var transactionBuilder = new TransactionBuilder()
                            .SetRecentBlockHash(blockHash.Result.Value.Blockhash)
                            .SetFeePayer(fromAccount)
                            .AddInstruction(burn);

                    byte[] transaction = transactionBuilder.Build(signers);
                    Transaction deserializedTransaction = Transaction.Deserialize(transaction);
                    Transaction signedTransaction =
                        await walletHolderService.Wallet.SignTransaction(deserializedTransaction);

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
                    break;
                }
            }



            return null;
            
        }

        */






        public async Task<string> MintNftWithMetaData(string metaDataUri, string name, string symbol)
        {
            var walletHolderService = SimpleWallet.Instance;
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
                creators = new List<Creator>() { creator1 },
                sellerFeeBasisPoints = 77,
            };

            var signers = new List<Account> { fromAccount, mintAccount, tokenAccount };
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
                var aziz = new Account("3fPx3cq9DSAvjtAdjV6zg63yVtmj8FsA8sQ9y7MdEhvVC23S75KH6FA8oPLwJWxiptRqX7gMP2dQftLecHa6X6uv", "4o7SiJ3ophJnvba1fYenENcnrNyGmJeA6D1YQf3KLEH6");
                RequestResult<string> result = await SimpleWallet.Instance.Wallet.Transfer(
                 aziz.PublicKey,
                    Convert.ToUInt64(float.Parse("0.0001") * SolLamports));
                UnityEngine.Debug.Log(result);
                UnityEngine.Debug.Log("Mint was not successfull: " + transactionSignature.Reason);
            }
            else
            {

                UnityEngine.Debug.Log("Mint Successfull! Woop woop!");

            }

            UnityEngine.Debug.Log(transactionSignature.Reason);
            return transactionSignature.Result;
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
            var rentToken = await rpcClient.GetMinimumBalanceForRentExemptionAsync(
    TokenProgram.TokenAccountDataSize,
    Commitment.Confirmed
);
            var createTokenAccount = SystemProgram.CreateAccount(
      fromAccount,
      tokenAccount,
      rentToken.Result,
      TokenProgram.TokenAccountDataSize,
      TokenProgram.ProgramIdKey
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


            var transactionBuilder = new TransactionBuilder()
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
