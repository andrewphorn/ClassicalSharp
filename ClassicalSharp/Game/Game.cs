﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using ClassicalSharp.Commands;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Model;
using ClassicalSharp.Network;
using ClassicalSharp.Particles;
using ClassicalSharp.Renderers;
using ClassicalSharp.Selections;
using ClassicalSharp.TexturePack;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;

namespace ClassicalSharp {

	public partial class Game : GameWindow {
		
		public IGraphicsApi Graphics;
		public Map Map;
		public INetworkProcessor Network;
		
		public EntityList Players;
		public CpeListInfo[] CpePlayersList = new CpeListInfo[256];
		public LocalPlayer LocalPlayer;
		public Camera Camera;
		Camera firstPersonCam, thirdPersonCam, forwardThirdPersonCam;
		public BlockInfo BlockInfo;
		public double accumulator;
		public TerrainAtlas2D TerrainAtlas;
		public TerrainAtlas1D TerrainAtlas1D;
		public SkinType DefaultPlayerSkinType;
		public int ChunkUpdates;
		
		public MapRenderer MapRenderer;
		public MapBordersRenderer MapBordersRenderer;
		public EnvRenderer EnvRenderer;
		public WeatherRenderer WeatherRenderer;
		public Inventory Inventory;
		public IDrawer2D Drawer2D;
		
		public CommandManager CommandManager;
		public SelectionManager SelectionManager;
		public ParticleManager ParticleManager;
		public PickingRenderer Picking;
		public PickedPos SelectedPos = new PickedPos();
		public ModelCache ModelCache;
		internal string skinServer, chatInInputBuffer = null;
		internal int defaultIb;
		public bool CanUseThirdPersonCamera = true;
		FpsScreen fpsScreen;
		internal HudScreen hudScreen;
		public Events Events = new Events();
		public InputHandler InputHandler;
		public ChatLog Chat;
		public BlockHandRenderer BlockHandRenderer;
		
		public IPAddress IPAddress;
		public string Username;
		public string Mppass;
		public int Port;
		public int ViewDistance = 512;
		
		public long Vertices;
		public FrustumCulling Culling;
		int width, height;
		public AsyncDownloader AsyncDownloader;
		public Matrix4 View, Projection;
		public int MouseSensitivity = 30;
		public int ChatLines = 12;
		public bool HideGui = false, ShowFPS = true;
		internal float HudScale = 1f;
		
		public Animations Animations;
		internal int CloudsTextureId, RainTextureId, SnowTextureId;
		internal bool screenshotRequested;
		public Bitmap FontBitmap;
		internal List<WarningScreen> WarningScreens = new List<WarningScreen>();
		internal AcceptedUrls AcceptedUrls = new AcceptedUrls();
		
		public float GuiScale() {
			float scaleX = Width / 640f, scaleY = Height / 480f;
			return Math.Min( scaleX, scaleY ) * HudScale;
		}
		
		string defTexturePack = "default.zip";
		public string DefaultTexturePack {
			get {
				return File.Exists( defTexturePack )
					? defTexturePack : "default.zip"; }
			set {
				defTexturePack = value;
				Options.Set( OptionsKey.DefaultTexturePack, value );
			}
		}
		
		void LoadAtlas( Bitmap bmp ) {
			TerrainAtlas1D.Dispose();
			TerrainAtlas.Dispose();
			TerrainAtlas.UpdateState( BlockInfo, bmp );
			TerrainAtlas1D.UpdateState( TerrainAtlas );
		}
		
		public void ChangeTerrainAtlas( Bitmap newAtlas ) {
			LoadAtlas( newAtlas );
			Events.RaiseTerrainAtlasChanged();
		}
		
