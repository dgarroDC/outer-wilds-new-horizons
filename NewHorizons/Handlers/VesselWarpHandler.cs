using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NewHorizons.Utility;
using Logger = NewHorizons.Utility.Logger;
using static NewHorizons.Main;

namespace NewHorizons.Handlers
{
    public static class VesselWarpHandler
    {
        public static AssetBundle VesselBundle { get; private set; }
        public static GameObject VesselPrefab { get; private set; }

        internal static void Initialize()
        {
            VesselBundle = Main.Instance.ModHelper.Assets.LoadBundle("Assets/vessel.newhorizons");
            VesselPrefab = VesselBundle.LoadAsset<GameObject>("Vessel_Body");
        }

        public static void OnReceiveWarpedBody(OWRigidbody warpedBody, NomaiWarpPlatform startPlatform, NomaiWarpPlatform targetPlatform)
        {
            bool isPlayer = warpedBody.CompareTag("Player");
            if (isPlayer)
            {
                Transform player_body = Locator.GetPlayerTransform();
                OWRigidbody s_rb = Locator.GetShipBody();
                OWRigidbody p_rb = Locator.GetPlayerBody();
                Vector3 newPos = player_body.position;
                Vector3 offset = player_body.up * 10;
                newPos += offset;
                s_rb.SetPosition(newPos);
                s_rb.SetRotation(player_body.transform.rotation);
                s_rb.SetVelocity(p_rb.GetVelocity());
            }
        }

        public static EyeSpawnPoint CreateVessel()
        {
            var system = SystemDict[Instance.CurrentStarSystem];
            Logger.Log("Checking for Vessel Prefab");
            if (VesselPrefab == null) return null;
            Logger.Log("Creating Vessel");
            var vesselObject = GameObject.Instantiate(VesselPrefab);
            vesselObject.name = VesselPrefab.name;
            vesselObject.transform.parent = null;
            if (system.Config.vesselPosition != null)
                vesselObject.transform.position += system.Config.vesselPosition;
            if (system.Config.vesselRotation != null)
                vesselObject.transform.eulerAngles = system.Config.vesselRotation;

            VesselWarpController vesselWarpController = vesselObject.GetComponentInChildren<VesselWarpController>(true);
            vesselWarpController._sourceWarpPlatform.transform.DestroyAllChildren();
            vesselWarpController._targetWarpPlatform.transform.DestroyAllChildren();
            GameObject.Destroy(vesselWarpController._blackHole.transform.parent.gameObject);
            GameObject.Destroy(vesselWarpController._whiteHole.transform.parent.gameObject);
            GameObject WarpPlatform = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/WarpController/Prefab_NOM_WarpPlatform");
            GameObject warpBH = WarpPlatform.transform.Find("BlackHole").gameObject;
            GameObject warpWH = WarpPlatform.transform.Find("WhiteHole").gameObject;
            GameObject sourceBH = GameObject.Instantiate(warpBH, vesselWarpController._sourceWarpPlatform.transform, false);
            vesselWarpController._sourceWarpPlatform._blackHole = sourceBH.GetComponentInChildren<SingularityController>();
            GameObject sourceWH = GameObject.Instantiate(warpWH, vesselWarpController._sourceWarpPlatform.transform, false);
            vesselWarpController._sourceWarpPlatform._whiteHole = sourceWH.GetComponentInChildren<SingularityController>();
            GameObject targetBH = GameObject.Instantiate(warpBH, vesselWarpController._targetWarpPlatform.transform, false);
            vesselWarpController._targetWarpPlatform._blackHole = targetBH.GetComponentInChildren<SingularityController>();
            GameObject targetWH = GameObject.Instantiate(warpWH, vesselWarpController._targetWarpPlatform.transform, false);
            vesselWarpController._targetWarpPlatform._whiteHole = targetWH.GetComponentInChildren<SingularityController>();
            GameObject blackHole = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/BlackHole");
            GameObject newBlackHole = GameObject.Instantiate(blackHole, vesselWarpController.transform.parent, false);
            newBlackHole.name = "BlackHole";
            vesselWarpController._blackHole = newBlackHole.GetComponentInChildren<SingularityController>();
            vesselWarpController._blackHoleOneShot = vesselWarpController._blackHole.transform.parent.Find("BlackHoleAudio_OneShot").GetComponent<OWAudioSource>();
            GameObject whiteHole = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension/Sector_VesselBridge/Interactibles_VesselBridge/WhiteHole");
            GameObject newWhiteHole = GameObject.Instantiate(whiteHole, vesselWarpController.transform.parent, false);
            newWhiteHole.name = "WhiteHole";
            vesselWarpController._whiteHole = newWhiteHole.GetComponentInChildren<SingularityController>();
            vesselWarpController._whiteHoleOneShot = vesselWarpController._whiteHole.transform.parent.Find("WhiteHoleAudio_OneShot").GetComponent<OWAudioSource>();

            vesselObject.SetActive(true);
            vesselWarpController._targetWarpPlatform.OnReceiveWarpedBody += OnReceiveWarpedBody;
            if (system.Config.warpExitPosition != null)
                vesselWarpController._targetWarpPlatform.transform.localPosition = system.Config.warpExitPosition;
            if (system.Config.warpExitRotation != null)
                vesselObject.transform.localEulerAngles = system.Config.warpExitRotation;
            vesselObject.GetComponent<MapMarker>()._labelID = (UITextType)TranslationHandler.AddUI("Vessel");
            EyeSpawnPoint eyeSpawnPoint = vesselObject.GetComponentInChildren<EyeSpawnPoint>(true);
            system.SpawnPoint = eyeSpawnPoint;
            Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => SetupWarpController(vesselWarpController));
            return eyeSpawnPoint;
        }

