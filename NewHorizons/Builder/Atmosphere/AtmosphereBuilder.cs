using NewHorizons.External.Modules;
using NewHorizons.Utility;
using System.Collections.Generic;
using UnityEngine;
namespace NewHorizons.Builder.Atmosphere
{
    public static class AtmosphereBuilder
    {
        private static readonly int InnerRadius = Shader.PropertyToID("_InnerRadius");
        private static readonly int OuterRadius = Shader.PropertyToID("_OuterRadius");
        private static readonly int SkyColor = Shader.PropertyToID("_SkyColor");

        public static readonly List<(GameObject, Material)> Skys = new();

        public static void Init()
        {
            Skys.Clear();
        }

        public static void Make(GameObject planetGO, Sector sector, AtmosphereModule atmosphereModule, float surfaceSize)
        {
            GameObject atmoGO = new GameObject("Atmosphere");
            atmoGO.SetActive(false);
            atmoGO.transform.parent = sector?.transform ?? planetGO.transform;

            if (atmosphereModule.useAtmosphereShader)
            {
                GameObject atmo = GameObject.Instantiate(SearchUtilities.Find("TimberHearth_Body/Atmosphere_TH/AtmoSphere"), atmoGO.transform, true);
                atmo.transform.position = planetGO.transform.TransformPoint(Vector3.zero);
                atmo.transform.localScale = Vector3.one * atmosphereModule.size * 1.2f;

                var renderers = atmo.GetComponentsInChildren<MeshRenderer>();
                var sharedMaterial = new Material(renderers[0].material);
                foreach (var meshRenderer in renderers)
                {
                    meshRenderer.sharedMaterial = sharedMaterial;
                }
                sharedMaterial.SetFloat(InnerRadius, atmosphereModule.clouds != null ? atmosphereModule.size : surfaceSize);
                sharedMaterial.SetFloat(OuterRadius, atmosphereModule.size * 1.2f);
                if (atmosphereModule.atmosphereTint != null) sharedMaterial.SetColor(SkyColor, atmosphereModule.atmosphereTint.ToColor());

                atmo.SetActive(true);

                Skys.Add((planetGO, sharedMaterial));
            }

            atmoGO.transform.position = planetGO.transform.TransformPoint(Vector3.zero);
            atmoGO.SetActive(true);
        }
    }
}
