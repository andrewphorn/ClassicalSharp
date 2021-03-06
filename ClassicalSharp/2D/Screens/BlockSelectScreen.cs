﻿using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;
using OpenTK.Input;

namespace ClassicalSharp {
	
	public class BlockSelectScreen : Screen {
		
		public BlockSelectScreen( Game game ) : base( game ) {
			font = new Font( "Arial", 13 );
		}
		
		Block[] blocksTable;
		Texture blockInfoTexture;
		const int blocksPerRow = 10;
		int selectedIndex, rows;
		int startX, startY, blockSize;
		float selBlockExpand;
		readonly Font font;
		StringBuffer buffer = new StringBuffer( 96 );
		
		static FastColour backCol = new FastColour( 30, 30, 30, 200 );
		static FastColour selCol = new FastColour( 213, 200, 223 );
		public override void Render( double delta ) {
			graphicsApi.Draw2DQuad( startX - 5, startY - 30 - 5, blocksPerRow * blockSize + 10,
			                       rows * blockSize + 30 + 10, backCol );
			graphicsApi.Texturing = true;
			graphicsApi.BindTexture( game.TerrainAtlas.TexId );
			graphicsApi.SetBatchFormat( VertexFormat.Pos3fTex2fCol4b );
			
			for( int i = 0; i < blocksTable.Length; i++ ) {
				int x, y;
				GetCoords( i, out x, out y );
				
				// We want to always draw the selected block on top of others
				if( i == selectedIndex ) continue;
				IsometricBlockDrawer.Draw( game, (byte)blocksTable[i], blockSize * 0.7f / 2f,
				                          x + blockSize / 2, y + blockSize / 2 );
			}
			
			if( selectedIndex != -1 ) {
				int x, y;
				GetCoords( selectedIndex, out x, out y );
				IsometricBlockDrawer.Draw( game, (byte)blocksTable[selectedIndex], (blockSize + selBlockExpand) * 0.7f / 2,
				                          x + blockSize / 2, y + blockSize / 2 );
			}
			
			if( blockInfoTexture.IsValid )
				blockInfoTexture.Render( graphicsApi );
			game.hudScreen.RenderHotbar( delta );
			graphicsApi.Texturing = false;
		}
		
		void GetCoords( int i, out int x, out int y ) {
			int col = i % blocksPerRow;
			int row = i / blocksPerRow;
			x = startX + blockSize * col;
			y = startY + blockSize * row + 3;
		}
		
		public override void Dispose() {
			font.Dispose();
			graphicsApi.DeleteTexture( ref blockInfoTexture );
			game.Events.BlockPermissionsChanged -= BlockPermissionsChanged;
		}
		
		public override void OnResize( int oldWidth, int oldHeight, int width, int height ) {
			blockSize = (int)(50 * Math.Sqrt(game.GuiScale()));
			selBlockExpand = (float)(25 * Math.Sqrt(game.GuiScale()));
			
			startX = game.Width / 2 - (blockSize * blocksPerRow) / 2;
			startY = game.Height / 2 - (rows * blockSize) / 2;
			blockInfoTexture.X1 = startX + (blockSize * blocksPerRow) / 2 - blockInfoTexture.Width / 2;
			blockInfoTexture.Y1 = startY - blockInfoTexture.Height - 5;
		}
		
		public override void Init() {
			blockSize = (int)(50 * Math.Sqrt(game.GuiScale()));
			selBlockExpand = (float)(25 * Math.Sqrt(game.GuiScale()));
			game.Events.BlockPermissionsChanged += BlockPermissionsChanged;
			RecreateBlockTextures();
		}

		void BlockPermissionsChanged( object sender, EventArgs e ) {
			RecreateBlockTextures();
			if( selectedIndex >= blocksTable.Length ) {
				selectedIndex = blocksTable.Length - 1;
			}
			RecreateBlockInfoTexture();
		}
		