        public static SpawnPoint UpdateVessel()
        {
            var system = SystemDict[Instance.CurrentStarSystem];
            var vectorSector = SearchUtilities.Find("DB_VesselDimension_Body/Sector_VesselDimension");
            var spawnPoint = vectorSector.GetComponentInChildren<SpawnPoint>();
            system.SpawnPoint = spawnPoint;
            VesselWarpController vesselWarpController = vectorSector.GetComponentInChildren<VesselWarpController>(true);
            if (vesselWarpController._targetWarpPlatform != null)
                vesselWarpController._targetWarpPlatform.OnReceiveWarpedBody += OnReceiveWarpedBody;
            Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => SetupWarpController(vesselWarpController, true));
            return spawnPoint;
        }

        public static void SetupWarpController(VesselWarpController vesselWarpController, bool db = false)
        {
            if (db)
            {
                //Make warp core
                foreach (WarpCoreItem core in Resources.FindObjectsOfTypeAll<WarpCoreItem>())
                {
                    if (core.GetWarpCoreType().Equals(WarpCoreType.Vessel))
                    {
                        var newCore = GameObject.Instantiate(core, AstroObjectLocator.GetAstroObject("Vessel Dimension")?.transform ?? Locator.GetPlayerBody()?.transform);
                        newCore._visible = true;
                        foreach (OWRenderer render in newCore._renderers)
                        {
                            if (render)
                            {
                                render.enabled = true;
                                render.SetActivation(true);
                                render.SetLODActivation(true);
                                if (render.GetRenderer()) render.GetRenderer().enabled = true;
                            }
                        }
                        foreach (ParticleSystem particleSystem in newCore._particleSystems)
                        {
                            if (particleSystem) particleSystem.Play(true);
                        }
                        foreach (OWLight2 light in newCore._lights)
                        {
                            if (light)
                            {
                                light.enabled = true;
                                light.SetActivation(true);
                                light.SetLODActivation(true);
                                if (light.GetLight()) light.GetLight().enabled = true;
                            }
                        }
                        vesselWarpController._coreSocket.PlaceIntoSocket(newCore);
                        break;
                    }
                }
            }
            vesselWarpController.OnSlotDeactivated(vesselWarpController._coordinatePowerSlot);
            if (!db) vesselWarpController.OnSlotActivated(vesselWarpController._coordinatePowerSlot);
            vesselWarpController._gravityVolume.SetFieldMagnitude(vesselWarpController._origGravityMagnitude);
            vesselWarpController._coreCable.SetPowered(true);
            vesselWarpController._coordinateCable.SetPowered(!db);
            vesselWarpController._warpPlatformCable.SetPowered(false);
            vesselWarpController._cageClosed = true;
            if (vesselWarpController._cageAnimator != null)
            {
                vesselWarpController._cageAnimator.TranslateToLocalPosition(new Vector3(0.0f, -8.1f, 0.0f), 0.1f);
                vesselWarpController._cageAnimator.RotateToLocalEulerAngles(new Vector3(0.0f, 180f, 0.0f), 0.1f);
                vesselWarpController._cageAnimator.OnTranslationComplete -= new TransformAnimator.AnimationEvent(vesselWarpController.OnCageAnimationComplete);
                vesselWarpController._cageAnimator.OnTranslationComplete += new TransformAnimator.AnimationEvent(vesselWarpController.OnCageAnimationComplete);
            }
            if (vesselWarpController._cageLoopingAudio != null)
                vesselWarpController._cageLoopingAudio.FadeIn(1f);
        }
    }
}