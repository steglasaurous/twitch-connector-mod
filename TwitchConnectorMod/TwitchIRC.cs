using MelonLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TwitchConnectorMod
{
    
    public class TwitchIRC
    {
        public string oauth;
        public string nickName;
        public string channelName;
        private string server = "irc.twitch.tv";
        private int port = 6667;

        // Make a EventArgs class that gives a nice object with username and message
        public class MessageEventArgs : EventArgs
        {
            public string username;
            public string message;
            public string channel;
        }
        
        public delegate void MessageReceivedEventHandler(object sender, TwitchIRC.MessageEventArgs e);
        public event MessageReceivedEventHandler MessageReceived;

        private string buffer = string.Empty;
        private bool stopThreads = false;
        private Queue<string> commandQueue = new Queue<string>();
        // FIXME: If I don't need this, then remove it. 
        //private List<string> recievedMsgs = new List<string>();
        private System.Threading.Thread inProc, outProc;
        private void StartIRC()
        {
            System.Net.Sockets.TcpClient sock = new System.Net.Sockets.TcpClient();
            sock.Connect(server, port);
            if (!sock.Connected)
            {
                Melon<TwitchConnectorMod>.Logger.Msg("Failed to connect!");
                return;
            }
            
            var networkStream = sock.GetStream();
            var input = new System.IO.StreamReader(networkStream);
            var output = new System.IO.StreamWriter(networkStream);

            //Send PASS & NICK.
            output.WriteLine("PASS " + oauth);
            output.WriteLine("NICK " + nickName.ToLower());
            output.Flush();
            //output proc
            outProc = new System.Threading.Thread(() => IRCOutputProcedure(output));
            outProc.Start();
            //input proc
            inProc = new System.Threading.Thread(() => IRCInputProcedure(input, networkStream));
            inProc.Start();
        }
        private void IRCInputProcedure(System.IO.TextReader input, System.Net.Sockets.NetworkStream networkStream)
        {
            while (!stopThreads)
            {
                if (!networkStream.DataAvailable)
                    continue;

                buffer = input.ReadLine();
                // FIXME: Debugging - remove
                //Melon<TwitchConnectorMod>.Logger.Msg("RAW: " + buffer);
                
                //was message?
                if (buffer.Contains("PRIVMSG #"))
                {
                    MessageReceivedEventHandler handler = MessageReceived;
                    MessageEventArgs eventArgs = new MessageEventArgs();
                    Regex messageRegex = new Regex(@"\:(?<username>\w+)!\w+\@[\w.]+ (?<command>[A-Z]+) #(?<channel>\w+) :(?<message>[\S\s]*)");
                    MatchCollection messageMatches = messageRegex.Matches(buffer);

                    eventArgs.username = messageMatches[0].Groups["username"].Value;
                    eventArgs.channel = messageMatches[0].Groups["channel"].Value;
                    eventArgs.message = messageMatches[0].Groups["message"].Value;
                    
                    if (handler != null)
                    {
                        handler(this, eventArgs);
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
    }
}