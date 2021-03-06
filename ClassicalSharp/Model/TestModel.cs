﻿using System;
using System.Drawing;
using ClassicalSharp.GraphicsAPI;
using OpenTK;

namespace ClassicalSharp.Model {
	
	public class TestModel : IModel {
		
		ModelSet Set, SetSlim;
		public TestModel( Game window ) : base( window ) {
			vertices = new ModelVertex[boxVertices * ( 7 + 2 )];
			Set = new ModelSet();
			
			Set.Head = BuildBox( MakeBoxBounds( -0, 0, -0, 8, 8, 8 )
			                    .SetTexOrigin( 0, 0 ) );
			Set.Torso = BuildBox( MakeBoxBounds( -4, 12, -2, 4, 24, 2 )
			                     .SetTexOrigin( 16, 16 ) );
			Set.LeftLeg = BuildBox( MakeBoxBounds( 0, 0, -2, -4, 12, 2 )
			                       .SetTexOrigin( 0, 16 ) );
			Set.RightLeg = BuildBox( MakeBoxBounds( 0, 0, -2, 4, 12, 2 ).
			                        SetTexOrigin( 0, 16 ) );
			Set.Hat = BuildBox( MakeBoxBounds( -4, 24, -4, 4, 32, 4 )
			                   .SetTexOrigin( 32, 0 )
			                   .SetModelBounds( -4.5f, 23.5f, -4.5f, 4.5f, 32.5f, 4.5f ) );
			Set.LeftArm = BuildBox( MakeBoxBounds( -4, 12, -2, -8, 24, 2 )
			                       .SetTexOrigin( 40, 16 ) );
			Set.RightArm = BuildBox( MakeBoxBounds( 4, 12, -2, 8, 24, 2 )
			                        .SetTexOrigin( 40, 16 ) );
			
			SetSlim = new ModelSet();
			SetSlim.Head = Set.Head;
			SetSlim.Torso = Set.Torso;
			SetSlim.LeftLeg = Set.LeftLeg;
			SetSlim.RightLeg = Set.RightLeg;
			SetSlim.LeftArm = BuildBox( MakeBoxBounds( -7, 12, -2, -4, 24, 2 )
			                           .SetTexOrigin( 32, 48 ) );
			SetSlim.RightArm = BuildBox( MakeBoxBounds( 4, 12, -2, 7, 24, 2 )
			                            .SetTexOrigin( 40, 16 ) );
			SetSlim.Hat = Set.Hat;
		}
		
		public override float NameYOffset {
			get { return 2.1375f; }
		}
		
		public override float GetEyeY( Player player ) {
			return 27/16f;
		}
		
		public override Vector3 CollisionSize {
			get { return new Vector3( 8/16f, 28.5f/16f, 8/16f ); }
		}
		
		public override BoundingBox PickingBounds {
			get { return new BoundingBox( -8/16f, 0, -4/16f, 8/16f, 32/16f, 4/16f ); }
		}
		
		protected override void DrawPlayerModel( Player p ) {
			graphics.Texturing = true;
			int texId = p.MobTextureId <= 0 ? cache.TestTexId : p.MobTextureId;
			graphics.BindTexture( texId );
			
			SkinType skinType = p.SkinType;
			_64x64 = skinType != SkinType.Type64x32;
			ModelSet model = skinType == SkinType.Type64x64Slim ? SetSlim : Set;
			
			DrawRotate( 0, 24/16f, 0, -p.PitchRadians, 0, 0, model.Head );
			DrawPart( model.Torso );
			DrawRotate( 0, 12/16f, 0, p.leftLegXRot, 0, 0, model.LeftLeg );
			DrawRotate( 0, 12/16f, 0, p.rightLegXRot, 0, 0, model.RightLeg );
			DrawRotate( -6/16f, 22/16f, 0, p.leftArmXRot, 0, p.leftArmZRot, model.LeftArm );
			DrawRotate( 6/16f, 22/16f, 0, p.rightArmXRot, 0, p.rightArmZRot, model.RightArm );
			
			graphics.AlphaTest = true;
			if( p.RenderHat )
				DrawRotate( 0, 24f/16f, 0, -p.PitchRadians, 0, 0, model.Hat );
		}
		
		class ModelSet {
			public ModelPart Head, Torso, LeftLeg, RightLeg, LeftArm, RightArm, Hat;
		}
	}
}