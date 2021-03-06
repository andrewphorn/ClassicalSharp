﻿using System;

namespace ClassicalSharp {
	
	public sealed class WrappableStringBuffer : StringBuffer {
		
		char[] wrap;
		
		public WrappableStringBuffer( int capacity ) : base( capacity ) {
			wrap = new char[capacity];
		}
		
		public void WordWrap( ref string[] lines, ref int[] lineLens, int lineSize ) {
			int len = Length;
			for( int i = 0; i < lines.Length; i++ ) {
				lines[i] = null;
				lineLens[i] = 0;
			}
			
			// Need to make a copy because we mutate the characters.
			char[] realText = value;
			MakeWrapCopy();
			
			int linesCount = 0;
			for( int index = 0; index < capacity; index += lineSize ) {
				if( value[index] == '\0' )
					break;
				
				int lineEnd = index + (lineSize - 1);
				int nextLine = index + lineSize;
				linesCount++;
				
				// Do we need word wrapping?
				bool needWrap = !IsWrapper( value[lineEnd] ) && nextLine < capacity && !IsWrapper( value[nextLine] );
				int wrappedLen = needWrap ? WrapLine( index, lineSize ) : lineSize;
				
				// Calculate the maximum size of this line
				int lineLen = lineSize;
				for( int i = lineEnd; i >= index; i-- ) {
					if( value[i] != '\0' ) break;
					lineLen--;
				}
				lineLens[index / lineSize] = Math.Min( lineLen, wrappedLen );
			}
			
			// Output the used lines
			OutputLines( ref lines, linesCount, lineSize, lineLens );
			value = realText;
		}
		
		void MakeWrapCopy() {
			int len = Length;
			for( int i = 0; i < len; i++ )
				wrap[i] = value[i];
			
			for( int i = len; i < capacity; i++ )
				wrap[i] = '\0';		
			value = wrap;
		}
		
		void OutputLines( ref string[] lines, int linesCount, int lineSize, int[] lineLens ) {
			for( int i = 0; i < capacity; i++ ) {
				if( value[i] == '\0' ) value[i] = ' ';
			}
			
			for( int i = 0; i < Math.Max( 1, linesCount ); i++ ) {
				lines[i] = new String( value, i * lineSize, lineLens[i] );
			}
		}
		
		int WrapLine( int index, int lineSize ) {
			int lineEnd = index + (lineSize - 1);
			// wrap - but we don't want to wrap if the entire line is filled.
			for( int i = lineEnd; i >= index + 1; i-- ) {
				if( IsWrapper( value[i] ) ) {
					for( int j = lineEnd; j >= i + 1; j-- ) {
						InsertAt( index + lineSize, value[j] );
						value[j] = ' ';
					}
					return (i + 1) - index;
				}
			}
			return lineSize;
		}
		
		bool IsWrapper( char c ) {
			return c == '\0' || c == ' ' || c == '-' || c == '>'
				|| c == '<' || c == '/' || c == '\\';
		}
	}
}
