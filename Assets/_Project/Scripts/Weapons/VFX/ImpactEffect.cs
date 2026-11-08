using UnityEngine;

namespace Lake.Weapons.VFX
{
    /// <summary>
    /// Спавнит партиклы/декали в точке попадания. Тип поверхности определяется
    /// по тегу коллайдера (Flesh, Wood, Metal, Concrete).
    /// </summary>
    public static class ImpactEffect
    {
        public static void Spawn(Vector3 point, Vector3 normal, GameObject hitObject)
        {
            string tag = hitObject != null ? hitObject.tag : "Untagged";
            Color sparkColor = Color.yellow;
            int particleCount = 8;
            float speed = 5f;

            switch (tag)
            {
                case "Flesh":
                    sparkColor = new Color(0.6f, 0.05f, 0.05f);
                    particleCount = 12;
                    speed = 3f;
                    break;
                case "Metal":
                    sparkColor = new Color(1f, 0.9f, 0.5f);
                    particleCount = 15;
                    speed = 8f;
                    break;
                case "Wood":
                    sparkColor = new Color(0.6f, 0.4f, 0.2f);
                    particleCount = 6;
                    speed = 4f;
                    break;
                default:
                    sparkColor = new Color(0.7f, 0.7f, 0.7f);
                    particleCount = 5;
                    speed = 3f;
                    break;
            }

            SpawnSparks(point, normal, sparkColor, particleCount, speed);
        }

        static void SpawnSparks(Vector3 point, Vector3 normal, Color color, int count, float speed)
        {
            var go = new GameObject("Impact");
            go.transform.position = point + normal * 0.01f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startSpeed = speed;
            main.startSize = 0.03f;
            main.startColor = color;
            main.maxParticles = count;
            main.gravityModifier = 1.5f;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.02f;

            go.transform.rotation = Quaternion.LookRotation(normal);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("HDRP/Unlit"));
            if (mat.HasProperty("_UnlitColor")) mat.SetColor("_UnlitColor", color);
            renderer.material = mat;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            Object.Destroy(go, 1f);
        }
    }
}