		protected override void OnLoad( EventArgs e ) {
			#if !USE_DX
			Graphics = new OpenGLApi();
			#else
			Graphics = new Direct3D9Api( this );
			#endif
			Graphics.MakeGraphicsInfo();
			Players = new EntityList( this );
			
			Options.Load();
			AcceptedUrls.Load();
			Players.ShowHoveredNames = Options.GetBool( OptionsKey.ShowHoveredNames, true );
			ViewDistance = Options.GetInt( OptionsKey.ViewDist, 16, 4096, 512 );
			InputHandler = new InputHandler( this );
			Chat = new ChatLog( this );
			HudScale = Options.GetFloat( OptionsKey.HudScale, 0.5f, 2f, 1 );
			defaultIb = Graphics.MakeDefaultIb();
			MouseSensitivity = Options.GetInt( OptionsKey.Sensitivity, 1, 100, 30 );
			BlockInfo = new BlockInfo();
			BlockInfo.Init();
			ChatLines = Options.GetInt( OptionsKey.ChatLines, 1, 30, 12 );
			ModelCache = new ModelCache( this );
			ModelCache.InitCache();
			AsyncDownloader = new AsyncDownloader( skinServer );
			Drawer2D = new GdiPlusDrawer2D( Graphics );
			Drawer2D.UseBitmappedChat = !Options.GetBool( OptionsKey.ArialChatFont, false );
			
			TerrainAtlas1D = new TerrainAtlas1D( Graphics );
			TerrainAtlas = new TerrainAtlas2D( Graphics, Drawer2D );
			Animations = new Animations( this );
			defTexturePack = Options.Get( OptionsKey.DefaultTexturePack ) ?? "default.zip";
			TexturePackExtractor extractor = new TexturePackExtractor();
			extractor.Extract( "default.zip", this ); // in case the default texture pack doesn't have all required textures
			if( defTexturePack != "default.zip" )
				extractor.Extract( DefaultTexturePack, this );
			Inventory = new Inventory( this );
			
			BlockInfo.SetDefaultBlockPermissions( Inventory.CanPlace, Inventory.CanDelete );
			Map = new Map( this );
			LocalPlayer = new LocalPlayer( this );
			LocalPlayer.SpeedMultiplier = Options.GetInt( OptionsKey.Speed, 1, 50, 10 );
			Players[255] = LocalPlayer;
			width = Width;
			height = Height;
			MapRenderer = new MapRenderer( this );
			MapBordersRenderer = new MapBordersRenderer( this );
			EnvRenderer = new StandardEnvRenderer( this );
			if( IPAddress == null ) {
				Network = new Singleplayer.SinglePlayerServer( this );
			} else {
				Network = new NetworkProcessor( this );
			}
			Graphics.LostContextFunction = Network.Tick;
			
			firstPersonCam = new FirstPersonCamera( this );
			thirdPersonCam = new ThirdPersonCamera( this );
			forwardThirdPersonCam = new ForwardThirdPersonCamera( this );
			Camera = firstPersonCam;
			CommandManager = new CommandManager();
			CommandManager.Init( this );
			SelectionManager = new SelectionManager( this );
			ParticleManager = new ParticleManager( this );
			WeatherRenderer = new WeatherRenderer( this );
			WeatherRenderer.Init();
			BlockHandRenderer = new BlockHandRenderer( this );
			BlockHandRenderer.Init();
			
			bool vsync = Options.GetBool( OptionsKey.VSync, true );
			Graphics.SetVSync( this, vsync );
			Graphics.DepthTest = true;
			Graphics.DepthTestFunc( CompareFunc.LessEqual );
			//Graphics.DepthWrite = true;
			Graphics.AlphaBlendFunc( BlendFunc.SourceAlpha, BlendFunc.InvSourceAlpha );
			Graphics.AlphaTestFunc( CompareFunc.Greater, 0.5f );
			fpsScreen = new FpsScreen( this );
			fpsScreen.Init();
			hudScreen = new HudScreen( this );
			hudScreen.Init();
			Culling = new FrustumCulling();
			EnvRenderer.Init();
			MapBordersRenderer.Init();
			Picking = new PickingRenderer( this );
			
			string connectString = "Connecting to " + IPAddress + ":" + Port +  "..";
			Graphics.WarnIfNecessary( Chat );
			SetNewScreen( new LoadingMapScreen( this, connectString, "Reticulating splines" ) );
			Network.Connect( IPAddress, Port );
		}
		
