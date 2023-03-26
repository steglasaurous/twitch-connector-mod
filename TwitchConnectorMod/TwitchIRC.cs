using MelonLoader;
using System.Collections.Generic;

namespace TwitchConnectorMod
{
    
    public class TwitchIRC
    {
        public string oauth;
        public string nickName;
        public string channelName;
        private string server = "irc.twitch.tv";
        private int port = 6667;
        public bool logRawMessages = false;
        public delegate void MessageReceivedEventHandler(object sender, ParsedTwitchMessage e);
        public event MessageReceivedEventHandler MessageReceived;

        private string buffer = string.Empty;
        private bool stopThreads = false;
        private Queue<string> commandQueue = new Queue<string>();
        private List<string> receivedMsgs = new List<string>();
        private System.Threading.Thread inProc, outProc;
        private void StartIRC()
        {
            Melon<TwitchConnectorMod>.Logger.Msg("StartIRC() - attempting socket connection");
            System.Net.Sockets.TcpClient sock = new System.Net.Sockets.TcpClient();
            sock.Connect(server, port);
            Melon<TwitchConnectorMod>.Logger.Msg("StartIRC() - Finished Connect()");

            if (!sock.Connected)
            {
                Melon<TwitchConnectorMod>.Logger.Msg("Failed to connect to Twitch - network error");
                return;
            }
            
            var networkStream = sock.GetStream();
            var input = new System.IO.StreamReader(networkStream);
            var output = new System.IO.StreamWriter(networkStream);
            //output proc
            outProc = new System.Threading.Thread(() => IRCOutputProcedure(output));
            outProc.Start();
            //input proc
            inProc = new System.Threading.Thread(() => IRCInputProcedure(input, networkStream));
            inProc.Start();
            Melon<TwitchConnectorMod>.Logger.Msg("StartIRC() - Sending PASS and NICK");

            //Send PASS & NICK.
            output.WriteLine("PASS " + oauth);
            output.WriteLine("NICK " + nickName.ToLower());
            // Adds information to incoming messages that include details like badges, whether they're a subscriber, or a mod/broadcaster, etc.
            output.WriteLine("CAP REQ :twitch.tv/tags"); 
            output.Flush();
            Melon<TwitchConnectorMod>.Logger.Msg("StartIRC() - Sent PASS and NICK");
        }
        private void IRCInputProcedure(System.IO.TextReader input, System.Net.Sockets.NetworkStream networkStream)
        {
            while (!stopThreads)
            {
                if (!networkStream.DataAvailable)
                    continue;

                buffer = input.ReadLine();
                if (this.logRawMessages)
                {
                    Melon<TwitchConnectorMod>.Logger.Msg("Twitch IRC: " + buffer);
                }
                
                if (buffer.Contains("PRIVMSG #"))
                {
                    lock (receivedMsgs)
                    {
                        this.receivedMsgs.Add(buffer);
                    }
                }

                //Send pong reply to any ping messages
                if (buffer.StartsWith("PING "))
                {
                    SendCommand(buffer.Replace("PING", "PONG"));
                }

                //After server sends 001 command, we can join a channel
                if (buffer.Split(' ')[1] == "001")
                {
                    SendCommand("JOIN #" + channelName);
                    Melon<TwitchConnectorMod>.Logger.Msg("Connected to Twitch.");
                }
            }
        }
        private void IRCOutputProcedure(System.IO.TextWriter output)
        {
            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();
            while (!stopThreads)
            {
                lock (commandQueue)
                {
                    if (commandQueue.Count > 0) //do we have any commands to send?
                    {
                        // https://github.com/justintv/Twitch-API/blob/master/IRC.md#command--message-limit 
                        //have enough time passed since we last sent a message/command?
                        if (stopWatch.ElapsedMilliseconds > 1750)
                        {
                            //send msg.
                            if (this.logRawMessages)
                            {
                                Melon<TwitchConnectorMod>.Logger.Msg($"Twitch IRC SEND: " + commandQueue.Peek());
                            }
                            
                            output.WriteLine(commandQueue.Peek());
                            output.Flush();
                            //remove msg from queue.
                            commandQueue.Dequeue();
                            //restart stopwatch.
                            stopWatch.Reset();
                            stopWatch.Start();
                        }
                    }
                }
            }
        }

        public void SendCommand(string cmd)
        {
            lock (commandQueue)
            {
                commandQueue.Enqueue(cmd);
            }
        }
        public void SendMsg(string msg)
        {
            lock (commandQueue)
            {
                commandQueue.Enqueue("PRIVMSG #" + channelName + " :" + msg);
            }
        }

        public void Enable()
        {
            stopThreads = false;
            StartIRC();
        }
        public void Disable()
        {
            stopThreads = true;
            // FIXME: Need something to disconnect from IRC here?
        }

        public void Update()
        {
            lock (receivedMsgs)
            {
                if (this.receivedMsgs.Count > 0)
                {
                    for (int i = 0; i < receivedMsgs.Count; i++)
                    {
                        MessageReceivedEventHandler handler = MessageReceived;
                        ParsedTwitchMessage parsedMessage = new ParsedTwitchMessage(receivedMsgs[i]);
                        
                        if (handler != null)
                        {
                            handler(this, parsedMessage);
                        }
                    }
                    receivedMsgs.Clear();
                }
            }
        }
    }
}