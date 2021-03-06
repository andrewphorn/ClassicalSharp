﻿using System;
using System.Drawing;
using System.IO;
using ClassicalSharp;
using ClassicalSharp.TexturePack;

namespace Launcher2 {

	public sealed partial class LauncherWindow {
		
		internal FastColour clearColour = new FastColour( 30, 30, 30 );
		
		bool useTexture = false;
		internal void TryLoadTexturePack() {
			Options.Load();
			string texPack = Options.Get( OptionsKey.DefaultTexturePack ) ?? "default.zip";
			
			if( !File.Exists( texPack ) )
				texPack = "default.zip";
			if( !File.Exists( texPack ) )
				return;		
			
			using( Stream fs = new FileStream( texPack, FileMode.Open, FileAccess.Read, FileShare.Read ) ) {
				ZipReader reader = new ZipReader();
				
				reader.ShouldProcessZipEntry = (f) => f == "terrain.png";
				reader.ProcessZipEntry = ProcessZipEntry;
				reader.Extract( fs );
			}
		}
		
		Bitmap dirtBmp;
		FastBitmap dirtFastBmp;
		int elementSize;
		
		void ProcessZipEntry( string filename, byte[] data, ZipEntry entry ) {
			MemoryStream stream = new MemoryStream( data );
			using( Bitmap bmp = new Bitmap( stream ) ) {
				using( FastBitmap fastBmp = new FastBitmap( bmp, true ) ) {
					elementSize = bmp.Width / 16;
					dirtBmp = new Bitmap( elementSize, elementSize );
					dirtFastBmp = new FastBitmap( dirtBmp, true );
					FastBitmap.MovePortion( elementSize * 2, 0, 0, 0, fastBmp, dirtFastBmp, elementSize );
				}
			}
			useTexture = true;
		}
		
		public void MakeBackground() {
			if( Framebuffer != null )
				Framebuffer.Dispose();
			
			Framebuffer = new Bitmap( Width, Height );
			using( IDrawer2D drawer = Drawer ) {
				drawer.SetBitmap( Framebuffer );
				if( useTexture )
					ClearDirt( 0, 0, Width, Height );
				else
					Drawer.Clear( clearColour );
				
				DrawTextArgs args1 = new DrawTextArgs( "&eClassical", logoItalicFont, true );
				Size size1 = drawer.MeasureSize( ref args1 );
				DrawTextArgs args2 = new DrawTextArgs( "&eSharp", logoFont, true );
				Size size2 = drawer.MeasureSize( ref args2 );
				
				int xStart = Width / 2 - (size1.Width + size2.Width ) / 2;
				drawer.DrawText( ref args1, xStart, 20 );
				drawer.DrawText( ref args2, xStart + size1.Width, 20 );
			}
			Dirty = true;
		}
		
		public void ClearArea( int x, int y, int width, int height ) {
			if( useTexture )
				ClearDirt( x, y, width, height );
			else
				Drawer.Clear( clearColour, x, y, width, height );
		}
		
		void ClearDirt( int x, int y, int width, int height ) {
			using( FastBitmap dst = new FastBitmap( Framebuffer, true ) ) {
				Rectangle srcRect = new Rectangle( 0, 0, elementSize, elementSize );
				int tileSize = 64;
				int xMax = x + width, xOrig = x, yMax = y + height;
				
				for( ; y < yMax; y += tileSize ) {
					for( x = xOrig; x < xMax; x += tileSize ) {
						int x2 = Math.Min( x + tileSize,  Math.Min( x + width, Width ) );
						int y2 = Math.Min( y + tileSize, Math.Min( y + height, Height ) );
						
						Size size = new Size( tileSize, tileSize );
						Rectangle dstRect = new Rectangle( x, y, x2 - x, y2 - y );
						CopyScaledPixels( dirtFastBmp, dst, size, srcRect, dstRect, 128 );
					}
				}
			}
		}
		
		unsafe static void CopyScaledPixels( FastBitmap src, FastBitmap dst, Size scale,
		                                    Rectangle srcRect, Rectangle dstRect, byte rgbScale ) {
			int srcWidth = srcRect.Width, dstWidth = dstRect.Width;
			int srcHeight = srcRect.Height, dstHeight = dstRect.Height;
			int srcX = srcRect.X, dstX = dstRect.X;
			int srcY = srcRect.Y, dstY = dstRect.Y;
			int scaleWidth = scale.Width, scaleHeight = scale.Height;
			
			for( int yy = 0; yy < dstHeight; yy++ ) {
				int scaledY = yy * srcHeight / scaleHeight;
				int* srcRow = src.GetRowPtr( srcY + scaledY );
				int* dstRow = dst.GetRowPtr( dstY + yy );
				
				for( int xx = 0; xx < dstWidth; xx++ ) {
					int scaledX = xx * srcWidth / scaleWidth;
					int pixel = srcRow[srcX + scaledX];
					
					int col = pixel & ~0xFFFFFF; // keep a but clear rgb
					col |= ((pixel & 0xFF) * rgbScale / 255);
					col |= (((pixel >> 8) & 0xFF) * rgbScale / 255) << 8;
					col |= (((pixel >> 16) & 0xFF) * rgbScale / 255) << 16;
					dstRow[dstX + xx] = col;
				}
			}
		}
	}
}
