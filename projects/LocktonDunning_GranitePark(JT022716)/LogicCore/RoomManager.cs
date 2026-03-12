using System;
using System.Collections.Generic;

namespace LocktonLogic
{
    /// <summary>
    /// Central manager for all room controllers.
    /// Acts as the orchestration layer between hardware/UI and logic.
    /// </summary>
    public class RoomManager
    {
        public BreakRoomLogic BreakRoom { get; private set; }
        public TrainingRoomLogic TrainingRoom { get; private set; }
        public RestroomLogic Restroom9F { get; private set; }
        public RestroomLogic Restroom10F { get; private set; }

        public RoomManager()
        {
            BreakRoom = new BreakRoomLogic();
            TrainingRoom = new TrainingRoomLogic();
            
            Restroom9F = new RestroomLogic("Restroom 9F");
            Restroom9F.MasterMuteJoin = JoinMap.RestroomMute;
            Restroom9F.MasterLevelJoin = JoinMap.RestroomLevel;

            Restroom10F = new RestroomLogic("Restroom 10F");
            Restroom10F.MasterMuteJoin = JoinMap.RestroomMute; // Using same UI join for both or unique?
            Restroom10F.MasterLevelJoin = JoinMap.RestroomLevel;
        }

        public void InitializeAll()
        {
            BreakRoom.Initialize();
            TrainingRoom.Initialize();
            Restroom9F.Initialize();
            Restroom10F.Initialize();
        }
    }
}
