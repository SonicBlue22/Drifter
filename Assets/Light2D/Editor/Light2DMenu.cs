﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Light2D
{
    public static class Light2DMenu
    {
        [MenuItem("GameObject/Light2D/Lighting System")]
        public static void CreateLightingSystem()
        {
            LightingSystemCreationWindow.CreateWindow();
        }

        [MenuItem("GameObject/Light2D/Light Source")]
        public static void CreateLightSource() {
            var obj = new GameObject("Light");
            if(LightingSystem.Instance != null)
                obj.layer = LightingSystem.Instance.LightSourcesLayer;
            var light = obj.AddComponent<LightSprite>();
            light.Material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Light2D/Materials/Light60Points.mat");
            light.Sprite = Resources.Load<Sprite>("DefaultLight");
            light.Color = new Color(1, 1, 1, 0.5f);
            Selection.activeObject = obj;
        }

        [MenuItem("GameObject/Light2D/Light Probe")]
        public static void CreateLightProbe(MenuCommand command) {
            GameObject probe = new GameObject("Light2D Probe");
            GameObject parent = command.context as GameObject;
            GameObjectUtility.SetParentAndAlign(probe, parent);

            if(LightingSystem.Instance != null)
                probe.layer = LightingSystem.Instance.LightProbesLayer;
            var bc = probe.AddComponent<BoxCollider2D>();
            var sr = parent.GetComponent<SpriteRenderer>();
            if(sr != null) {
                bc.size = sr.sprite.bounds.size;
                bc.offset = (bc.size / 2) - (sr.sprite.pivot / sr.sprite.pixelsPerUnit);
            }
            var lp = probe.AddComponent<LightProbe>();
            var nr = parent.GetComponent<NormalRenderer>();
            if(nr != null) nr.probe = lp;

            Undo.RegisterCreatedObjectUndo(probe, "Create " + probe.name);
            Selection.activeObject = probe;
        }

        [MenuItem("GameObject/Light2D/Light Obstacle")]
        public static void CreateLightObstacle()
        {
            var baseObjects = Selection.gameObjects.Select(o => o.GetComponent<Renderer>()).Where(r => r != null).ToList();
            if (baseObjects.Count == 0)
            {
                Debug.LogError("Can't create light obstacle from selected object. You need to select any object with renderer attached to it to create light obstacle.");
            }

            foreach (var gameObj in baseObjects)
            {
                var name = gameObj.name + " Light Obstacle";

                var child = gameObj.transform.Find(name);
                var obstacleObj = child == null ? new GameObject(name) : child.gameObject;

                foreach (var obstacleSprite in obstacleObj.GetComponents<LightObstacleSprite>())
                    Util.Destroy(obstacleSprite);

                obstacleObj.transform.parent = gameObj.transform;
                obstacleObj.transform.localPosition = Vector3.zero;
                obstacleObj.transform.localRotation = Quaternion.identity;
                obstacleObj.transform.localScale = Vector3.one;

                obstacleObj.AddComponent<LightObstacleSprite>();
            }
        }

        [MenuItem("GameObject/Light2D/Enable 2DTK Support")]
        public static void Enable2DToolkitSupport()
        {
            var targets = (BuildTargetGroup[]) Enum.GetValues(typeof (BuildTargetGroup));
            foreach (var target in targets)
                DefineSymbol("LIGHT2D_2DTK", target);
        }

        [MenuItem("GameObject/Light2D/Disable 2DTK Support")]
        public static void Disable2DToolkitSupport()
        {
            var targets = (BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup));
            foreach (var target in targets)
                UndefineSymbol("LIGHT2D_2DTK", target);
        }

        public static void DefineSymbol(string symbol, BuildTargetGroup target)
        {
            UndefineSymbol(symbol, target);

            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            if (!defines.EndsWith(";"))
                defines += ";";
            defines += symbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
        }

        public static void UndefineSymbol(string symbol, BuildTargetGroup target)
        {
            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            defines = defines.Replace(symbol + ";", "");
            defines = defines.Replace(";" + symbol, "");
            defines = defines.Replace(symbol, "");
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
        }
    }
}