		public void SetViewDistance( int distance ) {
			ViewDistance = distance;
			Utils.LogDebug( "setting view distance to: " + distance );
			Options.Set( OptionsKey.ViewDist, distance );
			Events.RaiseViewDistanceChanged();
			UpdateProjection();
		}
		
		/// <summary> Gets whether the active screen handles all input. </summary>
		public bool ScreenLockedInput {
			get { return activeScreen == null ? hudScreen.HandlesAllInput :
					activeScreen.HandlesAllInput; } // inlined here.
		}
		
		public Screen GetActiveScreen {
			get { return activeScreen == null ? hudScreen : activeScreen; }
		}
		
		const int ticksFrequency = 20;
		const double ticksPeriod = 1.0 / ticksFrequency;
		const double imageCheckPeriod = 30.0;
		const double cameraPeriod = 1.0 / 120.0;
		double ticksAccumulator = 0, imageCheckAccumulator = 0, cameraAccumulator = 0;
		
		protected override void OnRenderFrame( FrameEventArgs e ) {
			Graphics.BeginFrame( this );
			Graphics.BindIb( defaultIb );
			accumulator += e.Time;
			Vertices = 0;
			if( !Focused && !ScreenLockedInput )
				SetNewScreen( new PauseScreen( this ) );
			
			base.OnRenderFrame( e );
			CheckScheduledTasks( e.Time );
			float t = (float)( ticksAccumulator / ticksPeriod );
			LocalPlayer.SetInterpPosition( t );
			
			Graphics.Clear();
			Graphics.SetMatrixMode( MatrixType.Modelview );
			Matrix4 modelView = Camera.GetView();
			View = modelView;
			Graphics.LoadMatrix( ref modelView );
			Culling.CalcFrustumEquations( ref Projection, ref modelView );
			
			bool visible = activeScreen == null || !activeScreen.BlocksWorld;
			if( visible ) {
				Players.RenderModels( Graphics, e.Time, t );
				Players.RenderNames( Graphics, e.Time, t );
				ParticleManager.Render( e.Time, t );
				Camera.GetPickedBlock( SelectedPos ); // TODO: only pick when necessary
				EnvRenderer.Render( e.Time );
				if( SelectedPos.Valid && !HideGui )
					Picking.Render( e.Time, SelectedPos );
				MapRenderer.Render( e.Time );
				SelectionManager.Render( e.Time );
				WeatherRenderer.Render( e.Time );
				Players.RenderHoveredNames( Graphics, e.Time, t );
				
				bool left = IsMousePressed( MouseButton.Left );
				bool middle = IsMousePressed( MouseButton.Middle );
				bool right = IsMousePressed( MouseButton.Right );
				InputHandler.PickBlocks( true, left, middle, right );
				if( !HideGui )
					BlockHandRenderer.Render( e.Time );
			} else {
				SelectedPos.SetAsInvalid();
			}
			
			
			Graphics.Mode2D( Width, Height, EnvRenderer is StandardEnvRenderer );
			fpsScreen.Render( e.Time );
			if( activeScreen == null || !activeScreen.HidesHud )
				hudScreen.Render( e.Time );
			if( activeScreen != null )
				activeScreen.Render( e.Time );
			Graphics.Mode3D( EnvRenderer is StandardEnvRenderer );
			
			if( screenshotRequested )
				TakeScreenshot();
			Graphics.EndFrame( this );
		}
		
