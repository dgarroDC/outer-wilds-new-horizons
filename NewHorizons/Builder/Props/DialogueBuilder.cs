﻿using NewHorizons.External.Modules;
using NewHorizons.Handlers;
using OWML.Common;
using System.Xml;
using UnityEngine;
namespace NewHorizons.Builder.Props
{
    public static class DialogueBuilder
    {
        public static void Make(GameObject go, Sector sector, PropModule.DialogueInfo info, IModBehaviour mod)
        {
            // In stock I think they disable dialogue stuff with conditions
            // Here we just don't make it at all
            if (info.blockAfterPersistentCondition != null && PlayerData._currentGameSave.GetPersistentCondition(info.blockAfterPersistentCondition)) return;

            var dialogue = MakeConversationZone(go, sector, info, mod.ModHelper);
            if (info.remoteTriggerPosition != null || info.remoteTriggerRadius != 0) MakeRemoteDialogueTrigger(go, sector, info, dialogue);

            // Make the character look at the player
            // Useful for dialogue replacement
            if (!string.IsNullOrEmpty(info.pathToAnimController)) MakePlayerTrackingZone(go, dialogue, info);
        }

        public static void MakeRemoteDialogueTrigger(GameObject planetGO, Sector sector, PropModule.DialogueInfo info, CharacterDialogueTree dialogue)
        {
            GameObject conversationTrigger = new GameObject("ConversationTrigger");
            conversationTrigger.SetActive(false);

            var remoteDialogueTrigger = conversationTrigger.AddComponent<RemoteDialogueTrigger>();
            var sphereCollider = conversationTrigger.AddComponent<SphereCollider>();
            conversationTrigger.AddComponent<OWCollider>();

            remoteDialogueTrigger._listDialogues = new RemoteDialogueTrigger.RemoteDialogueCondition[]
            {
                new RemoteDialogueTrigger.RemoteDialogueCondition()
                {
                    priority = 1,
                    dialogue = dialogue,
                    prereqConditionType = RemoteDialogueTrigger.MultiConditionType.AND,
                    prereqConditions = new string[]{ },
                    onTriggerEnterConditions = new string[]{ }
                }
            };
            remoteDialogueTrigger._activatedDialogues = new bool[1];
            remoteDialogueTrigger._deactivateTriggerPostConversation = true;

            sphereCollider.radius = info.remoteTriggerRadius == 0 ? info.radius : info.remoteTriggerRadius;

            conversationTrigger.transform.parent = sector?.transform ?? planetGO.transform;
            conversationTrigger.transform.position = planetGO.transform.TransformPoint(info.remoteTriggerPosition ?? info.position);
            conversationTrigger.SetActive(true);
        }

        public static CharacterDialogueTree MakeConversationZone(GameObject planetGO, Sector sector, PropModule.DialogueInfo info, IModHelper mod)
        {
            GameObject conversationZone = new GameObject("ConversationZone");
            conversationZone.SetActive(false);

            conversationZone.layer = LayerMask.NameToLayer("Interactible");

            var sphere = conversationZone.AddComponent<SphereCollider>();
            sphere.radius = info.radius;
            sphere.isTrigger = true;

            var owCollider = conversationZone.AddComponent<OWCollider>();
            var interact = conversationZone.AddComponent<InteractReceiver>();

            if (info.radius <= 0)
            {
                sphere.enabled = false;
                owCollider.enabled = false;
                interact.enabled = false;
            }

            var dialogueTree = conversationZone.AddComponent<CharacterDialogueTree>();

            var xml = System.IO.File.ReadAllText(mod.Manifest.ModFolderPath + info.xmlFile);
            var text = new TextAsset(xml);

            dialogueTree.SetTextXml(text);
            AddTranslation(xml);

            conversationZone.transform.parent = sector?.transform ?? planetGO.transform;
            conversationZone.transform.position = planetGO.transform.TransformPoint(info.position);
            conversationZone.SetActive(true);

            return dialogueTree;
        }

