using System;

namespace LocktonLogic
{
    /// <summary>
    /// Abstract base class for all room controllers in the Lockton system.
    /// Provides common state tracking and panel reporting hooks.
    /// </summary>
    public abstract class BaseRoom
    {
        public string RoomName { get; protected set; }
        public bool IsSystemOn { get; protected set; }

        protected BaseRoom(string roomName)
        {
            RoomName = roomName;
            IsSystemOn = false;
        }

        /// <summary>
        /// Initialize the room logic and push initial state to the panel.
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// Handle digital presses from a touch panel or internal trigger.
        /// </summary>
        public abstract void HandleDigitalPress(uint join);

        /// <summary>
        /// Handle analog changes from a touch panel or feedback loop.
        /// </summary>
        public abstract void HandleAnalogChange(uint join, ushort value);

        /// <summary>
        /// Power down the room and reset state.
        /// </summary>
        public virtual void PowerOff()
        {
            IsSystemOn = false;
        }
    }
}
