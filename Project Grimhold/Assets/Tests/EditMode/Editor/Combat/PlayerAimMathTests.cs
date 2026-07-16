using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Combat
{
    public class PlayerAimMathTests
    {
        private Vector2 CalculateRangedAttackDirection(Vector2 origin, Vector2 aimPosition, Vector2 facingDirection)
        {
            Vector2 direction = aimPosition - origin;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = facingDirection;
            }
            return direction.normalized;
        }

        [Test]
        public void CalculateDirection_TowardsAimPoint()
        {
            Vector2 origin = new Vector2(1f, 1f);
            Vector2 aim = new Vector2(4f, 5f);
            Vector2 facing = Vector2.right;

            Vector2 direction = CalculateRangedAttackDirection(origin, aim, facing);

            Vector2 expected = (new Vector2(4f, 5f) - new Vector2(1f, 1f)).normalized;
            Assert.AreEqual(expected.x, direction.x, 0.0001f);
            Assert.AreEqual(expected.y, direction.y, 0.0001f);
        }

        [Test]
        public void CalculateDirection_IsNormalized()
        {
            Vector2 origin = new Vector2(0f, 0f);
            Vector2 aim = new Vector2(100f, 200f);
            Vector2 facing = Vector2.up;

            Vector2 direction = CalculateRangedAttackDirection(origin, aim, facing);

            Assert.AreEqual(1f, direction.magnitude, 0.0001f);
        }

        [Test]
        public void CalculateDirection_PlayerStill_CanShootTowardsCursor()
        {
            Vector2 origin = new Vector2(0f, 0f);
            Vector2 aim = new Vector2(-5f, 2f);
            Vector2 facing = Vector2.right; // Facing right but aiming top-left

            Vector2 direction = CalculateRangedAttackDirection(origin, aim, facing);

            Vector2 expected = new Vector2(-5f, 2f).normalized;
            Assert.AreEqual(expected.x, direction.x, 0.0001f);
            Assert.AreEqual(expected.y, direction.y, 0.0001f);
        }

        [Test]
        public void CalculateDirection_MovementDirectionDoesNotReplaceAim()
        {
            Vector2 origin = new Vector2(0f, 0f);
            Vector2 aim = new Vector2(1f, 0f); // Aiming right
            Vector2 facing = Vector2.left; // Moving/Facing left

            Vector2 direction = CalculateRangedAttackDirection(origin, aim, facing);

            Assert.AreEqual(Vector2.right.x, direction.x, 0.0001f);
            Assert.AreEqual(Vector2.right.y, direction.y, 0.0001f);
        }

        [Test]
        public void CalculateDirection_FallbackToFacingDirection_WhenAimIsInvalid()
        {
            Vector2 origin = new Vector2(3f, 3f);
            Vector2 aim = new Vector2(3f, 3.00001f); // Extremely close to player position (almost zero direction)
            Vector2 facing = Vector2.down; // Fallback direction

            Vector2 direction = CalculateRangedAttackDirection(origin, aim, facing);

            Assert.AreEqual(Vector2.down.x, direction.x, 0.0001f);
            Assert.AreEqual(Vector2.down.y, direction.y, 0.0001f);
        }
    }
}
