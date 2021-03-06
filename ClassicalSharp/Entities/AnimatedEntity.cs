﻿using System;
using OpenTK;

namespace ClassicalSharp {

	/// <summary> Entity that is animated depending on movement speed and time. </summary>
	public abstract class AnimatedEntity : PhysicsEntity {
		
		public AnimatedEntity( Game game ) : base( game ) {
		}
		public float leftLegXRot, leftArmXRot, leftArmZRot;
		public float rightLegXRot, rightArmXRot, rightArmZRot;
		protected float walkTimeO, walkTimeN, swingO, swingN;
		
		/// <summary> Calculates the next animation state based on old and new position. </summary>
		protected void UpdateAnimState( Vector3 oldPos, Vector3 newPos, double delta ) {
			walkTimeO = walkTimeN;
			swingO = swingN;
			float dx = newPos.X - oldPos.X;
			float dz = newPos.Z - oldPos.Z;
			double distance = Math.Sqrt( dx * dx + dz * dz );
			
			if( distance > 0.05 ) {
				walkTimeN += (float)distance * 2 * (float)(20 * delta);
				swingN += (float)delta * 3;
			} else {
				swingN -= (float)delta * 3;
			}
			Utils.Clamp( ref swingN, 0, 1 );
		}
		
		const float armMax = 60 * Utils.Deg2Rad;
		const float legMax = 80 * Utils.Deg2Rad;
		const float idleMax = 3 * Utils.Deg2Rad;
		const float idleXPeriod = (float)(2 * Math.PI / 5.0f);
		const float idleZPeriod = (float)(2 * Math.PI / 3.5f);
		
		/// <summary> Calculates the interpolated state between the last and next animation state. </summary>
		protected void GetCurrentAnimState( float t ) {
			float swing = Utils.Lerp( swingO, swingN, t );
			float walkTime = Utils.Lerp( walkTimeO, walkTimeN, t );
			float idleTime = (float)game.accumulator;
			float idleXRot = (float)(Math.Sin( idleTime * idleXPeriod ) * idleMax);
			float idleZRot = (float)(idleMax + Math.Cos(idleTime * idleZPeriod) * idleMax);
			
			leftArmXRot = (float)(Math.Cos( walkTime ) * swing * armMax) - idleXRot;
			rightArmXRot = -leftArmXRot;
			rightLegXRot = (float)(Math.Cos( walkTime ) * swing * legMax);
			leftLegXRot = -rightLegXRot;
			rightArmZRot = idleZRot;
			leftArmZRot = -idleZRot;
		}
	}
}