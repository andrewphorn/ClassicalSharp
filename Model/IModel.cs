﻿using System;
using ClassicalSharp.GraphicsAPI;
using ClassicalSharp.Renderers;
using OpenTK;

namespace ClassicalSharp.Model {

	public abstract class IModel {		
		
		protected Game window;
		protected IGraphicsApi graphics;
		
		public IModel( Game window ) {
			this.window = window;
			graphics = window.Graphics;
		}
		
		public abstract float NameYOffset { get; }
		
		public abstract void RenderModel( Player player, PlayerRenderer renderer );
		
		public abstract void Dispose();
		
		public int DefaultSkinTextureId; //{ get; protected set; }
		
		protected FastColour col = new FastColour( 178, 178, 178 );
		protected VertexPos3fTex2fCol4b[] vertices;
		protected int index = 0;
		
		protected ModelPart MakePart( int x, int y, int sidesW, int sidesH, int endsW, int endsH, int bodyW, int bodyH,
		                   float x1, float x2, float y1, float y2, float z1, float z2, bool _64x64 ) {
			index = 0;
			YPlane( x + sidesW, y, endsW, endsH, x2, x1, z2, z1, y2, _64x64 ); // top
			YPlane( x + sidesW + bodyW, y, endsW, endsH, x2, x1, z1, z2, y1, _64x64 ); // bottom
			ZPlane( x + sidesW, y + endsH, bodyW, bodyH, x2, x1, y1, y2, z1, _64x64 ); // front
			ZPlane( x + sidesW + bodyW + sidesW, y + endsH, bodyW, bodyH, x1, x2, y1, y2, z2, _64x64 ); // back
			XPlane( x + sidesW + bodyW, y + endsH, sidesW, sidesH, z1, z2, y1, y2, x1, _64x64 ); // left
			XPlane( x, y + endsH, sidesW, sidesH, z2, z1, y1, y2, x2, _64x64 ); // right
			return new ModelPart( vertices, graphics );
		}
		
		protected static TextureRectangle SkinTexCoords( int x, int y, int width, int height, float skinWidth, float skinHeight ) {
			return new TextureRectangle( x / skinWidth, y / skinHeight, width / skinWidth, height / skinHeight );
		}
		
		protected void XPlane( int texX, int texY, int texWidth, int texHeight,
		                   float z1, float z2, float y1, float y2, float x, bool _64x64 ) {
			TextureRectangle rec = SkinTexCoords( texX, texY, texWidth, texHeight, 64, _64x64 ? 64 : 32 );
			vertices[index++] = new VertexPos3fTex2fCol4b( x, y1, z1, rec.U1, rec.V2, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x, y2, z1, rec.U1, rec.V1, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x, y2, z2, rec.U2, rec.V1, col );
			
			vertices[index++] = new VertexPos3fTex2fCol4b( x, y2, z2, rec.U2, rec.V1, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x, y1, z2, rec.U2, rec.V2, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x, y1, z1, rec.U1, rec.V2, col );
		}
		
		protected void YPlane( int texX, int texY, int texWidth, int texHeight,
		                   float x1, float x2, float z1, float z2, float y, bool _64x64 ) {
			TextureRectangle rec = SkinTexCoords( texX, texY, texWidth, texHeight, 64, _64x64 ? 64 : 32 );
			vertices[index++] = new VertexPos3fTex2fCol4b( x1, y, z1, rec.U1, rec.V1, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x2, y, z1, rec.U2, rec.V1, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x2, y, z2, rec.U2, rec.V2, col );
			
			vertices[index++] = new VertexPos3fTex2fCol4b( x2, y, z2, rec.U2, rec.V2, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x1, y, z2, rec.U1, rec.V2, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x1, y, z1, rec.U1, rec.V1, col );
		}
		
		protected void ZPlane( int texX, int texY, int texWidth, int texHeight,
		                   float x1, float x2, float y1, float y2, float z, bool _64x64 ) {
			TextureRectangle rec = SkinTexCoords( texX, texY, texWidth, texHeight, 64, _64x64 ? 64 : 32 );
			vertices[index++] = new VertexPos3fTex2fCol4b( x1, y1, z, rec.U1, rec.V2, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x2, y1, z, rec.U2, rec.V2, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x2, y2, z, rec.U2, rec.V1, col );
			
			vertices[index++] = new VertexPos3fTex2fCol4b( x2, y2, z, rec.U2, rec.V1, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x1, y2, z, rec.U1, rec.V1, col );
			vertices[index++] = new VertexPos3fTex2fCol4b( x1, y1, z, rec.U1, rec.V2, col );
		}
		
		protected void DrawRotateX( float x, float y, float z, float angleX, ModelPart part ) {
			graphics.PushMatrix();
			graphics.Translate( x, y, z );
			graphics.RotateX( angleX ); // x is ignored anyways.. probably should remove it from arguments.
			graphics.Translate( -x, -y, -z );
			part.Render();
			graphics.PopMatrix();
		}
		
		protected void DrawRotateXZ( float x, float y, float z, float angleX, float angleZ, ModelPart part ) {
			graphics.PushMatrix();
			graphics.Translate( x, y, z );
			graphics.RotateZ( angleZ );
			graphics.RotateX( angleX );
			graphics.Translate( -x, -y, -z );
			part.Render();
			graphics.PopMatrix();
		}
	}
}