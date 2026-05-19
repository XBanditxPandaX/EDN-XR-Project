using System.Collections;
using UnityEngine;

namespace PotionClassroom
{
    [RequireComponent(typeof(ParticleSystem))]
    public class CauldronParticles : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Cauldron principal (pour les evenements ingredient/brew).")]
        public Cauldron cauldron;

        [Header("Emission vapeur")]
        [Tooltip("Particules/s au repos quand des ingredients sont presents.")]
        public float idleRate   = 10f;
        [Tooltip("Rayon du disque d'emission (doit correspondre a l'ouverture du chaudron).")]
        public float emitRadius = 0.18f;

        private ParticleSystem _steam;
        private ParticleSystem _sparkles;

        // -----------------------------------------------------------------------
        private void Awake()
        {
            _steam = GetComponent<ParticleSystem>();
            SetupSteam();
            _sparkles = BuildSparkles();
        }

        private void Start()
        {
            if (cauldron != null)
            {
                cauldron.onIngredientAdded.AddListener(OnIngredientAdded);
                cauldron.onPotionBrewed.AddListener(OnPotionBrewed);
                cauldron.onBrewFailed.AddListener(OnBrewFailed);
                cauldron.onCauldronReset.AddListener(OnReset);
            }

            SetSteamRate(0f);
            _steam.Play();
        }

        private void OnDestroy()
        {
            if (cauldron != null)
            {
                cauldron.onIngredientAdded.RemoveListener(OnIngredientAdded);
                cauldron.onPotionBrewed.RemoveListener(OnPotionBrewed);
                cauldron.onBrewFailed.RemoveListener(OnBrewFailed);
                cauldron.onCauldronReset.RemoveListener(OnReset);
            }
        }

        // -----------------------------------------------------------------------
        //  Gestionnaires d'evenements
        // -----------------------------------------------------------------------
        private void OnIngredientAdded(string _)
        {
            SetSteamRate(idleRate);
            Burst(new Color(0.6f, 0.85f, 1f), 18);
        }

        private void OnPotionBrewed(PotionRecipe recipe)
        {
            Burst(recipe.resultColor, 70);
            StartCoroutine(ResetRateAfter(0.3f));
        }

        private void OnBrewFailed()
        {
            Burst(new Color(0.55f, 0f, 0f), 35);
            StartCoroutine(ResetRateAfter(2.3f));
        }

        private void OnReset() => SetSteamRate(0f);

        // -----------------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------------
        private void SetSteamRate(float rate)
        {
            var em = _steam.emission;
            em.rateOverTime = rate;
        }

        private void Burst(Color color, int count)
        {
            var main = _sparkles.main;
            main.startColor = color;
            _sparkles.Emit(count);
        }

        private IEnumerator ResetRateAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetSteamRate(0f);
        }

        // -----------------------------------------------------------------------
        private static Material BuildParticleMaterial()
        {
            // URP d'abord, fallback Built-in
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                     ?? Shader.Find("Particles/Standard Unlit")
                     ?? Shader.Find("Sprites/Default");
            Material mat = new Material(sh);
            mat.SetFloat("_Surface", 1f);          // Transparent
            mat.SetFloat("_Blend", 0f);            // Alpha
            mat.renderQueue = 3000;
            return mat;
        }

        // -----------------------------------------------------------------------
        //  Configuration du systeme de vapeur (continu)
        // -----------------------------------------------------------------------
        private void SetupSteam()
        {
            _steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = _steam.main;
            main.loop            = true;
            main.playOnAwake     = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.012f, 0.045f);
            main.startColor      = new ParticleSystem.MinMaxGradient(
                                       new Color(0.75f, 0.9f, 1f, 0.7f),
                                       new Color(0.9f, 1f, 1f, 0.5f));
            main.maxParticles    = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.05f;

            var em = _steam.emission;
            em.rateOverTime = 0f;

            var shape = _steam.shape;
            shape.enabled         = true;
            shape.shapeType       = ParticleSystemShapeType.Circle;
            shape.radius          = emitRadius;
            shape.radiusThickness = 1f;

            var vel = _steam.velocityOverLifetime;
            vel.enabled = true;
            vel.space   = ParticleSystemSimulationSpace.Local;
            vel.x       = new ParticleSystem.MinMaxCurve(-0.07f, 0.07f);
            vel.z       = new ParticleSystem.MinMaxCurve(-0.07f, 0.07f);

            var sizeLife = _steam.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f,    0.15f, 0f,  4f),
                new Keyframe(0.25f, 1f,   0f,  0f),
                new Keyframe(1f,    0f,  -1f,  0f)));

            var colorLife = _steam.colorOverLifetime;
            colorLife.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f,    0f),
                    new GradientAlphaKey(0.65f, 0.15f),
                    new GradientAlphaKey(0.45f, 0.75f),
                    new GradientAlphaKey(0f,    1f)
                });
            colorLife.color = new ParticleSystem.MinMaxGradient(g);

            var rend = _steam.GetComponent<ParticleSystemRenderer>();
            rend.material = BuildParticleMaterial();
        }

        // -----------------------------------------------------------------------
        //  Construction du systeme de scintillements (burst)
        // -----------------------------------------------------------------------
        private ParticleSystem BuildSparkles()
        {
            GameObject go = new GameObject("SparklesBurst");
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop            = false;
            main.playOnAwake     = false;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(1f, 2.8f);
            main.startSpeed      = new ParticleSystem.MinMaxCurve(0.3f, 1.4f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.018f, 0.07f);
            main.startColor      = Color.white;
            main.maxParticles    = 300;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.18f;

            var em = ps.emission;
            em.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius    = 0.14f;

            var sizeLife = ps.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var colorLife = ps.colorOverLifetime;
            colorLife.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = new ParticleSystem.MinMaxGradient(g);

            ps.GetComponent<ParticleSystemRenderer>().material = BuildParticleMaterial();

            return ps;
        }
    }
}
