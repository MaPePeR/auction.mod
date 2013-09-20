using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using JsonFx.Json;
using ScrollsModLoader.Interfaces;
using UnityEngine;
using Mono.Cecil;
using Irrelevant.Assets;
using System.Net;
using System.IO;

namespace Auction.mod
{


    using System;
    using UnityEngine;

	public struct aucitem
	{
		public Card card;
		public string seller;
		public string price;
		public int priceinint;
		public string time;
		public DateTime dtime;
		public string whole;
	}

    struct nickelement
    {
        public string nick;
        public string cardname;
    }


    public class Auction : BaseMod, ICommListener
    {

        private bool hidewispers = true; //  false = testmodus


         // = realcardnames + loadedscrollsnicks
        private string[] aucfiles;
        int screenh = 0;
        int screenw = 0;
        private const bool debug = false;
        bool deckchanged = false;
        private FieldInfo chatLogStyleinfo;
        private MethodInfo drawsubmenu;

        //Texture2D arrowdown = ResourceManager.LoadTexture("ChatUI/dropdown_arrow");

        Settings sttngs;
        Network ntwrk;
        Searchsettings srchsvr;
        cardviewer crdvwr;
        Prices prcs;
        listfilters lstfltrs;
        messageparser mssgprsr;
        Rectomat recto;
        auclists alists;
        Helpfunktions helpf;
        AuctionHouseUI ahui;
        GeneratorUI genui;
        settingsUI setui;
        
        

        public void handleMessage(Message msg)
        {

            if (msg is BuyStoreItemResponseMessage)
            {
                // if we buy a card in the store, we have to reload the own cards , nexttime we open ah/generator
                List<Card> boughtCards = null;
                BuyStoreItemResponseMessage buyStoreItemResponseMessage = (BuyStoreItemResponseMessage)msg;
                if (buyStoreItemResponseMessage.cards.Length > 0)
                {
                    boughtCards = new List<Card>(buyStoreItemResponseMessage.cards);
                }
                else
                {
                    boughtCards = null;
                }
                DeckInfo deckInfo = buyStoreItemResponseMessage.deckInfo;
                if (boughtCards != null)
                {
                    deckchanged = true;
                }
                else
                {
                    if (deckInfo != null)
                    {
                        deckchanged = true; 
                    }
                }
            }



            if (msg is TradeResponseMessage)
            {
                //if he doesnt accept the trade, reset the variables
                TradeResponseMessage trm = (TradeResponseMessage)msg;
                if (trm.status != "ACCEPT")
                {
                    helpf.postmsgontrading = false;
                    helpf.postmsggetnextroomenter = false;
                    helpf.postmsgmsg = "";
                }
            }


            if (msg is RoomEnterMessage && helpf.postmsggetnextroomenter)
            {// he accept your trade, post the auction message to yourself
                RoomEnterMessage rmem = (RoomEnterMessage)msg;
                if (rmem.roomName.StartsWith("trade-"))
                {
                    helpf.postmsggetnextroomenter = false;
                    // post the msg here!:
                    RoomChatMessageMessage joinmessage = new RoomChatMessageMessage(rmem.roomName, "<color=#777460>" + helpf.postmsgmsg + "</color>");
                    joinmessage.from = "Scrolls";

                    //App.ChatUI.handleMessage(new RoomChatMessageMessage(rmem.roomName, "<color=#777460>" + postmsgmsg + "</color>"));
                    App.ArenaChat.ChatRooms.ChatMessage(joinmessage);
                    helpf.postmsgmsg = "";
                }
            }

            if (msg is GameInfoMessage && ntwrk.contonetwork)
            {// you are connected to network and start a battle -> disconnect
                GameInfoMessage gim =(GameInfoMessage) msg;
                if (ntwrk.inbattle == false) { ntwrk.inbattle = true; ntwrk.disconfromaucnet(); Console.WriteLine("discon"); }
            }

            if (msg is RoomInfoMessage && ntwrk.contonetwork)
            {
                // you enter a auc-x room , while connected to network... so do communication stuff, like adding the users etc
                RoomInfoMessage roominfo = (RoomInfoMessage)msg;
                if (roominfo.roomName.StartsWith("auc-"))
                {
                    ntwrk.enteraucroom(roominfo);
                }
            
            }

            if ( msg is FailMessage)
            {   // delete user if he cant be whispered ( so he doesnt check out propperly... blame on him!)
                FailMessage fm = (FailMessage)msg;
                if (ntwrk.idtesting > 0)
                {

                    if (fm.op == "ProfilePageInfo") { ntwrk.idtesting--; };
                }
                if (fm.op == "Whisper" && fm.info.StartsWith("Could not find the user "))
                {
                    string name = "";
                    name = (fm.info).Split('\'')[1];
                    //Console.WriteLine("could not find: " + name);

                }
            }

            if (ntwrk.idtesting > 0 && msg is ProfilePageInfoMessage)//doesnt needed anymore
            {
                ProfilePageInfoMessage ppim = (ProfilePageInfoMessage)msg;
                ChatUser newuser = new ChatUser();
                newuser.acceptChallenges = false;
                newuser.acceptTrades = true;
                newuser.adminRole = AdminRole.None;
                newuser.name = ppim.name;
                newuser.id = ppim.id;
                if (!helpf.globalusers.ContainsKey(newuser.name)) { helpf.globalusers.Add(newuser.name, newuser); ntwrk.addglobalusers(newuser); }
                ntwrk.adduser(newuser);

            }

            if (msg is CardTypesMessage)
            {

                // get all available cards, save them!
                helpf.setarrays(msg);
                prcs.resetarrays(helpf.cardids.Length);
                if (helpf.nicks) helpf.readnicksfromfile();
                mssgprsr.searchscrollsnicks.Clear();
                prcs.wtbpricelist1.Clear();
                lstfltrs.allcardsavailable.Clear();
                for (int j = 0; j < helpf.cardnames.Length; j++)
                {
                    prcs.wtbpricelist1.Add(helpf.cardnames[j].ToLower(), "");
                    CardType type = CardTypeManager.getInstance().get(helpf.cardids[j]);
                    Card card = new Card(helpf.cardids[j], type, true);
                    aucitem ai = new aucitem();
                    ai.card = card;
                    ai.price = "";
                    ai.priceinint = lstfltrs.allcardsavailable.Count;
                    ai.seller="me";
                    lstfltrs.allcardsavailable.Add(ai);
                    nickelement nele;
                    nele.nick = helpf.cardnames[j];
                    nele.cardname = helpf.cardnames[j];
                    mssgprsr.searchscrollsnicks.Add(nele);
                };
                mssgprsr.searchscrollsnicks.AddRange(helpf.loadedscrollsnicks);

                lstfltrs.allcardsavailable.Sort(delegate(aucitem p1, aucitem p2) { return (p1.card.getName()).CompareTo(p2.card.getName()); });
                prcs.totalpricecheck();//helpf.cardids
            }

            return;
        }
        public void onReconnect()
        {
            return; // don't care
        }

