using UnityEngine;

namespace Lake.Weapons.VFX
{
    /// <summary>
    /// Фабрика вспышек. Создаёт префаб на лету — не требует ручного создания ассета.
    /// Один объект, самоуничтожается через 0.1 сек.
    /// </summary>
    public class MuzzleFlashFactory : MonoBehaviour
    {
        public static GameObject CreateMuzzleFlash(Vector3 position, Quaternion rotation, Transform parent)
        {
            var root = new GameObject("MuzzleFlash");
            root.transform.SetPositionAndRotation(position, rotation);
            root.transform.SetParent(parent, true);

            // 1) Particle: короткий конус искр
            var psGo = new GameObject("Particles");
            psGo.transform.SetParent(root.transform, false);
            var ps = psGo.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.05f;
            main.startLifetime = 0.08f;
            main.startSpeed = 15f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 0.85f, 0.4f, 1f);
            main.maxParticles = 12;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.02f;

            var renderer = psGo.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("HDRP/Unlit"));
            if (renderer.material.HasProperty("_UnlitColor"))
                renderer.material.SetColor("_UnlitColor", new Color(1f, 0.85f, 0.4f));
            renderer.material.renderQueue = 3000;

            // 2) Mesh: звезда/квад — яркая вспышка
            var flash = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flash.name = "Flash";
            flash.transform.SetParent(root.transform, false);
            flash.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
            flash.transform.localScale = Vector3.one * Random.Range(0.15f, 0.25f);
            Object.Destroy(flash.GetComponent<Collider>());
            var flashMat = new Material(Shader.Find("HDRP/Unlit"));
            if (flashMat.HasProperty("_UnlitColor"))
                flashMat.SetColor("_UnlitColor", new Color(1f, 0.9f, 0.6f, 1f));
            if (flashMat.HasProperty("_UnlitColorMap"))
                flashMat.SetColor("_UnlitColorMap", new Color(1f, 0.9f, 0.6f, 1f));
            flashMat.renderQueue = 3000;
            flash.GetComponent<Renderer>().material = flashMat;
            flash.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 3) Point light — освещает комнату (Bodycam-фишка)
            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.85f, 0.5f);
            light.intensity = 8f;
            light.range = 5f;
            light.shadows = LightShadows.None;
            lightGo.AddComponent<MuzzleFlashLight>().Setup(8f, 5f, 0.06f);

            Object.Destroy(root, 0.15f);
            return root;
        }
    }
}