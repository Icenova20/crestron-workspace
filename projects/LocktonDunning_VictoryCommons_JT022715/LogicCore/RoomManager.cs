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
        public HospitalityLogic HospitalityArea { get; private set; }
        public TrainingRoomLogic TrainingRoom { get; private set; }
        public RestroomLogic Restroom14F { get; private set; }
        public RestroomLogic Restroom15F { get; private set; }

        public RoomManager()
        {
            HospitalityArea = new HospitalityLogic();
            TrainingRoom = new TrainingRoomLogic();
            
            Restroom14F = new RestroomLogic("Restroom 14F");
            Restroom14F.MasterMuteJoin = JoinMap.RestroomMute;
            Restroom14F.MasterLevelJoin = JoinMap.RestroomLevel;

            Restroom15F = new RestroomLogic("Restroom 15F");
            Restroom15F.MasterMuteJoin = JoinMap.RestroomMute;
            Restroom15F.MasterLevelJoin = JoinMap.RestroomLevel;
        }

        public void InitializeAll()
        {
            HospitalityArea.Initialize();
            TrainingRoom.Initialize();
            Restroom14F.Initialize();
            Restroom15F.Initialize();
        }
    }
}
