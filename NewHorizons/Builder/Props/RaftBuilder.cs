﻿using NewHorizons.Builder.General;
using NewHorizons.Components;
using NewHorizons.External;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using OWML.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;

namespace NewHorizons.Builder.Props
{
    public static class RaftBuilder
    {
        private static GameObject _prefab;

        public static void Make(GameObject planetGO, Sector sector, PropModule.RaftInfo info, OWRigidbody planetBody)
        {
            if(_prefab == null)
            {
                _prefab = GameObject.FindObjectOfType<RaftController>().gameObject.InstantiateInactive();
                _prefab.name = "Raft_Body_Prefab";
            }

            GameObject raftObject = _prefab.InstantiateInactive();
            raftObject.name = "Raft_Body";
            raftObject.transform.parent = sector.transform;
            raftObject.transform.localPosition = info.position;
            raftObject.transform.localRotation = Quaternion.identity;

            sector.OnOccupantEnterSector += (sd) => OWAssetHandler.OnOccupantEnterSector(raftObject, sd, sector);
            OWAssetHandler.LoadObject(raftObject);

            var raftController = raftObject.GetComponent<RaftController>();
            // Since awake already ran we have to unhook these events
            raftController._sector.OnOccupantEnterSector -= raftController.OnOccupantEnterSector;
            raftController._sector.OnOccupantExitSector -= raftController.OnOccupantExitSector;
            raftController._riverFluid = null;

            raftController._sector = sector;
            sector.OnOccupantEnterSector += raftController.OnOccupantEnterSector;
            sector.OnOccupantExitSector += raftController.OnOccupantExitSector;

            // Detectors
            var fluidDetector = raftObject.transform.Find("Detector_Raft").GetComponent<RaftFluidDetector>();
            var waterVolume = planetGO.GetComponentInChildren<NHFluidVolume>();
            fluidDetector._alignmentFluid = waterVolume;

            // Light sensors
            foreach (var lightSensor in raftObject.GetComponentsInChildren<SingleLightSensor>())
            {
                lightSensor._sector.OnSectorOccupantsUpdated -= lightSensor.OnSectorOccupantsUpdated;
                lightSensor._sector = sector;
                sector.OnSectorOccupantsUpdated += lightSensor.OnSectorOccupantsUpdated;
            }

            /*
            // Debug
            foreach (var point in fluidDetector._localAlignmentCheckPoints)
            {
                var sphere = AddDebugShape.AddSphere(fluidDetector.gameObject, 0.5f, Color.green);
                sphere.transform.localPosition = point;
            }
            */

            raftObject.SetActive(true);
        }
    }
}
