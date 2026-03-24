using NUnit.Framework;
using Player;
using UnityEngine;

namespace Tests.EditMode
{
    public class PlayerStateMachineTests
    {
        private PlayerStateMachine stateMachine;
        private GameObject testObject;

        [SetUp]
        public void SetUp()
        {
            testObject = new GameObject("TestPlayer");
            stateMachine = testObject.AddComponent<PlayerStateMachine>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(testObject);
        }

        [Test]
        public void StartsInIdleState()
        {
            Assert.AreEqual(PlayerState.Idle, stateMachine.CurrentState);
        }

        [Test]
        public void PreviousStateStartsAsIdle()
        {
            Assert.AreEqual(PlayerState.Idle, stateMachine.PreviousState);
        }

        [Test]
        public void ChangeState_UpdatesCurrentState()
        {
            stateMachine.ChangeState(PlayerState.Running);
            Assert.AreEqual(PlayerState.Running, stateMachine.CurrentState);
        }

        [Test]
        public void ChangeState_UpdatesPreviousState()
        {
            stateMachine.ChangeState(PlayerState.Running);
            Assert.AreEqual(PlayerState.Idle, stateMachine.PreviousState);
        }

        [Test]
        public void ChangeState_SameStateDoesNothing()
        {
            stateMachine.ChangeState(PlayerState.Running);
            stateMachine.ChangeState(PlayerState.Running);
            Assert.AreEqual(PlayerState.Idle, stateMachine.PreviousState);
        }

        [Test]
        public void ChangeState_TracksMultipleTransitions()
        {
            stateMachine.ChangeState(PlayerState.Running);
            stateMachine.ChangeState(PlayerState.Jumping);
            stateMachine.ChangeState(PlayerState.Falling);

            Assert.AreEqual(PlayerState.Falling, stateMachine.CurrentState);
            Assert.AreEqual(PlayerState.Jumping, stateMachine.PreviousState);
        }

        [Test]
        public void ChangeState_FullCycle()
        {
            stateMachine.ChangeState(PlayerState.Running);
            stateMachine.ChangeState(PlayerState.Jumping);
            stateMachine.ChangeState(PlayerState.Falling);
            stateMachine.ChangeState(PlayerState.Landing);
            stateMachine.ChangeState(PlayerState.Idle);

            Assert.AreEqual(PlayerState.Idle, stateMachine.CurrentState);
            Assert.AreEqual(PlayerState.Landing, stateMachine.PreviousState);
        }

        [Test]
        public void Dashing_StateTransitionWorks()
        {
            stateMachine.ChangeState(PlayerState.Running);
            stateMachine.ChangeState(PlayerState.Dashing);

            Assert.AreEqual(PlayerState.Dashing, stateMachine.CurrentState);
            Assert.AreEqual(PlayerState.Running, stateMachine.PreviousState);
        }

        [Test]
        public void Dashing_IsNotGrounded()
        {
            stateMachine.ChangeState(PlayerState.Dashing);

            Assert.IsFalse(stateMachine.IsGrounded);
        }

        [Test]
        public void Dashing_PreviousStateTracksCorrectly()
        {
            stateMachine.ChangeState(PlayerState.Running);
            stateMachine.ChangeState(PlayerState.Jumping);
            stateMachine.ChangeState(PlayerState.Dashing);

            Assert.AreEqual(PlayerState.Dashing, stateMachine.CurrentState);
            Assert.AreEqual(PlayerState.Jumping, stateMachine.PreviousState);
        }

        [Test]
        public void Dashing_FullCycleWithDash()
        {
            stateMachine.ChangeState(PlayerState.Running);
            stateMachine.ChangeState(PlayerState.Dashing);
            stateMachine.ChangeState(PlayerState.Falling);
            stateMachine.ChangeState(PlayerState.Landing);
            stateMachine.ChangeState(PlayerState.Idle);

            Assert.AreEqual(PlayerState.Idle, stateMachine.CurrentState);
            Assert.AreEqual(PlayerState.Landing, stateMachine.PreviousState);
        }
    }
}