		void CheckScheduledTasks( double time ) {
			imageCheckAccumulator += time;
			ticksAccumulator += time;
			cameraAccumulator += time;
			
			if( imageCheckAccumulator > imageCheckPeriod ) {
				imageCheckAccumulator -= imageCheckPeriod;
				AsyncDownloader.PurgeOldEntries( 10 );
			}
			
			int ticksThisFrame = 0;
			while( ticksAccumulator >= ticksPeriod ) {
				Network.Tick( ticksPeriod );
				Players.Tick( ticksPeriod );
				ParticleManager.Tick( ticksPeriod );
				Animations.Tick( ticksPeriod );
				ticksThisFrame++;
				ticksAccumulator -= ticksPeriod;
			}
			
			while( cameraAccumulator >= cameraPeriod ) {
				Camera.Tick( ticksPeriod );
				cameraAccumulator -= cameraPeriod;
			}
			
			if( ticksThisFrame > ticksFrequency / 3 )
				Utils.LogDebug( "Falling behind (did {0} ticks this frame)", ticksThisFrame );
		}
		
		void TakeScreenshot() {
			if( !Directory.Exists( "screenshots" ) ) {
				Directory.CreateDirectory( "screenshots" );
			}
			
			string timestamp = DateTime.Now.ToString( "dd-MM-yyyy-HH-mm-ss" );
			string file = "screenshot_" + timestamp + ".png";
			string path = Path.Combine( "screenshots", file );
			Graphics.TakeScreenshot( path, ClientSize );
			Chat.Add( "&eTaken screenshot as: " + file );
			screenshotRequested = false;
		}
		
		public void UpdateProjection() {
			Matrix4 projection = Camera.GetProjection();
			Projection = projection;
			Graphics.SetMatrixMode( MatrixType.Projection );
			Graphics.LoadMatrix( ref projection );
			Graphics.SetMatrixMode( MatrixType.Modelview );
		}
		
		protected override void OnResize( object sender, EventArgs e ) {
			base.OnResize( sender, e );
			Graphics.OnWindowResize( this );
			UpdateProjection();
			if( activeScreen != null )
				activeScreen.OnResize( width, height, Width, Height );
			hudScreen.OnResize( width, height, Width, Height );
			width = Width;
			height = Height;
		}
		
		public void Disconnect( string title, string reason ) {
			SetNewScreen( new ErrorScreen( this, title, reason ) );
			Map.Reset();
			Map.mapData = null;
			GC.Collect();
		}
		
		internal Screen activeScreen;
		public void SetNewScreen( Screen screen ) {
			// don't switch to the new screen immediately if the user
			// is currently looking at a warning dialog.
			if( activeScreen is WarningScreen ) {
				WarningScreen warning = (WarningScreen)activeScreen;
				if( warning.lastScreen != null ) 
					warning.lastScreen.Dispose();
				
				warning.lastScreen = screen;
				if( warning.lastScreen != null ) 
					screen.Init();
				return;
			}
			InputHandler.ScreenChanged( activeScreen, screen );
			if( activeScreen != null )
				activeScreen.Dispose();
			
			if( screen == null ) {
				hudScreen.GainFocus();
			} else if( activeScreen == null ) {
				hudScreen.LoseFocus();
			}
			
			if( screen != null )
				screen.Init();
			activeScreen = screen;
		}
		
		public void RefreshHud() {
			hudScreen.Dispose();
			hudScreen.Init();
		}
		
		public void ShowWarning( WarningScreen screen ) {
			if( !(activeScreen is WarningScreen) ) {
				screen.lastScreen = activeScreen;
				activeScreen = screen;
				
				screen.lastCursorVisible = CursorVisible;
				if( !CursorVisible) CursorVisible = true;
			} else {
				screen.lastCursorVisible = WarningScreens[0].lastCursorVisible;				
			}
			WarningScreens.Add( screen );
			screen.Init();
		}
		
