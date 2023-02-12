# TwitchConnectorMod - A basic Twitch IRC implementation for Audica



This mod connects to twitch chat and exposes two-way twitch chat communication to other Audica mods. 



## Prerequisites

This requires MelonLoader 0.5.3 or later be installed on your Audica installation.  Check out https://melonwiki.xyz for installation instructions. 

## Installation

1. Download the latest TwitchConnectorMod.dll from the releases section of this repository and drop it into your `Mods` folder in your Audica installation folder.

2. In your Audica installation folder, edit the file `UserData\MelonPreferences.cfg` with a text editor (notepad will work) and add the following at the end:

   ```
   [TwitchConnector]
   Username = "your_twitch_username" # Replace "your_twitch_username" with your twitch username.
   OAuthToken = "oauth:xxxxx" # Get your twitch oauth token by visiting https://twitchapps.com/tmi/ and paste the token you receive into here.
   Channel = "your_twitch_username" # The twitch channel to join - typically this is the same as your username
   
   ```

3. Start Audica.  In the MelonLoader window, if the connection to Twitch was successful, you should see a message similar to this: 

   ```
   [19:51:21.241] [Twitch_Connector_Mod] Twitch connected
   ```

   

## Usage with Mods

This section applies to modders that want to send/receive messages from Twitch chat.



### Receiving Twitch messages

Create a method to process incoming messages:

```csharp
void OnChatMessage(Object sender, TwitchConnectorMod.TwitchIRC.MessageEventArgs eventArgs)
{
    // process the message how you see fit.
}

```

The MessageEventArgs object has the following structure:

```csharp
        public class MessageEventArgs : EventArgs
        {
            public string username; // The username that sent the message
            public string message; // The message content
            public string channel; // The channel the message is in
            public string rawMessage; // The raw IRC message - useful to parse lower-level twitch info if needed.

            public MessageEventArgs();
        }

```



Register the event handler with the TwitchConnectorMod:

```csharp
            TwitchConnectorMod.TwitchConnectorMod.AddChatMsgReceivedEventHandler(OnChatMessage);

```



### Sending Twitch messages

Call the SendMessage method:

```csharp
TwitchConnectorMod.TwitchConnectorMod.SendMessage("Greetings!");
```



At the moment there's no parsing of twitch emotes or other features. Also note that this is a VERY basic IRC client, and does not currently use Twitch Pub/Sub or any other Twitch features.  