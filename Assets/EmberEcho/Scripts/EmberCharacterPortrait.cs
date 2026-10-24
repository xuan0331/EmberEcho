using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace EmberEcho
{
    public sealed class EmberCharacterPortrait : MonoBehaviour
    {
        private const string ResourceRoot = "Characters3D/";
        private const string RetargetedIdleRoot = "CharacterAnimations/HumanoidIdle_";
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static readonly Dictionary<string, AnimationClip> IdleClips = new Dictionary<string, AnimationClip>();
        private static int _nextStageIndex;

        private RenderTexture _renderTexture;
        private GameObject _stageRoot;
        private GameObject _modelInstance;
        private AnimationClip _idleClip;
        private PlayableGraph _idleGraph;
        private AnimationClipPlayable _idlePlayable;
        private Transform _modelRoot;
        private float _turnAmplitude;
        private float _phase;
        private float _animationPhase;
        private float _baseYaw;

        public static RawImage Create(
            Transform parent,
            string resourceKey,
            Vector2 position,
            Vector2 size,
            float turnAmplitude = 7f,
            float framing = 1.35f,
            float baseYaw = 180f)
        {
            var prefab = LoadPrefab(resourceKey);
            if (prefab == null) return null;

            var rect = EmberUI.Center("Character3D_" + resourceKey, parent, position, size);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.color = Color.white;
            image.raycastTarget = false;

            var portrait = rect.gameObject.AddComponent<EmberCharacterPortrait>();
            if (!portrait.Initialize(prefab, resourceKey, Mathf.Clamp(Mathf.RoundToInt(size.x * 1.8f), 256, 640), framing, turnAmplitude, baseYaw))
            {
                Destroy(rect.gameObject);
                return null;
            }

            image.texture = portrait._renderTexture;
            return image;
        }

        private static GameObject LoadPrefab(string resourceKey)
        {
            GameObject prefab;
            if (Prefabs.TryGetValue(resourceKey, out prefab)) return prefab;
            prefab = Resources.Load<GameObject>(ResourceRoot + ModelResourceName(resourceKey));
            Prefabs[resourceKey] = prefab;
            return prefab;
        }

        private bool Initialize(
            GameObject prefab,
            string resourceKey,
            int resolution,
            float framing,
            float turnAmplitude,
            float baseYaw)
        {
            _turnAmplitude = turnAmplitude;
            _baseYaw = baseYaw;
            _phase = Random.Range(0f, Mathf.PI * 2f);
            _animationPhase = Random.value;

            _stageRoot = new GameObject("CharacterStage_" + prefab.name);
            _stageRoot.transform.position = new Vector3(1000f + _nextStageIndex * 12f, 1000f, 1000f);
            _nextStageIndex = (_nextStageIndex + 1) % 32;

            var instance = Instantiate(prefab, _stageRoot.transform);
            instance.name = "Model";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            _modelInstance = instance;

            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }

            foreach (var animation in instance.GetComponentsInChildren<Animation>(true))
            {
                animation.enabled = false;
            }

            _idleClip = LoadIdleClip(resourceKey);
            SetupIdleGraph(instance, resourceKey);
            SampleIdle(0f);

            var bounds = CalculateBounds(instance);
            if (bounds.size.y <= 0.001f)
            {
                Debug.LogWarning("Character model has no renderable bounds: " + prefab.name);
                Destroy(_stageRoot);
                return false;
            }

            instance.transform.position += _stageRoot.transform.position - bounds.center;
            _modelRoot = new GameObject("ModelPivot").transform;
            _modelRoot.SetParent(_stageRoot.transform, false);
            instance.transform.SetParent(_modelRoot, true);
            _modelRoot.localScale = Vector3.one * (2f / bounds.size.y);

            var cameraObject = new GameObject("PortraitCamera", typeof(Camera));
            cameraObject.transform.SetParent(_stageRoot.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -6f);
            cameraObject.transform.LookAt(_stageRoot.transform.position, Vector3.up);

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = framing;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.cullingMask = ~0;

            _renderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
            {
                name = "CharacterPortrait_" + prefab.name,
                antiAliasing = 2,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false
            };
            _renderTexture.Create();
            camera.targetTexture = _renderTexture;

            CreateLight("KeyLight", new Vector3(32f, -28f, 0f), new Color(1f, 0.78f, 0.58f), 1.45f);
            CreateLight("RimLight", new Vector3(22f, 158f, 0f), new Color(0.38f, 0.58f, 1f), 0.9f);
            CreateLight("FillLight", new Vector3(-18f, 72f, 0f), new Color(0.58f, 0.42f, 0.35f), 0.45f);
            return true;
        }

        private static AnimationClip LoadIdleClip(string resourceKey)
        {
            AnimationClip cached;
            if (IdleClips.TryGetValue(resourceKey, out cached)) return cached;

            AnimationClip clip = null;
            if (IsRetargetedHumanoid(resourceKey))
            {
                clip = Resources.Load<AnimationClip>(RetargetedIdleRoot + resourceKey);
            }
            else
            {
                var desiredName = IdleName(resourceKey);
                if (!string.IsNullOrEmpty(desiredName))
                {
                    var clips = Resources.LoadAll<AnimationClip>(ResourceRoot + ModelResourceName(resourceKey));
                    foreach (var candidate in clips)
                    {
                        if (candidate.name == desiredName)
                        {
                            clip = candidate;
                            break;
                        }
                    }

                    if (clip == null)
                    {
                        foreach (var candidate in clips)
                        {
                            if (candidate.name.EndsWith("|" + desiredName))
                            {
                                clip = candidate;
                                break;
                            }
                        }
                    }
                }
            }

            IdleClips[resourceKey] = clip;
            return clip;
        }

        private void SetupIdleGraph(GameObject instance, string resourceKey)
        {
            if (_idleClip == null) return;

            var animator = instance.GetComponent<Animator>();
            if (animator == null)
            {
                animator = instance.GetComponentInChildren<Animator>(true);
            }

            if (animator == null)
            {
                animator = instance.AddComponent<Animator>();
            }

            animator.enabled = true;
            animator.applyRootMotion = false;
            animator.Rebind();

            _idleGraph = PlayableGraph.Create("CharacterIdle_" + resourceKey);
            _idleGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

            var output = AnimationPlayableOutput.Create(_idleGraph, "Idle", animator);
            _idlePlayable = AnimationClipPlayable.Create(_idleGraph, _idleClip);
            _idlePlayable.SetApplyFootIK(false);
            _idlePlayable.SetApplyPlayableIK(false);
            output.SetSourcePlayable(_idlePlayable);
            _idleGraph.Play();
        }

        private static string ModelResourceName(string resourceKey)
        {
            switch (resourceKey)
            {
                case "enemy_demon": return "enemy_demon_dragon";
                case "enemy_wraith": return "enemy_wraith_skeleton";
                default: return resourceKey;
            }
        }

        private static bool IsRetargetedHumanoid(string resourceKey)
        {
            return resourceKey == "hero_ember" ||
                   resourceKey == "npc_guard" ||
                   resourceKey == "enemy_cultist";
        }

        private static string IdleName(string resourceKey)
        {
            switch (resourceKey)
            {
                case "enemy_slime": return "MonsterArmature|Idle";
                case "enemy_spider": return "SpiderArmature|Spider_Idle";
                case "enemy_demon": return "DragonArmature|Dragon_Flying";
                case "enemy_wraith": return "SkeletonArmature|Skeleton_Idle";
                case "enemy_brute": return "Armature|TRex_Idle";
                case "enemy_knight": return "HumanArmature|Idle";
                default: return null;
            }
        }

        private void CreateLight(string name, Vector3 euler, Color color, float intensity)
        {
            var lightObject = new GameObject(name, typeof(Light));
            lightObject.transform.SetParent(_stageRoot.transform, false);
            lightObject.transform.localRotation = Quaternion.Euler(euler);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = color;
            light.intensity = intensity;
            light.shadows = LightShadows.None;
        }

        private static Bounds CalculateBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(instance.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private void Update()
        {
            if (_modelRoot == null) return;
            var turn = _baseYaw + Mathf.Sin(Time.unscaledTime * 0.55f + _phase) * _turnAmplitude;
            _modelRoot.localRotation = Quaternion.Euler(0f, turn, 0f);
            SampleIdle(Time.unscaledTime + _animationPhase);
        }

        private void SampleIdle(float time)
        {
            if (_modelInstance == null || _idleClip == null || _idleClip.length <= 0f) return;
            if (!_idleGraph.IsValid()) return;

            _idlePlayable.SetTime(Mathf.Repeat(time, _idleClip.length));
            _idleGraph.Evaluate(0f);
        }

        private void OnDestroy()
        {
            if (_idleGraph.IsValid())
            {
                _idleGraph.Destroy();
            }

            if (_stageRoot != null)
            {
                Destroy(_stageRoot);
                _stageRoot = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }

            _idleClip = null;
            _modelInstance = null;
        }
    }
}