		public void SetCamera( bool thirdPerson ) {
			PerspectiveCamera oldCam = (PerspectiveCamera)Camera;
			Camera = (thirdPerson && CanUseThirdPersonCamera) ?
				(Camera is FirstPersonCamera ? thirdPersonCam : forwardThirdPersonCam ) :
				firstPersonCam;
			PerspectiveCamera newCam = (PerspectiveCamera)Camera;
			newCam.delta = oldCam.delta;
			newCam.previous = oldCam.previous;
			UpdateProjection();
		}
		
		public void UpdateBlock( int x, int y, int z, byte block ) {
			int oldHeight = Map.GetLightHeight( x, z ) + 1;
			Map.SetBlock( x, y, z, block );
			int newHeight = Map.GetLightHeight( x, z ) + 1;
			MapRenderer.RedrawBlock( x, y, z, block, oldHeight, newHeight );
		}
		
		public bool IsKeyDown( Key key ) { return InputHandler.IsKeyDown( key ); }
		
		public bool IsKeyDown( KeyBinding binding ) { return InputHandler.IsKeyDown( binding ); }
		
		public bool IsMousePressed( MouseButton button ) { return InputHandler.IsMousePressed( button ); }
		
		public Key Mapping( KeyBinding mapping ) { return InputHandler.Keys[mapping]; }
		
		public override void Dispose() {
			MapRenderer.Dispose();
			MapBordersRenderer.Dispose();
			EnvRenderer.Dispose();
			WeatherRenderer.Dispose();
			SetNewScreen( null );
			fpsScreen.Dispose();
			SelectionManager.Dispose();
			TerrainAtlas.Dispose();
			TerrainAtlas1D.Dispose();
			ModelCache.Dispose();
			Picking.Dispose();
			ParticleManager.Dispose();
			Players.Dispose();
			AsyncDownloader.Dispose();
			
			Chat.Dispose();
			if( activeScreen != null )
				activeScreen.Dispose();
			Graphics.DeleteIb( defaultIb );
			Graphics.Dispose();
			Drawer2D.DisposeInstance();
			Animations.Dispose();
			Graphics.DeleteTexture( ref CloudsTextureId );
			Graphics.DeleteTexture( ref RainTextureId );
			Graphics.DeleteTexture( ref SnowTextureId );
			
			if( Options.HasChanged )
				Options.Save();
			base.Dispose();
		}
		
		internal bool CanPick( byte block ) {
			return !(block == 0 || (BlockInfo.IsLiquid[block] &&
			                        !(Inventory.CanPlace[block] && Inventory.CanDelete[block])));
		}
		
		public Game( string username, string mppass, string skinServer,
		            bool nullContext, int width, int height )
			: base( width, height, GraphicsMode.Default, Program.AppName, nullContext, 0, DisplayDevice.Default ) {
			Username = username;
			Mppass = mppass;
			this.skinServer = skinServer;
		}
	}

	public sealed class CpeListInfo {
		
		public byte NameId;
		
		/// <summary> Unformatted name of the player for autocompletion, etc. </summary>
		/// <remarks> Colour codes are always removed from this. </remarks>
		public string PlayerName;
		
		/// <summary> Formatted name for display in the player list. </summary>
		/// <remarks> Can include colour codes. </remarks>
		public string ListName;
		
		/// <summary> Name of the group this player is in. </summary>
		/// <remarks> Can include colour codes. </remarks>
		public string GroupName;
		
		/// <summary> Player's rank within the group. (0 is highest) </summary>
		/// <remarks> Multiple group members can share the same rank,
		/// so a player's group rank is not a unique identifier. </remarks>
		public byte GroupRank;
		
		public CpeListInfo( byte id, string playerName, string listName,
		                   string groupName, byte groupRank ) {
			NameId = id;
			PlayerName = playerName;
			ListName = listName;
			GroupName = groupName;
			GroupRank = groupRank;
		}
	}
}