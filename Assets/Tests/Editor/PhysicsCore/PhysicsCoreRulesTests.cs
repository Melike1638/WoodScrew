using System.Collections.Generic;
using NUnit.Framework;
using WoodScrew.PhysicsCore;

namespace WoodScrew.Tests.PhysicsCore
{
    public class PhysicsCoreRulesTests
    {
        [Test]
        public void Rule1_SwingingPlankCoveringAnchor_TargetIsInvalid()
        {
            CoreOrientedRect start =
                new CoreOrientedRect(
                    new CoreVector2(0f, 0f),
                    new CoreVector2(2f, 0.40f),
                    0f
                );

            CoreVector2 pivot =
                new CoreVector2(-1f, 0f);

            CoreVector2 anchor =
                new CoreVector2(
                    -0.30f,
                    0.70f
                );

            Assert.IsFalse(
                WoodScrewPhysicsCore
                    .IsAnchorBlockedAtPose(
                        anchor,
                        0.08f,
                        start
                    )
            );

            Assert.IsTrue(
                WoodScrewPhysicsCore
                    .IsAnchorBlockedDuringSwing(
                        anchor,
                        0.08f,
                        start,
                        pivot,
                        90f
                    )
            );

            // Corner regression:
            // A square-expanded rectangle would incorrectly block
            // this point, but a real circular anchor does not touch.
            CoreVector2 cornerAnchor =
                new CoreVector2(
                    1.07f,
                    0.27f
                );

            Assert.IsFalse(
                WoodScrewPhysicsCore
                    .IsAnchorBlockedAtPose(
                        cornerAnchor,
                        0.08f,
                        start
                    )
            );
        }


        [Test]
        public void Rule2_TwoOrMoreSupports_PlankIsFixed()
        {
            Assert.AreEqual(
                PlankMotionState.Fixed,
                WoodScrewPhysicsCore
                    .GetMotionState(2)
            );

            Assert.AreEqual(
                PlankMotionState.Fixed,
                WoodScrewPhysicsCore
                    .GetMotionState(3)
            );
        }


        [Test]
        public void Rule3_OneSupport_PlankPivotsAroundThatScrew()
        {
            List<int> supports =
                new List<int>
                {
                    42
                };

            Assert.AreEqual(
                PlankMotionState.Pivoting,
                WoodScrewPhysicsCore
                    .GetMotionState(
                        supports.Count
                    )
            );

            int pivotId;

            Assert.IsTrue(
                WoodScrewPhysicsCore
                    .TryGetPivotScrewId(
                        supports,
                        out pivotId
                    )
            );

            Assert.AreEqual(
                42,
                pivotId
            );
        }


        [Test]
        public void Rule4_ZeroSupports_PlankFalls()
        {
            Assert.AreEqual(
                PlankMotionState.Falling,
                WoodScrewPhysicsCore
                    .GetMotionState(0)
            );
        }


        [Test]
        public void Rule5_SweepCollision_CatchesMidArcObstacle_NoTunneling()
        {
            CoreOrientedRect start =
                new CoreOrientedRect(
                    new CoreVector2(0f, 0f),
                    new CoreVector2(2f, 0.40f),
                    0f
                );

            CoreVector2 pivot =
                new CoreVector2(-1f, 0f);

            CoreCircle obstacle =
                new CoreCircle(
                    new CoreVector2(
                        -0.30f,
                        0.70f
                    ),
                    0.10f
                );

            Assert.IsTrue(
                WoodScrewPhysicsCore
                    .SweepHitsCircle(
                        start,
                        pivot,
                        90f,
                        obstacle,
                        0.01f,
                        64
                    )
            );
        }
    }
}
