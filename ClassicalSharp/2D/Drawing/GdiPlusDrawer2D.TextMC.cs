﻿using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;

namespace ClassicalSharp {

	public unsafe sealed partial class GdiPlusDrawer2D {
		
		// TODO: underline
		FastBitmap fontPixels;
		int boxSize;
		const int italicSize = 8;
		
		public override void SetFontBitmap( Bitmap bmp ) {
			FontBitmap = bmp;
			boxSize = FontBitmap.Width / 16;
			fontPixels = new FastBitmap( FontBitmap, true );
			CalculateTextWidths();
		}
		
		int[] widths = new int[256];
		void CalculateTextWidths() {
			for( int i = 0; i < 256; i++ )
				MakeTile( i, (i & 0x0F) * boxSize, (i >> 4) * boxSize );
			widths[(int)' '] = boxSize / 4;
		}
		
		void MakeTile( int i, int tileX, int tileY ) {
			// find first column (from right) where there is a solid pixel
			for( int x = boxSize - 1; x >= 0; x-- ) {
				for( int y = 0; y < boxSize; y++ ) {
					int pixel = fontPixels.GetRowPtr( tileY + y )[tileX + x];
					byte a = (byte)(pixel >> 24);
					if( a >= 127 ) { // found a solid pixel
						widths[i] = x + 1;
						return;
					}
				}
			}
			widths[i] = 0;
		}
		
		public override void DrawBitmappedText( ref DrawTextArgs args, int x, int y ) {
			if( !args.SkipPartsCheck )
				GetTextParts( args.Text );
			
			using( FastBitmap fastBmp = new FastBitmap( curBmp, true ) )
				DrawTextImpl( fastBmp, ref args, x, y );
		}
		
		void DrawTextImpl( FastBitmap fastBmp, ref DrawTextArgs args, int x, int y ) {
			bool italic = args.Font.Style == FontStyle.Italic;
			bool underline = args.Font.Style == FontStyle.Underline;
			if( args.UseShadow ) {
				int offset = ShadowOffset( args.Font.Size );
				int shadowX = x + offset, shadowY = y + offset;
				
				for( int i = 0; i < parts.Count; i++ ) {
					TextPart part = parts[i];
					part.TextColour = FastColour.Black;
					int orignX = shadowX;
					DrawPart( fastBmp, args.Font, ref shadowX, shadowY, part );
					if( underline )
						DrawUnderline( fastBmp, part.TextColour, orignX, shadowX, 0, ref args );
				}
			}
			
			for( int i = 0; i < parts.Count; i++ ) {
				TextPart part = parts[i];
				int orignX = x;
				DrawPart( fastBmp, args.Font, ref x, y, part );
				if( underline )
					DrawUnderline( fastBmp, part.TextColour, orignX, x, -2, ref args );
			}
			
		}
		
		void DrawPart( FastBitmap fastBmp, Font font, ref int x, int y, TextPart part ) {
			string text = part.Text;
			FastColour textCol = part.TextColour;
			float point = font.Size;
			int xMul = font.Style == FontStyle.Italic ? 1 : 0;
			int originX = x;
			
			foreach( char c in text ) {
				int coords = ConvertToCP437( c );
				int srcX = (coords & 0x0F) * boxSize;
				int srcY = (coords >> 4) * boxSize;
				
				int srcWidth = widths[coords], dstWidth = PtToPx( point, srcWidth );
				int srcHeight = boxSize, dstHeight = PtToPx( point, srcHeight );
				for( int yy = 0; yy < dstHeight; yy++ ) {
					int fontY = srcY + yy * srcHeight / dstHeight;
					int* fontRow = fontPixels.GetRowPtr( fontY );
					int* dstRow = fastBmp.GetRowPtr( y + yy );
					int xOffset = xMul * ((dstHeight - 1 - yy) / italicSize);
					
					for( int xx = 0; xx < dstWidth; xx++ ) {
						int fontX = srcX + xx * srcWidth / dstWidth;
						int pixel = fontRow[fontX];
						if( (byte)(pixel >> 24) == 0 ) continue;
						
						int col = pixel & ~0xFFFFFF;
						col |= ((pixel & 0xFF) * textCol.B / 255);
						col |= (((pixel >> 8) & 0xFF) * textCol.G / 255) << 8;
						col |= (((pixel >> 16) & 0xFF) * textCol.R / 255) << 16;
						dstRow[x + xx + xOffset] = col;
					}
				}
				x += PtToPx( point, srcWidth + 1 );
			}
		}
		
		void DrawUnderline( FastBitmap fastBmp, FastColour textCol, int startX, int endX,
		                   int yOffset, ref DrawTextArgs args ) {
			int height = PtToPx( args.Font.Size, boxSize );
			int offset = ShadowOffset( args.Font.Size );
			if( args.UseShadow )
				height += offset;
			
			for( int yy = height - offset; yy < height; yy++ ) {
				int* dstRow = fastBmp.GetRowPtr( yy + yOffset );
				for( int xx = startX; xx < endX; xx++ )
					dstRow[xx] = textCol.ToArgb();
			}
		}
		
		public override Size MeasureBitmappedSize( ref DrawTextArgs args ) {
			GetTextParts( args.Text );
			if( parts.Count == 0 ) 
				return Size.Empty;
			float point = args.Font.Size;
			Size total = new Size( 0, PtToPx( point, boxSize ) );
			
			for( int i = 0; i < parts.Count; i++ ) {
				foreach( char c in parts[i].Text ) {
					int coords = ConvertToCP437( c );
					total.Width += PtToPx( point, widths[coords] + 1 );
				}
			}
			
			if( args.Font.Style == FontStyle.Italic )
				total.Width += Utils.CeilDiv( total.Height, italicSize );
			if( args.UseShadow ) {
				int offset = ShadowOffset( args.Font.Size );
				total.Width += offset; total.Height += offset;
			}
			return total;
		}
		
		static int ConvertToCP437( char c ) {
			if( c >= ' ' && c <= '~')
				return (int)c;
			
			int cIndex = Utils.ControlCharReplacements.IndexOf( c );
			if( cIndex >= 0 ) return cIndex;
			int eIndex = Utils.ExtendedCharReplacements.IndexOf( c );
			if( eIndex >= 0 ) return 127 + eIndex;
			return (int)'?';
		}
		
		static int ShadowOffset( float fontSize ) {
			if( fontSize < 9.9f ) return 1;
			if( fontSize < 24.9f ) return 2;
			return 3;
		}
		
		int PtToPx( int point ) {
			return (int)Math.Ceiling( (float)point / 72 * 96 ); // TODO: non 96 dpi?
		}
		
		int PtToPx( float point, float value ) {
			return (int)Math.Ceiling( (value / boxSize) * point / 72f * 96f );
		}
		
		void DisposeBitmappedText() {
			fontPixels.Dispose();
			FontBitmap.Dispose();
		}
	}
}
