﻿using System;

namespace Launcher2 {

	public class ServerListEntry {
		
		public string Hash;
		
		/// <summary> Name of the server. </summary>
		public string Name;
		
		/// <summary> Current number of players on the server. </summary>
		public string Players;
		
		/// <summary> Maximum number of players that can play on the server. </summary>
		public string MaximumPlayers;
		
		/// <summary> How long the server has been 'alive'. </summary>
		public string Uptime;
		
		/// <summary> IP address the server is hosted on. </summary>
		public string IPAddress;
		
		/// <summary> Port the server is hosted on. </summary>
		public string Port;
		
		/// <summary> Mppass specific for the user and this server. </summary>
		public string Mppass;
		
		public ServerListEntry( string hash, string name, string players, string maxPlayers, 
                       string uptime, string mppass, string ip, string port ) {
			Hash = hash;
			Name = name;
			Players = players;
			MaximumPlayers = maxPlayers;
			Uptime = uptime;
			Mppass = mppass;
			IPAddress = ip;
			Port = port;
		}
	}
}