        public static void MakePlayerTrackingZone(GameObject go, CharacterDialogueTree dialogue, PropModule.DialogueInfo info)
        {
            var character = go.transform.Find(info.pathToAnimController);

            // At most one of these should ever not be null
            var nomaiController = character.GetComponent<SolanumAnimController>();
            var controller = character.GetComponent<CharacterAnimController>();

            var lookOnlyWhenTalking = info.lookAtRadius <= 0;

            // To have them look when you start talking
            if (controller != null)
            {
                controller._dialogueTree = dialogue;
                controller.lookOnlyWhenTalking = lookOnlyWhenTalking;
            }
            else if (nomaiController != null)
            {
                if (lookOnlyWhenTalking)
                {
                    dialogue.OnStartConversation += nomaiController.StartWatchingPlayer;
                    dialogue.OnEndConversation += nomaiController.StopWatchingPlayer;
                }
            }
            else
            {
                // TODO: make a custom controller for basic characters to just turn them to face you
            }

            if (info.lookAtRadius > 0)
            {
                GameObject playerTrackingZone = new GameObject("PlayerTrackingZone");
                playerTrackingZone.SetActive(false);

                playerTrackingZone.layer = LayerMask.NameToLayer("BasicEffectVolume");
                playerTrackingZone.SetActive(false);

                var sphereCollider = playerTrackingZone.AddComponent<SphereCollider>();
                sphereCollider.radius = info.lookAtRadius;
                sphereCollider.isTrigger = true;

                playerTrackingZone.AddComponent<OWCollider>();

                var triggerVolume = playerTrackingZone.AddComponent<OWTriggerVolume>();

                if (controller)
                {
                    // Since the Awake method is CharacterAnimController was already called 
                    if (controller.playerTrackingZone)
                    {
                        controller.playerTrackingZone.OnEntry -= controller.OnZoneEntry;
                        controller.playerTrackingZone.OnExit -= controller.OnZoneExit;
                    }
                    // Set it to use the new zone
                    controller.playerTrackingZone = triggerVolume;
                    triggerVolume.OnEntry += controller.OnZoneEntry;
                    triggerVolume.OnExit += controller.OnZoneExit;
                }
                // Simpler for the Nomai
                else if (nomaiController)
                {
                    triggerVolume.OnEntry += (_) => nomaiController.StartWatchingPlayer();
                    triggerVolume.OnExit += (_) => nomaiController.StopWatchingPlayer();
                }
                // No controller
                else
                {
                    // TODO
                }

                playerTrackingZone.transform.parent = dialogue.gameObject.transform;
                playerTrackingZone.transform.localPosition = Vector3.zero;

                playerTrackingZone.SetActive(true);
            }
        }

        private static void AddTranslation(string xml)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            XmlNode xmlNode = xmlDocument.SelectSingleNode("DialogueTree");
            XmlNodeList xmlNodeList = xmlNode.SelectNodes("DialogueNode");
            string characterName = xmlNode.SelectSingleNode("NameField").InnerText;
            TranslationHandler.AddDialogue(characterName);

            foreach (object obj in xmlNodeList)
            {
                XmlNode xmlNode2 = (XmlNode)obj;
                var name = xmlNode2.SelectSingleNode("Name").InnerText;

                XmlNodeList xmlText = xmlNode2.SelectNodes("Dialogue/Page");
                foreach (object Page in xmlText)
                {
                    XmlNode pageData = (XmlNode)Page;
                    var text = pageData.InnerText;
                    TranslationHandler.AddDialogue(text, name);
                }

                xmlText = xmlNode2.SelectNodes("DialogueOptionsList/DialogueOption/Text");
                foreach (object Page in xmlText)
                {
                    XmlNode pageData = (XmlNode)Page;
                    var text = pageData.InnerText;
                    TranslationHandler.AddDialogue(text, characterName, name);
                }
            }
        }
    }
}
