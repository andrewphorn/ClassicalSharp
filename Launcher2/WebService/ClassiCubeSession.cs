﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Launcher2 {
	
	public sealed class ClassicubeSession : IWebTask {
		
		const string classicubeNetUri = "https://www.classicube.net/",
		loginUri = "https://www.classicube.net/api/login/",
		publicServersUri = "https://www.classicube.net/api/servers",
		playUri = "https://www.classicube.net/api/server/";
		const string wrongCredentialsMessage = "Login failed";
		const string loggedInAs = @"<a href=""/acc"" class=""button"">";
		
		public List<ServerListEntry> Servers = new List<ServerListEntry>();
		
		public void LoginAsync( string user, string password ) {
			Username = user;
			Working = true;
			Exception = null;
			Status = "&eSigning in..";
			Servers = new List<ServerListEntry>();
			
			Thread thread = new Thread( LoginWorker, 256 * 1024 );
			thread.Name = "Launcher.CCLoginAsync";
			thread.Start( password );
		}
		
		void LoginWorker( object password ) {
			// Sign in to classicube.net
			try {
				Login( Username, (string)password );
			} catch( WebException ex ) {
				Finish( false, ex, "sign in" ); return;
			} catch( InvalidOperationException ex ) {
				Finish( false, null, "&eFailed to sign in: " +
				       Environment.NewLine + ex.Message ); return;
			}
			
			// Retrieve list of public servers
			Status = "&eRetrieving public servers list..";
			try {
				Servers = GetPublicServers();
			} catch( WebException ex ) {
				Servers = new List<ServerListEntry>();
				Finish( false, ex, "retrieving servers list" ); return;
			}
			Finish( true, null, "&eSigned in" );
		}
		
		void Login( string user, string password ) {
			Username = user;
			// Step 1: GET csrf token from login page.
			var swGet = System.Diagnostics.Stopwatch.StartNew();
			string getResponse = GetHtmlAll( loginUri, classicubeNetUri );
			int index = 0; bool success = true;
			Dictionary<string, object> data =
				(Dictionary<string, object>)Json.ParseValue( getResponse, ref index, ref success );
			string token = (string)data["token"];
			
			// Step 2: POST to login page with csrf token.
			string loginData = String.Format(
				"username={0}&password={1}&token={2}",
				Uri.EscapeDataString( user ),
				Uri.EscapeDataString( password ),
				Uri.EscapeDataString( token )
			);
			Log( "cc login took " + swGet.ElapsedMilliseconds );
			
			var sw = System.Diagnostics.Stopwatch.StartNew();
			string response = PostHtmlAll( loginUri, loginUri, loginData );
			index = 0; success = true;
			data = (Dictionary<string, object>)Json.ParseValue( response, ref index, ref success );
			
			List<object> errors = (List<object>)data["errors"];
			if( errors.Count > 0 )
				throw new InvalidOperationException( "Wrong username or password." );
			
			Username = (string)data["username"];
			Log( "cc login took " + sw.ElapsedMilliseconds );
			sw.Stop();
		}
		
		public ClientStartData GetConnectInfo( string hash ) {
			string uri = playUri + hash;
			string response = GetHtmlAll( uri, classicubeNetUri );
			
			int index = 0; bool success = true;
			Dictionary<string, object> root =
				(Dictionary<string, object>)Json.ParseValue( response, ref index, ref success );
			List<object> list = (List<object>)root["servers"];
			
			Dictionary<string, object> pairs = (Dictionary<string, object>)list[0];
			return new ClientStartData( Username, (string)pairs["mppass"], 
			                           (string)pairs["ip"], (string)pairs["port"] );
		}
		
		public List<ServerListEntry> GetPublicServers() {
			var sw = System.Diagnostics.Stopwatch.StartNew();
			List<ServerListEntry> servers = new List<ServerListEntry>();
			string response = GetHtmlAll( publicServersUri, classicubeNetUri );
			int index = 0; bool success = true;
			Dictionary<string, object> root =
				(Dictionary<string, object>)Json.ParseValue( response, ref index, ref success );
			List<object> list = (List<object>)root["servers"];
			
			foreach( object server in list ) {
				Dictionary<string, object> pairs = (Dictionary<string, object>)server;
				servers.Add( new ServerListEntry(
					(string)pairs["hash"], (string)pairs["name"],
					(string)pairs["players"], (string)pairs["maxplayers"],
					(string)pairs["uptime"], (string)pairs["mppass"],
					(string)pairs["ip"], (string)pairs["port"] ) );
			}
			Log( "cc servers took " + sw.ElapsedMilliseconds );
			sw.Stop();
			return servers;
		}
	}
}