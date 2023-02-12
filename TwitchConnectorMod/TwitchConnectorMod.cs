using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;

namespace TwitchConnectorMod
{
    public class TwitchConnectorMod : MelonMod
    {
        internal static TwitchIRC IRC = new TwitchIRC();

        private MelonPreferences_Category twitchConnectorPrefs;
        private MelonPreferences_Entry<string> oauthToken;
        private MelonPreferences_Entry<string> channel;
        private MelonPreferences_Entry<string> username;
        public override void OnInitializeMelon()
        {
            twitchConnectorPrefs = MelonPreferences.CreateCategory("TwitchConnector");

            username = twitchConnectorPrefs.CreateEntry<string>("Username", "");
            username.Comment = "Your twitch username";

            oauthToken = twitchConnectorPrefs.CreateEntry<string>("OAuthToken", "");
            oauthToken.Comment = "Get your twitch oauth token by visiting https://twitchapps.com/tmi/";

            // FIXME: Consider supporting multiple channels? 
            channel = twitchConnectorPrefs.CreateEntry<string>("Channel", "");
            channel.Comment = "The twitch channel to join - typically the same as your username";


            // Check for an oauth token.  If we don't have one, open up a web browser to go get one. 
            if (oauthToken.Value == "")
            {
                LoggerInstance.Msg("Twitch OAuth token not present, not connecting to twitch. (Add to MelonPreferences.cfg)");
                return;
            }

            if (username.Value == "")
            {
                LoggerInstance.Msg("Twitch username not present.  (Set you Username in MelonPreferences.cfg under TwitchConnector");
                return;
            }

            if (channel.Value == "")
            {
                LoggerInstance.Msg("Twitch channel not set, defaulting to twitch username.");
                channel.Value = username.Value;
            }

            Melon<TwitchConnectorMod>.Logger.Msg("Starting Connection");
            IRC.oauth = oauthToken.Value;
            IRC.channelName = channel.Value;
            IRC.nickName = username.Value;

            AddChatMsgReceivedEventHandler(OnChatMsgReceived);
            IRC.Enable();
        }

        public static void SendMessage(string message)
        {
            if (IRC != null)
            {
                IRC.SendMsg(message);
            }
        }

        public static void AddChatMsgReceivedEventHandler(TwitchIRC.MessageReceivedEventHandler eventHandler)
        {
            if (IRC != null)
            {
                IRC.MessageReceived += eventHandler;
            }
        }

        private void OnChatMsgReceived(Object sender, TwitchIRC.MessageEventArgs eventArgs)
        {
            Melon<TwitchConnectorMod>.Logger.Msg("MESSAGE: " + eventArgs.username + "::" + eventArgs.message);
        }
    }
}
