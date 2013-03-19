﻿//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena

using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;


namespace wmib
{
    [Serializable()]
    public class ProcessorIRC
    {
        public string text;
        public string sn;

        private void Ping()
        {
            return;
        }

        private bool Info(string command, string parameters, string value)
        {
            return true;
        }

        private bool ChannelInfo(string[] code, string command, string source, string parameters, string _value)
        {
            if (code.Length > 3)
            {

            }
            return false;
        }

        private bool ChannelTopic(string[] code, string command, string source, string parameters, string value)
        {

            return false;
        }

        private bool FinishChan(string[] code)
        {
            if (code.Length > 2)
            {
                config.channel channel = core.getChannel(code[3]);
                if (channel != null)
                {
                    Program.Log("Finished parsing for " + channel.Name + " parsed totaly: " + channel.UserList.Count.ToString());
                    channel.FreshList = true;
                }
            }
            return false;
        }

        private bool TopicInfo(string[] code, string parameters)
        {
            return false;
        }

        private bool ParseUs(string[] code)
        {
            if (code.Length > 8)
            {
                config.channel channel = core.getChannel(code[3]);
                string ident = code[4];
                string host = code[5];
                string nick = code[7];
                string server = code[6];
                if (channel != null)
                {
                    if (!channel.containsUser(nick))
                    {
                        User _user = new User(nick, host, ident);
                        channel.UserList.Add(_user);
                        return true;
                    }
                    foreach (User u in channel.UserList)
                    {
                        if (u.Nick == nick)
                        {
                            u.Ident = ident;
                            u.Host = host;
                            break;
                        }
                    }
                }
            }
            return false;
        }

        private bool ParseInfo(string[] code, string[] data)
        {
            if (code.Length > 3)
            {
                string name = code[4];
                config.channel channel = core.getChannel(name);
                if (channel != null)
                {
                    string[] _chan = data[2].Split(' ');
                    foreach (var user in _chan)
                    {
                        if (!channel.containsUser(user) && user != "")
                        {
                            lock (channel.UserList)
                            {
                                channel.UserList.Add(new User(user, "", ""));
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private bool ChannelBans(string[] code)
        {

            return false;
        }

        private bool ProcessNick(string source, string parameters, string value)
        {
            string nick = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            string _new = value;
            foreach (config.channel item in config.channels)
            {
                lock (item.UserList)
                {
                    foreach (User curr in item.UserList)
                    {
                        if (curr.Nick == nick)
                        {
                            lock (Module.module)
                            {
                                foreach (Module xx in Module.module)
                                {
                                    try
                                    {
                                        if (xx.working)
                                        {
                                            xx.Hook_Nick(item, new User(_new, _host, _ident), nick);
                                        }
                                    }
                                    catch (Exception er)
                                    {
                                        core.Log("Error on hook in " + xx.Name, true);
                                        core.handleException(er);
                                    }
                                }
                            }
                            curr.Nick = _new;
                        }
                    }
                }
            }
            return true;
        }

        private bool Mode(string source, string parameters, string value)
        {
            return false;
        }

        private bool Part(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            config.channel channel = core.getChannel(chan);
            User us = new User(user, _host, _ident);
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        if (!module.working)
                        {
                            continue;
                        }
                        try
                        {
                            module.Hook_Part(channel, us);
                        }
                        catch (Exception fail)
                        {
                            core.handleException(fail);
                        }
                    }
                }
                User delete = null;
                if (channel.containsUser(user))
                {
                    lock (channel.UserList)
                    {
                        foreach (User User in channel.UserList)
                        {
                            if (User.Nick == user)
                            {
                                delete = User;
                                break;
                            }
                        }
                        channel.UserList.Remove(delete);
                    }
                    return true;
                }
                return true;
            }
            return false;
        }

        private bool Topic(string source, string parameters, string value)
        {
            string chan = parameters;

            return false;
        }

        private bool Quit(string source, string parameters, string value)
        {
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            User _user = new User(user, _host, _ident);
            string _new = value;
            lock (Module.module)
            {
                foreach (Module module in Module.module)
                {
                    if (!module.working)
                    {
                        continue;
                    }
                    try
                    {
                        module.Hook_Quit(_user, value);
                    }
                    catch (Exception fail)
                    {
                        core.Log("MODULE: exception at Hook_Quit in " + module.Name, true);
                        core.handleException(fail);
                    }
                }
            }
            foreach (config.channel item in config.channels)
            {
                User target = null;
                lock (item.UserList)
                {
                    foreach (User curr in item.UserList)
                    {
                        if (curr.Nick == user)
                        {
                            target = curr;
                            break;
                        }
                    }
                }
                if (target != null)
                {
                    lock (item.UserList)
                    {
                        item.UserList.Remove(target);
                    }
                }
            }
            return true;
        }

        private bool Kick(string source, string parameters, string value)
        {
            string user = parameters.Substring(parameters.IndexOf(" ") + 1);
            string user2 = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            User user01 = new User(user, "", "");
            User sr = new User(user2, _host, _ident);
            // petan!pidgeon@petan.staff.tm-irc.org KICK #support HelpBot :Removed from the channel
            config.channel channel = core.getChannel(parameters.Substring(0, parameters.IndexOf(" ")));
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        if (!module.working)
                        {
                            continue;
                        }
                        try
                        {
                            module.Hook_Kick(channel, sr, user01);
                        }
                        catch (Exception fail)
                        {
                            core.Log("MODULE: exception at Hook_Kick in " + module.Name, true);
                            core.handleException(fail);
                        }
                    }
                }
                if (channel.containsUser(user))
                {
                    User delete = null;
                    lock (channel.UserList)
                    {
                        foreach (User _user in channel.UserList)
                        {
                            if (_user.Nick == user)
                            {
                                delete = _user;
                                break;
                            }
                        }
                        if (delete != null)
                        {
                            channel.UserList.Remove(delete);
                        }
                    }
                }
                return true;
            }
            return false;
        }

