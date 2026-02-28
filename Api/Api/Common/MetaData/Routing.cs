using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Api.Common.MetaData
{
    public static class Routing
    {
        public const string SingelId = "{Id}";
        public const string SingelName = "{Name}";
        public const string EmailName = "{Email}";
        public const string Text = "{Text}";
        public const string Root = "Api";
        public const string Version = "V1";
        public const string Role = Root + "/" + Version + "/";
 
         public static class Story
        {
            public const string Prefix = Role + "Story/";
            public const string AddStory = Prefix + "Add";
            public const string GetMyFriendsStories = Prefix + "GetMyFriendsStories/" + SingelId;
            public const string GetUserStorys = Prefix + "GetUserStorys/" + SingelId;
            public const string GetMyStorys = Prefix + "GetMyStorys/" + SingelId;
            public const string AddStorysView = Prefix + "AddStorysView";
        }
        public static class User
        {
            public const string Prefix = Role + "User/";
            public const string GetInfo = Prefix + "GetInfo";
            public const string SearchToUser = Prefix +"SearchBy/"+ EmailName ;

        }
        public static class Chat
        {
            public const string Prefix = Role + "Chat/";
            public const string SendMessage = Prefix + "Send-Message";
            public const string ReceiveMessage = Prefix + "Receive-Message/" + SingelId;
            public const string GetChatById = Prefix + "Get-Chat";
            public const string SyncChatSnapshot = Prefix + "Sync-Chat-Snapshot";
            public const string GetChatSnapshot = Prefix + "Get-ChatSnapshot";
            public const string GetGroupChatById = Prefix + "group";
            public const string AddNewChat = Prefix + "AddNewChat";
            public const string MakeReadPrivateChat = Prefix + "MakeReadPrivateChat";
            public const string GetChats = Prefix + "GetChats/" + SingelId;
            public const string GetChatInfo = Prefix + "GetChatInfo/" + SingelId;
            public const string GetAllMessages = Prefix + "Get-All-Messages/" + SingelId;
        }

        public static class Group
        {
            public const string Prefix = Role + "Group/";
            public const string AddNewGroup = Prefix + "Add-Group";
            public const string AddMemberGroup = Prefix + "Add-Member-Group";
        
        }
        public static class Message
        {
            public const string Prefix = Role + "Message/";
            public const string GetMsgInfo = Prefix + "Msg-Info/"+ SingelId;
    

        }

        public static class Contact
        {
            public const string Prefix = Role + "Contact/";
            public const string Add = Prefix + "Add";
            public const string UpdateContact = Prefix + "UpdateContact";
            public const string DeleteContact = Prefix + "DeleteContact";
            public const string GetUserContact = Prefix + "GetUserContact/"+ SingelId;
            public const string GetCacheUserContact = Prefix + "GetCacheUserContact/"+ SingelId;
          
        }
        public static class Authentication
        {
            public const string Prefix = Role + "Auth/";
            public const string RegisterUser = Prefix + "Register/User";
            public const string RegisterSeller = Prefix + "Register/Seller";
            public const string Login = Prefix + "Login";
            public const string LoginSeller = Prefix + "LoginSeller";
            public const string LoginWihtGoogle = Prefix + "Google-Login";
            public const string AuthCallBackGoogle = Prefix + "Google-Response";
            public const string EmailExist = Prefix + "EmailExist/" + EmailName;
            public const string UserNameExist = Prefix + "UserNameExist/" + SingelName;
            public const string RefreshToken = Prefix + "RefreshToken/";
            public const string ValidationToken = Prefix + "ValidationToken/";
            public const string GetToken = Prefix + "Get-Token/";
            public const string GetRefreshToken = Prefix + "Get-RefreshToken/";
        }
    }
}