        public Auction()
        {
            helpf = new Helpfunktions();
            helpf.setskins((GUISkin)Resources.Load("_GUISkins/CardListPopup"), (GUISkin)Resources.Load("_GUISkins/CardListPopupGradient"), (GUISkin)Resources.Load("_GUISkins/CardListPopupBigLabel"), (GUISkin)Resources.Load("_GUISkins/CardListPopupLeftButton"));

            sttngs = new Settings();
            ntwrk = new Network();
            srchsvr = new Searchsettings();
            Console.WriteLine("saveall");
            srchsvr.saveall();
            Console.WriteLine("savealldone");
            crdvwr = new cardviewer();
            prcs = new Prices(helpf);
            lstfltrs = new listfilters(srchsvr, prcs);
            recto = new Rectomat();
            alists = new auclists(lstfltrs, prcs, srchsvr);
            mssgprsr = new messageparser(alists, lstfltrs, this.sttngs, this.helpf);
            ahui = new AuctionHouseUI(mssgprsr,alists,recto,lstfltrs,prcs,crdvwr,srchsvr,ntwrk,sttngs,this.helpf);
            genui = new GeneratorUI(mssgprsr, alists, recto, lstfltrs, prcs, crdvwr, srchsvr, ntwrk, sttngs, this.helpf);
            setui = new settingsUI(mssgprsr, alists, recto, lstfltrs, prcs, crdvwr, srchsvr, ntwrk, sttngs, this.helpf);

            helpf.hideInformationinfo = typeof(Store).GetMethod("hideInformation", BindingFlags.Instance | BindingFlags.NonPublic);
            helpf.showBuyinfo = typeof(Store).GetField("showBuy", BindingFlags.Instance | BindingFlags.NonPublic);
            helpf.showSellinfo = typeof(Store).GetField("showSell", BindingFlags.Instance | BindingFlags.NonPublic);

            drawsubmenu = typeof(Store).GetMethod("drawSubMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            chatLogStyleinfo = typeof(ChatUI).GetField("chatMsgStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            helpf.targetchathightinfo = typeof(ChatUI).GetField("targetChatHeight", BindingFlags.Instance | BindingFlags.NonPublic);
            
            helpf.buymen = typeof(Store).GetField("buyMenuObj", BindingFlags.Instance | BindingFlags.NonPublic);
            helpf.sellmen = typeof(Store).GetField("sellMenuObj", BindingFlags.Instance | BindingFlags.NonPublic);

            
            
            Directory.CreateDirectory(helpf.ownaucpath);
            this.aucfiles = Directory.GetFiles(helpf.ownaucpath, "*auc.txt");
            if (aucfiles.Contains(helpf.ownaucpath + "wtsauc.txt"))//File.Exists() was slower
            {
                helpf.wtsmsgload = true;
            }
            if (aucfiles.Contains(helpf.ownaucpath + "wtbauc.txt"))//File.Exists() was slower
            {
                helpf.wtbmsgload = true;
            }
            if (aucfiles.Contains(helpf.ownaucpath + "nicauc.txt"))//File.Exists() was slower
            {
                helpf.nicks = true;
            }

            if (aucfiles.Contains(helpf.ownaucpath + "settingsauc.txt"))//File.Exists() was slower
            {
                sttngs.loadsettings(helpf.ownaucpath);
            }



            try
            {
                App.Communicator.addListener(this);
            }
            catch { }

        }

        public static string GetName()
        {
            return "auc";
        }

        public static int GetVersion()
        {
            return 4;
        }

        public static MethodDefinition[] GetHooks(TypeDefinitionCollection scrollsTypes, int version)
        {
            try
            {
                return new MethodDefinition[] {
                    scrollsTypes["ChatRooms"].Methods.GetMethod("SetRoomInfo", new Type[] {typeof(RoomInfoMessage)}),
                    scrollsTypes["ChatUI"].Methods.GetMethod("Initiate")[0],
                    scrollsTypes["ChatUI"].Methods.GetMethod("Show", new Type[]{typeof(bool)}),
                    scrollsTypes["Store"].Methods.GetMethod("OnGUI")[0],
                    scrollsTypes["ChatRooms"].Methods.GetMethod("ChatMessage", new Type[]{typeof(RoomChatMessageMessage)}),
                   scrollsTypes["ArenaChat"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),
                   //scrollsTypes["Lobby"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),
                   scrollsTypes["BattleMode"].Methods.GetMethod("_handleMessage", new Type[]{typeof(Message)}),
                   scrollsTypes["Store"].Methods.GetMethod("Start")[0],
                    scrollsTypes["Store"].Methods.GetMethod("showSellMenu")[0],
                     scrollsTypes["Store"].Methods.GetMethod("showBuyMenu")[0],
                     scrollsTypes["Store"].Methods.GetMethod("handleMessage", new Type[]{typeof(Message)}),
                     scrollsTypes["TradeSystem"].Methods.GetMethod("StartTrade", new Type[]{typeof(List<Card>) , typeof(List<Card>), typeof(string), typeof(string), typeof(int)}),
                     scrollsTypes["EndGameScreen"].Methods.GetMethod("GoToLobby")[0],
                     
                    // only for testing:
                    //scrollsTypes["Communicator"].Methods.GetMethod("sendRequest", new Type[]{typeof(Message)}),  
                };
            }
            catch
            {
                return new MethodDefinition[] { };
            }
        }

        public override bool WantsToReplace(InvocationInfo info)
        {
            if (info.target is Store && info.targetMethod.Equals("OnGUI"))
            {
                if (helpf.inauchouse || helpf.generator || helpf.settings) return true;
            }
            
            if (info.target is BattleMode && info.targetMethod.Equals("_handleMessage"))
            {
                Message msg = (Message)info.arguments[0];
                if (msg is WhisperMessage)
                {
                    WhisperMessage wmsg = (WhisperMessage)msg;
                    if ((wmsg.text).StartsWith("aucdeletes") || (wmsg.text).StartsWith("aucdeleteb") || (wmsg.text).StartsWith("aucupdate") || (wmsg.text).StartsWith("aucto1please") || (wmsg.text).StartsWith("aucstay? ") || (wmsg.text).StartsWith("aucstay! ") || (wmsg.text).StartsWith("aucrooms ") || (wmsg.text).StartsWith("aucstop") || (wmsg.text).StartsWith("aucs ") || (wmsg.text).StartsWith("aucb ") || (wmsg.text).StartsWith("needaucid") || (wmsg.text).StartsWith("aucid ")) return true;
                }
            }
            
            if (info.target is ArenaChat && info.targetMethod.Equals("handleMessage"))
            {
                Message msg = (Message)info.arguments[0];
                if (msg is WhisperMessage)
                {
                    WhisperMessage wmsg = (WhisperMessage)msg;
                    if (hidewispers)
                    { // hides all whisper messages from auc-mod
                        if ((wmsg.text).StartsWith("aucdeletes") || (wmsg.text).StartsWith("aucdeleteb") || (wmsg.text).StartsWith("aucupdate") || (wmsg.text).StartsWith("aucto1please") || (wmsg.text).StartsWith("aucstay? ") || (wmsg.text).StartsWith("aucstay! ") || (wmsg.text).StartsWith("aucrooms ") || (wmsg.text).StartsWith("aucstop") || (wmsg.text).StartsWith("aucs ") || (wmsg.text).StartsWith("aucb ") || (wmsg.text).StartsWith("needaucid") || (wmsg.text).StartsWith("aucid ")) return true;
                    }
                    else
                    {// show some whispers if not connected (testmode)
                        if (ntwrk.contonetwork)
                        {

                            if ((wmsg.text).StartsWith("aucdeletes") || (wmsg.text).StartsWith("aucdeleteb") || (wmsg.text).StartsWith("aucupdate") || (wmsg.text).StartsWith("aucto1please") || (wmsg.text).StartsWith("aucstay? ") || (wmsg.text).StartsWith("aucstay! ") || (wmsg.text).StartsWith("aucrooms ") || (wmsg.text).StartsWith("aucstop") || (wmsg.text).StartsWith("aucs ") || (wmsg.text).StartsWith("aucb ") || (wmsg.text).StartsWith("needaucid") || (wmsg.text).StartsWith("aucid ")) return true;
                        }
                        else
                        {
                            if ((wmsg.text).StartsWith("aucstop") || (wmsg.text).StartsWith("aucto1please")) return true;
                        }
                    }
                }
                if (msg is RoomChatMessageMessage)
                {
                    RoomChatMessageMessage rem = (RoomChatMessageMessage)msg;
                    if (ntwrk.contonetwork && rem.roomName.StartsWith("auc-")) return true;
                }

                if (msg is RoomEnterMessage)
                {   
                    RoomEnterMessage rem = (RoomEnterMessage) msg;
                    if (ntwrk.contonetwork && rem.roomName.StartsWith("auc-")) return true;
                }

                if (msg is RoomInfoMessage)
                {
                    RoomInfoMessage rem = (RoomInfoMessage)msg;
                    if (ntwrk.contonetwork && rem.roomName.StartsWith("auc-")) return true;
                }


            }
            /*if (info.target is Lobby && info.targetMethod.Equals("handleMessage"))
            {
                Message msg = (Message)info.arguments[0];

                if (msg is RoomEnterMessage)
                {
                    RoomEnterMessage rem = (RoomEnterMessage)msg;
                    if (this.contonetwork && rem.roomName.StartsWith("auc-")) return true;
                }

                if (msg is RoomInfoMessage)
                {
                    RoomInfoMessage rem = (RoomInfoMessage)msg;
                    if (this.contonetwork && rem.roomName.StartsWith("auc-")) return true;
                }


            }*/
            return false;
        }

        public override void ReplaceMethod(InvocationInfo info, out object returnValue)
        {
            returnValue = null;
            if (info.target is ArenaChat && info.targetMethod.Equals("handleMessage"))
            {
                
                Message msg = (Message)info.arguments[0];
                if (msg is WhisperMessage)
                {
                    WhisperMessage wmsg = (WhisperMessage)msg;
                    string text = wmsg.text;

                    if (text.StartsWith("aucdeletes"))
                    {

                            alists.wtslistfulltimed.RemoveAll(element => element.seller == wmsg.from);
                            alists.wtslistfull.RemoveAll(element => element.seller == wmsg.from);
                            alists.wtslist.RemoveAll(element => element.seller == wmsg.from);


                        

                    }
                    if (text.StartsWith("aucdeleteb"))
                    {

                        alists.wtblistfulltimed.RemoveAll(element => element.seller == wmsg.from);
                        alists.wtblistfull.RemoveAll(element => element.seller == wmsg.from);
                        alists.wtblist.RemoveAll(element => element.seller == wmsg.from);
                    

                    }

                    if (text.StartsWith("aucs ") || text.StartsWith("aucb "))
                    {
                        mssgprsr.getaucitemsformmsg(text, wmsg.from, wmsg.GetChatroomName(), helpf.generator, helpf.inauchouse, helpf.settings, helpf.wtsmenue);
                        //need playerid (wispering doesnt send it)
                        if (!helpf.globalusers.ContainsKey(wmsg.from)) { WhisperMessage needid = new WhisperMessage(wmsg.from, "needaucid"); App.Communicator.sendRequest(needid); }
                    }

                    if(wmsg.from==App.MyProfile.ProfileInfo.name)  return;

                    if (text.StartsWith("aucto1please") && ntwrk.contonetwork)
                    {
                        App.Communicator.sendRequest(new RoomExitMessage("auc-" + ntwrk.ownroomnumber));
                        ntwrk.ownroomnumber = 0;
                        App.Communicator.sendRequest(new RoomEnterMessage("auc-1"));
                        Console.WriteLine("aucto1please");
                    
                    }

                    if (text.StartsWith("aucstay? ") && ntwrk.contonetwork)
                    {   // user founded a room, but dont know if this is all

                        ntwrk.aucstayquestion(text, wmsg.from, srchsvr.shortgeneratedwtsmessage, srchsvr.shortgeneratedwtbmessage);

                    
                    }

                    if (text.StartsWith("aucstay! "))
                    {   // user founded a room, and he dont want to get the room-list
                        ntwrk.aucstay(text, wmsg.from, srchsvr.shortgeneratedwtsmessage, srchsvr.shortgeneratedwtbmessage);
                    }

                    if (text.StartsWith("aucrooms ") && !ntwrk.rooomsearched && ntwrk.contonetwork)
                    {
                        if (text.EndsWith("aucrooms ")) { ntwrk.realycontonetwork = true; }
                        else
                        {
                            ntwrk.visitrooms(text);
                            
                        }
                    }
                    
                    if (text.StartsWith("aucstop"))
                    {
                        ntwrk.deleteuser(wmsg.from);
                    }

                    

                    if (text.StartsWith("aucupdate"))  
                    {
                        ntwrk.sendownauctionstosingleuser(srchsvr.shortgeneratedwtsmessage, srchsvr.shortgeneratedwtbmessage);
                    }
                    

                    
                    //dont needed anymore left in only to be shure :D
                    if (text.StartsWith("needaucid"))
                    {
                        ntwrk.needid(wmsg.from);
                    }
                     //dont needed anymore
                    if (text.StartsWith("aucid "))
                    {
                        ntwrk.saveaucid(text,wmsg.from);
                        
                        
                    }


                }
            }

            
            

        }

        public override void BeforeInvoke(InvocationInfo info)
        {

            return;
        }

        public override void AfterInvoke(InvocationInfo info, ref object returnValue)
        {
            if (info.target is EndGameScreen && info.targetMethod.Equals("GoToLobby")) { ntwrk.inbattle = false; } // user leaved a battle

            if (info.target is ChatUI && info.targetMethod.Equals("Show")) { helpf.chatisshown = (bool)info.arguments[0]; this.screenh = 0; }// so position will be calculatet new on next ongui

            if (info.target is ChatUI && info.targetMethod.Equals("Initiate"))
            {
                helpf.target = (ChatUI)info.target;
                helpf.setchatlogstyle((GUIStyle)this.chatLogStyleinfo.GetValue(info.target));
            }

            if (info.target is TradeSystem && info.targetMethod.Equals("StartTrade"))// user start a trade, show the buy-message
            {
                if (helpf.postmsgontrading == true)
                {
                    helpf.postmsgontrading = false;
                    helpf.postmsggetnextroomenter = true;// the next RoomEnterMsg is the tradeRoom!
                }
            }

            if (info.target is Store && info.targetMethod.Equals("Start"))//user opened store
            {
                helpf.setlobbyskin((GUISkin)typeof(Store).GetField("lobbySkin", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(info.target));
                helpf.storeinfo=(Store)info.target;
                helpf.showtradedialog = false;
                helpf.inauchouse = false;
                helpf.generator = false;
                helpf.settings = false;
                
            }

            if (info.target is ChatRooms && info.targetMethod.Equals("SetRoomInfo")) //adding new users to userlist
            {
                RoomInfoMessage roomInfo = (RoomInfoMessage)info.arguments[0];
                RoomInfoProfile[] profiles = roomInfo.updated;
                for (int i = 0; i < profiles.Length; i++)
                {
                    RoomInfoProfile p = profiles[i];
                    ChatUser user = ChatUser.FromRoomInfoProfile(p) ;
                    if (!helpf.globalusers.ContainsKey(user.name)) { helpf.globalusers.Add(user.name, user); ntwrk.addglobalusers(user); };
                } 
            }

            if (info.target is Store && info.targetMethod.Equals("handleMessage"))// update orginal cards!
            {
                
                Message msg = (Message)info.arguments[0];
                if (msg is LibraryViewMessage)
                {
                    if (!(((LibraryViewMessage)msg).profileId == "test"))
                    {
                        alists.setowncards(msg, helpf.inauchouse, helpf.generator, helpf.wtsmenue);
                    }
                }
            }

            else if (info.target is Store && info.targetMethod.Equals("OnGUI"))
            {

               
                GUI.color = Color.white;
                GUI.contentColor = Color.white;
                drawsubmenu.Invoke(info.target, null);
                    Vector2 screenMousePos = GUIUtil.getScreenMousePos();
                   

                    if (!(Screen.height == screenh) || !(Screen.width == screenw)|| helpf.chatLogStyle==null) // if resolution was changed, recalc positions
                    {
                        screenh = Screen.height;
                        screenw = Screen.width;
                        App.ChatUI.AdjustToResolution();
                        helpf.chatLogStyle = (GUIStyle)chatLogStyleinfo.GetValue(helpf.target);
                        recto.setupPositions(helpf.chatisshown, sttngs.rowscale, helpf.chatLogStyle,helpf.cardListPopupSkin);
                        recto.setupsettingpositions(helpf.chatLogStyle, helpf.cardListPopupBigLabelSkin);

                    }
                   
                    
                    // delete picture on click!
                    if ((Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1)) && crdvwr.clicked >= 3) { crdvwr.clearallpics(); }
                    
                    //klick button AH
                    if (LobbyMenu.drawButton(recto.ahbutton, "AH", helpf.lobbySkin) && !helpf.showtradedialog)
                    {
                        if (this.deckchanged)
                        { App.Communicator.sendRequest(new LibraryViewMessage()); this.deckchanged = false; }
                        ahui.ahbuttonpressed();
                        
                    }
                    // klick button Gen
                    if (LobbyMenu.drawButton(recto.genbutton, "Gen", helpf.lobbySkin) && !helpf.showtradedialog)
                    {
                        if (this.deckchanged)
                        { App.Communicator.sendRequest(new LibraryViewMessage()); this.deckchanged = false; }
                        genui.genbuttonpressed();
                    }

                    if (LobbyMenu.drawButton(recto.settingsbutton, "settings", helpf.lobbySkin) && !helpf.showtradedialog)
                    {
                        setui.setbuttonpressed();
                        
                    }    


                    // draw ah oder gen-menu

                    if (helpf.inauchouse) ahui.drawAH();
                    if (helpf.generator) genui.drawgenerator();
                    if (helpf.settings) setui.drawsettings();
                    GUI.color = Color.white;
                    GUI.contentColor = Color.white;

                    crdvwr.draw();
                    
                
            }
            else if (info.target is Store && (info.targetMethod.Equals("showBuyMenu") || info.targetMethod.Equals("showSellMenu")))
            {
                //disable auc.house + generator
                Store.ENABLE_SHARD_PURCHASES = true;
                helpf.inauchouse = false;
                helpf.generator = false;
                helpf.settings = false;
                helpf.showtradedialog = false;
                if (info.targetMethod.Equals("showSellMenu")) { this.deckchanged = false; }

            }

            if (info.target is ChatRooms && info.targetMethod.Equals("ChatMessage"))
            {
                //get trademessages from chatmessages
                RoomChatMessageMessage msg = (RoomChatMessageMessage)info.arguments[0];
                if (msg.from != "Scrolls")
                {
                    mssgprsr.getaucitemsformmsg(msg.text, msg.from, msg.roomName, helpf.generator, helpf.inauchouse, helpf.settings, helpf.wtsmenue);
                }
            }

            return;
        }


        
       
        
        
     
    }
}