        private bool Join(string source, string parameters, string value)
        {
            string chan = parameters;
            chan = chan.Replace(" ", "");
            if (chan == "")
            {
                chan = value;
            }
            string user = source.Substring(0, source.IndexOf("!"));
            string _ident;
            string _host;
            _host = source.Substring(source.IndexOf("@") + 1);
            _ident = source.Substring(source.IndexOf("!") + 1);
            _ident = _ident.Substring(0, _ident.IndexOf("@"));
            config.channel channel = core.getChannel(chan);
            User _user = new User(user, _host, _ident);
            if (channel != null)
            {
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        try
                        {
                            if (module.working)
                            {
                                module.Hook_Join(channel, _user);
                            }
                        }
                        catch (Exception fail)
                        {
                            core.Log("MODULE: exception at Hook_Join in " + module.Name, true);
                            core.handleException(fail);
                        }
                    }
                }
                if (!channel.containsUser(user))
                {
                    lock (channel.UserList)
                    {
                        channel.UserList.Add(new User(user, _host, _ident));
                    }
                }
                return true;
            }
            return false;
        }

        public bool Result()
        {
            try
            {
                if (text == null || text == "")
                {
                    return false;
                }
                if (text.StartsWith(":"))
                {
                    string[] data = text.Split(':');
                    if (data.Length > 1)
                    {
                        string command = "";
                        string parameters = "";
                        string command2 = "";
                        string source;
                        string _value;
                        source = text.Substring(1);
                        source = source.Substring(0, source.IndexOf(" "));
                        command2 = text.Substring(1);
                        command2 = command2.Substring(source.Length + 1);
                        if (command2.Contains(" :"))
                        {
                            command2 = command2.Substring(0, command2.IndexOf(" :"));
                        }
                        string[] _command = command2.Split(' ');
                        if (_command.Length > 0)
                        {
                            command = _command[0];
                        }
                        if (_command.Length > 1)
                        {
                            int curr = 1;
                            while (curr < _command.Length)
                            {
                                parameters += _command[curr] + " ";
                                curr++;
                            }
                            if (parameters.EndsWith(" "))
                            {
                                parameters = parameters.Substring(0, parameters.Length - 1);
                            }
                        }
                        _value = "";
                        if (text.Length > 3 + command2.Length + source.Length)
                        {
                            _value = text.Substring(3 + command2.Length + source.Length);
                        }
                        if (_value.StartsWith(":"))
                        {
                            _value = _value.Substring(1);
                        }
                        string[] code = data[1].Split(' ');
                        switch (command)
                        {
                            case "001":
                                return true;
                            case "002":
                            case "003":
                            case "004":
                            case "005":
                                if (Info(command, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "PONG":
                                Ping();
                                return true;
                            case "INFO":
                                return true;
                            case "NOTICE":
                                return true;
                            case "PING":
                                //protocol.Transfer("PONG ", Configuration.Priority.High);
                                return true;
                            case "NICK":
                                if (ProcessNick(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "TOPIC":
                                if (Topic(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "MODE":
                                if (Mode(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "PART":
                                if (Part(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "QUIT":
                                if (Quit(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "JOIN":
                                if (Join(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                            case "KICK":
                                if (Kick(source, parameters, _value))
                                {
                                    return true;
                                }
                                break;
                        }
                        if (data[1].Contains(" "))
                        {
                            switch (command)
                            {
                                case "315":
                                    if (FinishChan(code))
                                    {
                                        return true;
                                    }
                                    break;
                                case "324":
                                    if (ChannelInfo(code, command, source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "332":
                                    if (ChannelTopic(code, command, source, parameters, _value))
                                    {
                                        return true;
                                    }
                                    break;
                                case "333":
                                    if (TopicInfo(code, parameters))
                                    {
                                        return true;
                                    }
                                    break;
                                case "352":
                                    if (ParseUs(code))
                                    {
                                        return true;
                                    }
                                    break;
                                case "353":
                                    if (ParseInfo(code, data))
                                    {
                                        return true;
                                    }
                                    break;
                                case "366":
                                    return true;
                                case "367":
                                    if (ChannelBans(code))
                                    {
                                        return true;
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return true;
        }

        public ProcessorIRC(string _text)
        {
            text = _text;
            sn = "";
        }
    }

    public class IRC
    {
        /// <summary>
        /// Server addr
        /// </summary>
        public string server;
        /// <summary>
        /// Nick
        /// </summary>
        public string nickname;
        /// <summary>
        /// ID
        /// </summary>
        public string ident;
        /// <summary>
        /// User
        /// </summary>
        public string username;
        /// <summary>
        /// Pw
        /// </summary>
        public string password;
        /// <summary>
        /// Socket
        /// </summary>
        public static System.Net.Sockets.NetworkStream data;
        /// <summary>
        /// Enabled
        /// </summary>
        public bool disabled = true;
        /// <summary>
        /// Socket Reader
        /// </summary>
        public System.IO.StreamReader rd;
        /// <summary>
        /// Writer
        /// </summary>
        public System.IO.StreamWriter wd;
        public static System.Threading.Thread check_thread;

        public System.Threading.Thread _Queue;

        public SlowQueue _SlowQueue;

        [Serializable()]
        public class SlowQueue : MarshalByRefObject
        {
            public SlowQueue(IRC _parent)
            {
                Parent = _parent;
            }

            public struct Message
            {
                public priority _Priority;
                public string message;
                public string channel;
            }

            public bool running = true;
            public List<Message> messages = new List<Message>();
            public List<Message> newmessages = new List<Message>();
            public IRC Parent;

            public void DeliverMessage(string Message, string Channel, priority Pr = priority.normal)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                text.channel = Channel;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            public void DeliverAct(string Message, string Channel, priority Pr = priority.normal)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                text.channel = Channel;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            public void DeliverMessage(string Message, User User, priority Pr = priority.low)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                text.channel = User.Nick;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            public void DeliverMessage(string Message, config.channel Channel, priority Pr = priority.normal)
            {
                Message text = new Message();
                text._Priority = Pr;
                text.message = Message;
                text.channel = Channel.Name;
                lock (messages)
                {
                    messages.Add(text);
                    return;
                }
            }

            public void Exit()
            {
                running = false;
                core.Log("Turning off the message queue with " + (newmessages.Count + messages.Count).ToString() + " untransfered data");
                lock (messages)
                {
                    messages.Clear();
                }
                lock (newmessages)
                {
                    newmessages.Clear();
                }
            }

            public void Run()
            {
                while (true)
                {
                    try
                    {
                        if (!running)
                        {
                            return;
                        }
                        if (messages.Count > 0)
                        {
                            lock (messages)
                            {
                                newmessages.AddRange(messages);
                                messages.Clear();
                            }
                        }
                        if (newmessages.Count > 0)
                        {
                            List<Message> Processed = new List<Message>();
                            priority highest = priority.low;
                            lock (newmessages)
                            {
                                while (newmessages.Count > 0)
                                {
                                    // we need to get all messages that have been scheduled to be send
                                    lock (messages)
                                    {
                                        if (messages.Count > 0)
                                        {
                                            newmessages.AddRange(messages);
                                            messages.Clear();
                                        }
                                    }
                                    highest = priority.low;
                                    // we need to check the priority we need to handle first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message._Priority > highest)
                                        {
                                            highest = message._Priority;
                                            if (message._Priority == priority.high)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    // send highest priority first
                                    foreach (Message message in newmessages)
                                    {
                                        if (message._Priority >= highest)
                                        {
                                            Processed.Add(message);
                                            Parent.Message(message.message, message.channel);
                                            System.Threading.Thread.Sleep(1000);
                                            if (highest != priority.high)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                    foreach (Message message in Processed)
                                    {
                                        if (newmessages.Contains(message))
                                        {
                                            newmessages.Remove(message);
                                        }
                                    }
                                }
                            }
                        }
                        lock (newmessages)
                        {
                            newmessages.Clear();
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        return;
                    }
                    System.Threading.Thread.Sleep(200);
                }
            }
        }

        public IRC(string _server, string _nick, string _ident, string _username)
        {
            server = _server;
            password = "";
            _SlowQueue = new SlowQueue(this);
            username = _username;
            nickname = _nick;
            ident = _ident;
        }

        /// <summary>
        /// Send a message to channel
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="channel">Channel</param>
        /// <returns></returns>
        public bool Message(string message, string channel)
        {
            try
            {
                config.channel curr = core.getChannel(channel);
                if (curr == null && channel.StartsWith("#"))
                {
                    Program.Log("Attempt to send a message to non existing channel: " + channel + " " + message, true);
                    return true;
                }
                else if (curr != null)
                {
                    if (curr.suppress)
                    {
                        return true;
                    }
                }
                SendData("PRIVMSG " + channel + " :" + message.Replace("\n", " "));
                lock (Module.module)
                {
                    foreach (Module module in Module.module)
                    {
                        try
                        {
                            if (module.working)
                            {
                                module.Hook_OnSelf(curr, new User(config.username, "wikimedia/bot/wm-bot", "wmib"), message);
                            }
                        }
                        catch (Exception fail)
                        {
                            core.handleException(fail);
                        }
                    }
                }
                wd.Flush();
            }
            catch (Exception fail)
            {
                core.handleException(fail);
            }
            return true;
        }

        public bool Join(config.channel Channel)
        {
            if (Channel != null)
            {
                SendData("JOIN " + Channel.Name);
                wd.Flush();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Ping
        /// </summary>
        public void Ping()
        {
            while (true)
            {
                try
                {
                    System.Threading.Thread.Sleep(20000);
                    if (!config.serverIO)
                    {
                        SendData("PING :" + config.network);
                        wd.Flush();
                    }
                    foreach (config.channel dd in config.channels)
                    {
                        if (!dd.FreshList)
                        {
                            SendData("WHO " + dd.Name);
                            Program.Log("requesting user list for" + dd.Name);
                            wd.Flush();
                            Thread.Sleep(2000);
                        }
                    }
                }
                catch (Exception)
                { }
            }
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <returns></returns>
        public bool Reconnect()
        {
            _Queue.Abort();
            string _s = server;
            if (config.serverIO)
            {
                data = new System.Net.Sockets.TcpClient("127.0.0.1", 6667).GetStream();
            }
            else
            {
                data = new System.Net.Sockets.TcpClient(server, 6667).GetStream();
            }
            rd = new StreamReader(data, System.Text.Encoding.UTF8);
            wd = new StreamWriter(data);
            SendData("USER " + username + " 8 * :" + ident);
            SendData("NICK " + nickname);
            Authenticate();
            _Queue = new System.Threading.Thread(_SlowQueue.Run);
            foreach (config.channel ch in config.channels)
            {
                System.Threading.Thread.Sleep(2000);
                this.Join(ch);
            }
            _SlowQueue.newmessages.Clear();
            _SlowQueue.messages.Clear();
            wd.Flush();
            _Queue.Start();
            return false;
        }

        public void SendData(string text)
        {
            wd.WriteLine(text);
            core.TrafficLog("MAIN>>>>>>" + text);
        }

        public bool Authenticate()
        {
            if (config.password != "")
            {
                SendData("PRIVMSG nickserv :identify " + config.login + " " + config.password);
                wd.Flush();
                System.Threading.Thread.Sleep(4000);
            }
            return true;
        }

        /// <summary>
        /// Connection
        /// </summary>
        /// <returns></returns>
        public void Connect()
        {
            try
            {
                if (!config.serverIO)
                {
                    data = new System.Net.Sockets.TcpClient(server, 6667).GetStream();
                }
                else
                {
                    data = new System.Net.Sockets.TcpClient("127.0.0.1", 6667).GetStream();
                    Program.Log("System is using external bouncer");
                }
                disabled = false;
                rd = new System.IO.StreamReader(data, System.Text.Encoding.UTF8);
                wd = new System.IO.StreamWriter(data);

                bool Auth = true;

                if (config.serverIO)
                {
                    SendData("CONTROL: STATUS");
                    wd.Flush();
                    Console.WriteLine("CACHE: Waiting for buffer");
                    bool done = true;
                    while (done)
                    {
                        string response = rd.ReadLine();
                        if (response == "CONTROL: TRUE")
                        {
                            done = false;
                            Auth = false;
                        }
                        else if (response == "CONTROL: FALSE")
                        {
                            done = false;
                            SendData("CONTROL: CREATE");
                            wd.Flush();
                        }
                    }
                }

                _Queue = new System.Threading.Thread(_SlowQueue.Run);

                check_thread = new System.Threading.Thread(Ping);
                check_thread.Start();

                if (Auth)
                {
                    SendData("USER " + username + " 8 * :" + ident);
                    SendData("NICK " + nickname);
                }

                _Queue.Start();
                System.Threading.Thread.Sleep(2000);

                if (Auth)
                {
                    Authenticate();

                    foreach (config.channel ch in config.channels)
                    {
                        if (ch.Name != "")
                        {
                            this.Join(ch);
                            System.Threading.Thread.Sleep(2000);
                        }
                    }
                }
                wd.Flush();
                string text = "";
                string nick = "";
                string host = "";
                string message = "";
                string channel = "";
                char delimiter = (char)001;

                while (!disabled)
                {
                    try
                    {
                        while (!rd.EndOfStream && !core.exit)
                        {
                            text = rd.ReadLine();
                            core.TrafficLog("MAIN<<<<<<" + text);
                            if (config.serverIO)
                            {
                                if (text.StartsWith("CONTROL: "))
                                {
                                    if (text == "CONTROL: DC")
                                    {
                                        SendData("CONTROL: CREATE");
                                        wd.Flush();
                                        Console.WriteLine("CACHE: Lost connection to remote, reconnecting");
                                        bool connected = false;
                                        while (!connected)
                                        {
                                            System.Threading.Thread.Sleep(800);
                                            SendData("CONTROL: STATUS");
                                            wd.Flush();
                                            string response = rd.ReadLine();
                                            core.TrafficLog("MAIN<<<<<<" + response);
                                            if (response == "CONTROL: OK")
                                            {
                                                Reconnect();
                                                connected = true;
                                            }
                                        }
                                    }
                                }
                            }
                            if (text.StartsWith(":"))
                            {
                                ProcessorIRC processor = new ProcessorIRC(text);
                                processor.Result();
                                string check = text.Substring(text.IndexOf(" "));
                                if (check.StartsWith(" 005"))
                                {

                                }
                                else
                                {
                                    string command = "";
                                    string[] part;
                                    if (text.Contains(" :"))
                                    {
                                        part = text.Split(':');
                                        command = text.Substring(1);
                                        command = command.Substring(0, command.IndexOf(" :"));
                                    }
                                    if (command.Contains("PRIVMSG"))
                                    {
                                        string info = text.Substring(1, text.IndexOf(" :", 1) - 1);
                                        string info_host;
                                        // we got a message here :)
                                        if (text.Contains("!") && text.Contains("@"))
                                        {
                                            nick = info.Substring(0, info.IndexOf("!"));
                                            host = info.Substring(info.IndexOf("@") + 1, info.IndexOf(" ", info.IndexOf("@")) - 1 - info.IndexOf("@"));
                                        }
                                        info_host = info.Substring(info.IndexOf("PRIVMSG "));

                                        if (info_host.Contains("#"))
                                        {
                                            channel = info_host.Substring(info_host.IndexOf("#"));
                                            message = text.Replace(info, "");
                                            message = message.Substring(message.IndexOf(" :") + 2);
                                            if (message.Contains(delimiter.ToString() + "ACTION"))
                                            {
                                                core.getAction(message.Replace(delimiter.ToString() + "ACTION", ""), channel, host, nick);
                                                continue;
                                            }
                                            else
                                            {
                                                core.getMessage(channel, nick, host, message);
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            message = text.Substring(text.IndexOf("PRIVMSG"));
                                            message = message.Substring(message.IndexOf(" :"));
                                            // private message
                                            if (message.StartsWith(" :" + delimiter.ToString() + "FINGER"))
                                            {
                                                SendData("NOTICE " + nick + " :" + delimiter.ToString() + "FINGER" + " I am a bot don't finger me");
                                                wd.Flush();
                                                continue;
                                            }
                                            if (message.StartsWith(" :" + delimiter.ToString() + "TIME"))
                                            {
                                                SendData("NOTICE " + nick + " :" + delimiter.ToString() + "TIME " + System.DateTime.Now.ToString());
                                                wd.Flush();
                                                continue;
                                            }
                                            if (message.StartsWith(" :" + delimiter.ToString() + "PING"))
                                            {
                                                SendData("NOTICE " + nick + " :" + delimiter.ToString() + "PING" + message.Substring(message.IndexOf(delimiter.ToString() + "PING") + 5));
                                                wd.Flush();
                                                continue;
                                            }
                                            if (message.StartsWith(" :" + delimiter.ToString() + "VERSION"))
                                            {
                                                SendData("NOTICE " + nick + " :" + delimiter.ToString() + "VERSION " + config.version);
                                                wd.Flush();
                                                continue;
                                            }
                                            bool respond = true;
                                            string modules = "";
                                            lock (Module.module)
                                            {
                                                foreach (Module module in Module.module)
                                                {
                                                    if (module.working)
                                                    {
                                                        try
                                                        {

                                                            if (module.Hook_OnPrivateFromUser(message.Substring(2), new User(nick, host, ident)))
                                                            {
                                                                respond = false;
                                                                modules += module.Name + " ";
                                                            }
                                                        }
                                                        catch (Exception fail)
                                                        {
                                                            core.handleException(fail);
                                                        }
                                                    }
                                                }
                                            }
                                            if (respond)
                                            {
                                                _SlowQueue.DeliverMessage("Hi, I am robot, this command was not understood. Please bear in mind that every message you send to me will be logged for debuging purposes. See documentation at http://meta.wikimedia.org/wiki/WM-Bot for explanation of commands", nick, priority.low);
                                                Program.Log("Ignoring private message: (" + nick + ") " + message.Substring(2), false);
                                            }
                                            else
                                            {
                                                Program.Log("Private message: (handled by " + modules + " from "  + nick + ") " + message.Substring(2), false);
                                            }
                                            continue;
                                        }
                                    }
                                    if (command.Contains("PING "))
                                    {
                                        SendData("PONG " + text.Substring(text.IndexOf("PING ") + 5));
                                        wd.Flush();
                                        Console.WriteLine(command);
                                    }
                                    if (command.Contains("KICK"))
                                    {
                                        string user;
                                        string _channel;
                                        string temp = command.Substring(command.IndexOf("KICK"));
                                        string[] parts = temp.Split(' ');
                                        if (parts.Length > 1)
                                        {
                                            _channel = parts[1];
                                            user = parts[2];
                                            if (user == nickname)
                                            {
                                                config.channel chan = core.getChannel(_channel);
                                                if (chan != null)
                                                {
                                                    if (config.channels.Contains(chan))
                                                    {
                                                        config.channels.Remove(chan);
                                                        Program.Log("I was kicked from " + parts[1]);
                                                        config.Save();
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            System.Threading.Thread.Sleep(50);
                        }
                        Program.Log("Reconnecting, end of data stream");
                        Reconnect();
                    }
                    catch (System.IO.IOException xx)
                    {
                        Program.Log("Reconnecting, connection failed " + xx.Message + xx.StackTrace);
                        Reconnect();
                    }
                    catch (Exception xx)
                    {
                        core.handleException(xx, channel);
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("IRC: Connection error");
                disabled = true;
            }
        }

        public int Disconnect()
        {
            wd.Flush();
            return 0;
        }

        public enum priority
        {
            low = 1,
            normal = 2,
            high = 3,
        }
    }
}
