﻿using System;
using ClassicalSharp.Model;
using OpenTK;

namespace ClassicalSharp {
	
	/// <summary> Contains a model, along with position, velocity, and rotation.
	/// May also contain other fields and properties. </summary>
	public abstract class Entity {
		
		public Entity( Game game ) {
			this.game = game;
			info = game.BlockInfo;
		}
		
		public IModel Model;
		public string ModelName;
		public byte ID;
		
		public Vector3 Position;
		public Vector3 Velocity;
		public float YawDegrees, PitchDegrees;
		
		protected Game game;
		protected BlockInfo info;
		
		/// <summary> Rotation of the entity horizontally (i.e. looking north or east) </summary>
		public float YawRadians {
			get { return YawDegrees * Utils.Deg2Rad; }
			set { YawDegrees = value * Utils.Rad2Deg; }
		}
		
		/// <summary> Rotation of the entity vertically. (i.e. looking up or down) </summary>
		public float PitchRadians {
			get { return PitchDegrees * Utils.Deg2Rad; }
			set { PitchDegrees = value * Utils.Rad2Deg; }
		}
		
		/// <summary> Returns the size of the model that is used for collision detection. </summary>
		public virtual Vector3 CollisionSize {
			get { return new Vector3( 8/16f, 28.5f/16f, 8/16f );
				//Model.CollisionSize; TODO: for non humanoid models
			}
		}
		
		/// <summary> Bounding box of the model that collision detection
		/// is performed with, in world coordinates.  </summary>
		public virtual BoundingBox CollisionBounds {
			get {
				Vector3 pos = Position;
				Vector3 size = CollisionSize;
				return new BoundingBox( pos.X - size.X / 2, pos.Y, pos.Z - size.Z / 2,
				                       pos.X + size.X / 2, pos.Y + size.Y, pos.Z + size.Z / 2 );
			}
		}
		
		public abstract void Despawn();
		
		/// <summary> Renders the entity's model, interpolating between the previous and next state. </summary>
		public abstract void RenderModel( double deltaTime, float t );
		
		/// <summary> Renders the entity's name over the top of its model. </summary>
		/// <remarks> Assumes that RenderModel was previously called this frame. </remarks>
		public abstract void RenderName();
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity satisfy the given condition. </summary>
		public bool TouchesAny( Predicate<byte> condition ) {
			return TouchesAny( CollisionBounds, condition );
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// given bounding box satisfy the given condition. </summary>
		public bool TouchesAny( BoundingBox bounds, Predicate<byte> condition ) {
			Vector3I bbMin = Vector3I.Floor( bounds.Min );
			Vector3I bbMax = Vector3I.Floor( bounds.Max );
			
			// Order loops so that we minimise cache misses		
			for( int y = bbMin.Y; y <= bbMax.Y; y++ ) {
				for( int z = bbMin.Z; z <= bbMax.Z; z++ ) {
					for( int x = bbMin.X; x <= bbMax.X; x++ ) {
						if( !game.Map.IsValidPos( x, y, z ) ) continue;
						byte block = game.Map.GetBlock( x, y, z );
						
						if( condition( block ) ) {
							float blockHeight = info.Height[block];
							Vector3 min = new Vector3( x, y, z ) + info.MinBB[block];
							Vector3 max = new Vector3( x, y, z ) + info.MaxBB[block];
							BoundingBox blockBB = new BoundingBox( min, max );
							if( blockBB.Intersects( bounds ) ) return true;
						}
					}
				}
			}
			return false;
		}
		
		/// <summary> Constant offset used to avoid floating point roundoff errors. </summary>
		public const float Adjustment = 0.001f;
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity are lava or still lava. </summary>
		protected bool TouchesAnyLava() {
			BoundingBox bounds = CollisionBounds;
			// Even though we collide with lava 3 blocks above our feet, vanilla client thinks
			// that we do not.. so we have to maintain compatibility here.
			bounds.Max.Y -= 4/16f;
			return TouchesAny( bounds,
			                  b => b == (byte)Block.Lava || b == (byte)Block.StillLava );
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity are rope. </summary>
		protected bool TouchesAnyRope() {
			return TouchesAny( CollisionBounds,
			                  b => b == (byte)Block.Rope );
		}
		
		/// <summary> Determines whether any of the blocks that intersect the
		/// bounding box of this entity are water or still water. </summary>
		protected bool TouchesAnyWater() {
			BoundingBox bounds = CollisionBounds;
			bounds.Max.Y -= 4/16f;
			return TouchesAny( bounds,
			                  b => b == (byte)Block.Water || b == (byte)Block.StillWater );
		}
	}
}