		void UpdateBlockInfoString( Block block ) {
			int index = 0;
			buffer.Clear();
			buffer.Append( ref index, "&f" );
			if( block == Block.TNT ) {
				buffer.Append( ref index, "TNT" );
			} else {
				string value = game.BlockInfo.Name[(byte)block];
				if( (byte)block < BlockInfo.CpeBlocksCount ) {
					SplitUppercase( value, ref index );
				} else {
					buffer.Append( ref index, value );
				}
			}
			buffer.Append( ref index, " (can place: " );
			buffer.Append( ref index, game.Inventory.CanPlace[(int)block] ? "&aYes" : "&cNo" );
			buffer.Append( ref index, "&f, can delete: " );
			buffer.Append( ref index, game.Inventory.CanDelete[(int)block] ? "&aYes" : "&cNo" );
			buffer.Append( ref index, "&f)" );
		}
		
		void SplitUppercase( string value, ref int index ) {
			for( int i = 0; i < value.Length; i++ ) {
				char c = value[i];
				if( Char.IsUpper( c ) && i > 0 ) {
					buffer.Append( ref index, ' ' );
				}
				buffer.Append( ref index, c );
			}
		}
		
		int lastCreatedIndex = -1000;
		void RecreateBlockInfoTexture() {
			if( selectedIndex == lastCreatedIndex ) return;
			lastCreatedIndex = selectedIndex;
			
			graphicsApi.DeleteTexture( ref blockInfoTexture );
			if( selectedIndex == -1 ) return;
			
			Block block = blocksTable[selectedIndex];
			UpdateBlockInfoString( block );
			string value = buffer.GetString();
			
			DrawTextArgs args = new DrawTextArgs( value, font, true );
			Size size = game.Drawer2D.MeasureSize( ref args );
			int x = startX + (blockSize * blocksPerRow) / 2 - size.Width / 2;
			int y = startY - size.Height - 5;
			
			args.SkipPartsCheck = true;
			blockInfoTexture = game.Drawer2D.MakeTextTexture( ref args, x, y );
		}
		
		void RecreateBlockTextures() {
			int blocksCount = 0;
			for( int tile = 1; tile < BlockInfo.BlocksCount; tile++ ) {
				if( game.Inventory.CanPlace[tile] || game.Inventory.CanDelete[tile] )
					blocksCount++;
			}
			
			rows = Utils.CeilDiv( blocksCount, blocksPerRow );
			startX = game.Width / 2 - (blockSize * blocksPerRow) / 2;
			startY = game.Height / 2 - (rows * blockSize) / 2;
			blocksTable = new Block[blocksCount];
			
			int tableIndex = 0;
			for( int tile = 1; tile < BlockInfo.BlocksCount; tile++ ) {
				if( game.Inventory.CanPlace[tile] || game.Inventory.CanDelete[tile] )
					blocksTable[tableIndex++] = (Block)tile;
			}
		}
		
		public override bool HandlesAllInput {
			get { return true; }
		}
		
		public override bool HandlesMouseMove( int mouseX, int mouseY ) {
			selectedIndex = -1;
			if( Contains( startX, startY, blocksPerRow * blockSize, rows * blockSize, mouseX, mouseY ) ) {
				for( int i = 0; i < blocksTable.Length; i++ ) {
					int x, y;
					GetCoords( i, out x, out y );
					
					if( Contains( x, y, blockSize, blockSize, mouseX, mouseY ) ) {
						selectedIndex = i;
						break;
					}
				}
			}
			RecreateBlockInfoTexture();
			return true;
		}
		
		public override bool HandlesMouseClick( int mouseX, int mouseY, MouseButton button ) {
			if( button == MouseButton.Left && selectedIndex != -1 ) {
				game.Inventory.HeldBlock = blocksTable[selectedIndex];
				game.SetNewScreen( null );
			}
			return true;
		}
		
		public override bool HandlesKeyDown( Key key ) {
			if( key == game.Mapping( KeyBinding.PauseOrExit ) ||
			   key == game.Mapping( KeyBinding.OpenInventory ) ) {
				game.SetNewScreen( null );
			}
			return true;
		}
		
		static bool Contains( int recX, int recY, int width, int height, int x, int y ) {
			return x >= recX && y >= recY && x < recX + width && y < recY + height;
		}
	}
}
