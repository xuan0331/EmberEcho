using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EmberEcho
{
    public sealed partial class EmberGame : MonoBehaviour
    {
        private Canvas _canvas;
        private RectTransform _backdrop;
        private RectTransform _screenRoot;
        private AudioBank _audio;
        private RunState _run;
        private System.Random _rng;
        private GameObject _modalRoot;
        private bool _busy;

        private Text _headerHpText;
        private Text _headerGoldText;
        private Text _headerRelicText;
        private string _lastAcquiredRelicId;

        private static readonly Key[] NumberKeys =
        {
            Key.Digit1,
            Key.Digit2,
            Key.Digit3,
            Key.Digit4,
            Key.Digit5,
            Key.Digit6,
            Key.Digit7,
            Key.Digit8,
            Key.Digit9
        };

        private readonly List<CardDef> _drawPile = new List<CardDef>();
        private readonly List<CardDef> _discardPile = new List<CardDef>();
        private readonly List<CardDef> _hand = new List<CardDef>();
        private readonly List<CardDef> _exhaustPile = new List<CardDef>();
        private readonly List<EnemyCombatant> _enemies = new List<EnemyCombatant>();
        private readonly List<EnemyView> _enemyViews = new List<EnemyView>();
        private readonly List<string> _combatLog = new List<string>();

        private NodeKind _currentNodeKind;
        private EncounterDef _encounter;
        private int _selectedEnemyIndex;
        private int _energy;
        private int _playerBlock;
        private int _playerStrength;
        private int _playerDexterity;
        private int _playerWeak;
        private int _playerVulnerable;
        private int _ember;
        private int _turn;
        private bool _enemyTurn;
        private bool _combatOver;
        private bool _inCombat;

        private RectTransform _handRoot;
        private Text _energyText;
        private Text _emberText;
        private Text _turnText;
        private Text _drawCountText;
        private Text _discardCountText;
        private Text _playerHpText;
        private Text _playerBlockText;
        private Text _playerStatusText;
        private Text _logText;
        private Text _targetText;
        private Image _playerHpFill;
        private Button _endTurnButton;
        private CanvasGroup _damageFlash;
        private Coroutine _turnRoutine;

        private class EnemyView
        {
            public EnemyCombatant Data;
            public RectTransform Root;
            public RectTransform ArtRect;
            public Graphic Art;
            public Image HpFill;
            public Image TargetRing;
            public Text IntentText;
            public Text HpText;
            public Text BlockText;
            public Text StatusText;
            public Text NameText;
        }

        private class ShopEntry
        {
            public CardDef Card;
            public int Price;
            public bool Sold;
            public Button BuyButton;
            public Text BuyText;
            public GameObject CardView;
        }

        private class EventChoice
        {
            public string Label;
            public string Detail;
            public Action Apply;
        }

        private class RelicDef
        {
            public string Id;
            public string Name;
            public string Description;
        }

        private static readonly List<RelicDef> Relics = new List<RelicDef>
        {
            new RelicDef { Id = "ember_lamp", Name = "余烬灯", Description = "每场战斗开始时获得 1 点余烬。" },
            new RelicDef { Id = "iron_butterfly", Name = "铁蝶", Description = "每场战斗首次生命低于一半时，获得 10 点护甲。" },
            new RelicDef { Id = "blood_fang", Name = "旧狼牙", Description = "每场战斗结束后回复 3 点生命。" },
            new RelicDef { Id = "wayfarer_compass", Name = "远行罗盘", Description = "获得时最大生命提高 10 点，并回复 10 点生命。" },
            new RelicDef { Id = "hunter_mark", Name = "猎手刻印", Description = "每场战斗的第一张攻击牌额外造成 5 点伤害。" }
        };

        private void Start()
        {
            Application.targetFrameRate = 60;
            _rng = new System.Random(Environment.TickCount);
            BuildCanvas();
            ShowTitleScreen();
        }

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame && _modalRoot != null)
            {
                CloseModal();
                return;
            }

            if (_run == null || _modalRoot != null || !_inCombat) return;
            if (!_enemyTurn && !_combatOver)
            {
                for (var i = 0; i < Mathf.Min(9, _hand.Count); i++)
                {
                    if (Keyboard.current[NumberKeys[i]].wasPressedThisFrame)
                    {
                        PlayCard(_hand[i]);
                        return;
                    }
                }

                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    EndPlayerTurn();
                }
            }
        }

        private void BuildCanvas()
        {
            var canvasObject = new GameObject(
                "EmberEchoCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 20;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            _backdrop = EmberUI.Stretch("Backdrop", _canvas.transform);
            BuildBackdrop();
            _screenRoot = EmberUI.Stretch("Screen", _canvas.transform);

            if (EventSystem.current == null)
            {
                var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(transform, false);
                var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
                try
                {
                    inputModule.AssignDefaultActions();
                }
                catch (Exception exception)
                {
                    Debug.LogWarning("Could not assign default UI input actions: " + exception.Message);
                }
            }

            _audio = gameObject.AddComponent<AudioBank>();
        }

        private void BuildBackdrop()
        {
            var baseImage = EmberUI.Stretch("Void", _backdrop).gameObject.AddComponent<Image>();
            baseImage.color = EmberUI.Ink;
            baseImage.raycastTarget = false;

            var stone = EmberUI.Raw(
                _backdrop,
                "EmberArt/bg_stone",
                Vector2.zero,
                new Vector2(2240f, 1320f),
                new Vector2(7f, 4.2f),
                new Color(0.27f, 0.28f, 0.23f, 0.36f));
            var drift = stone.gameObject.AddComponent<DriftTexture>();
            drift.SpeedX = 0.0028f;
            drift.SpeedY = 0.0005f;

            var moon = EmberUI.Shape(_backdrop, EmberUI.Glow, new Vector2(610f, 280f), new Vector2(980f, 980f), new Color(0.82f, 0.52f, 0.24f, 0.22f));
            moon.name = "DistantEmber";
            moon.gameObject.AddComponent<PulseLoop>().Amount = 0.018f;

            var lowGlow = EmberUI.Shape(_backdrop, EmberUI.Glow, new Vector2(-460f, -410f), new Vector2(1000f, 620f), new Color(0.42f, 0.2f, 0.11f, 0.26f));
            lowGlow.raycastTarget = false;

            var vignetteRect = EmberUI.Stretch("Vignette", _backdrop);
            var vignette = vignetteRect.gameObject.AddComponent<Image>();
            vignette.sprite = EmberUI.Vignette;
            vignette.color = Color.white;
            vignette.raycastTarget = false;

            var grainRect = EmberUI.Stretch("Grain", _backdrop);
            var grain = grainRect.gameObject.AddComponent<Image>();
            grain.sprite = EmberUI.Grain;
            grain.color = new Color(1f, 0.92f, 0.76f, 0.055f);
            grain.raycastTarget = false;

            for (var i = 0; i < 26; i++)
            {
                var size = UnityEngine.Random.Range(2f, 6f);
                var dust = EmberUI.Shape(
                    _backdrop,
                    EmberUI.Circle,
                    new Vector2(UnityEngine.Random.Range(-930f, 930f), UnityEngine.Random.Range(-520f, 520f)),
                    Vector2.one * size,
                    new Color(0.9f, 0.62f, 0.3f, UnityEngine.Random.Range(0.08f, 0.24f)));
                var particle = dust.gameObject.AddComponent<SlowParticle>();
                particle.Configure(UnityEngine.Random.Range(4f, 16f), UnityEngine.Random.Range(7f, 15f));
            }
        }

        private void ClearScreen()
        {
            StopAllCoroutines();
            _turnRoutine = null;
            _modalRoot = null;
            _inCombat = false;
            _enemyViews.Clear();
            EmberUI.Clear(_screenRoot);
        }

        private void ShowTitleScreen()
        {
            _run = null;
            ClearScreen();
            _busy = false;
            _enemyTurn = false;
            _combatOver = false;
            _headerHpText = null;
            _headerGoldText = null;
            _headerRelicText = null;

            var stage = EmberUI.Center("TitleStage", _screenRoot, Vector2.zero, new Vector2(1880f, 1000f));

            var halo = EmberUI.Shape(stage, EmberUI.Glow, new Vector2(-520f, 60f), new Vector2(830f, 830f), new Color(0.92f, 0.53f, 0.24f, 0.26f));
            halo.gameObject.AddComponent<PulseLoop>().Amount = 0.025f;
            var outerRing = EmberUI.Shape(stage, EmberUI.Ring, new Vector2(-520f, 60f), new Vector2(580f, 580f), new Color(0.82f, 0.64f, 0.36f, 0.22f));
            outerRing.gameObject.AddComponent<PulseLoop>().Amount = 0.012f;
            EmberUI.Shape(stage, EmberUI.Ring, new Vector2(-520f, 60f), new Vector2(470f, 470f), new Color(0.82f, 0.64f, 0.36f, 0.12f));
            EmberUI.Shape(stage, EmberUI.Circle, new Vector2(-520f, 0f), new Vector2(360f, 78f), new Color(0f, 0f, 0f, 0.34f));
            var hero = CharacterVisual(stage, "hero_ember", new Vector2(-520f, 75f), new Vector2(400f, 400f), Color.white, 5f, 1.22f);
            hero.gameObject.AddComponent<FloatBob>().Distance = 10f;

            EmberUI.Label(stage, "A ROGUELIKE OF ASH AND MEMORY", 18, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(275f, 355f), new Vector2(760f, 34f), FontStyle.Bold);
            EmberUI.Label(stage, "余烬回响", 92, EmberUI.Parchment, TextAnchor.MiddleLeft, new Vector2(275f, 265f), new Vector2(780f, 125f), FontStyle.Bold);
            EmberUI.Label(stage, "每一条路，都记得你的名字。", 25, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(278f, 175f), new Vector2(680f, 46f));
            EmberUI.Line(stage, new Vector2(-90f, 120f), new Vector2(650f, 120f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.36f));

            EmberUI.Label(stage, "单局构筑 · 三幕旅程 · 约 30 分钟", 18, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(278f, 88f), new Vector2(650f, 34f));

            var start = EmberUI.Button(
                stage,
                "点燃旅程",
                new Vector2(220f, -20f),
                new Vector2(360f, 72f),
                new Color32(164, 84, 48, 255),
                StartNewRun,
                25,
                EmberUI.Parchment);
            AddButtonDepth(start);

            EmberUI.Button(
                stage,
                "旅途规则",
                new Vector2(220f, -110f),
                new Vector2(360f, 58f),
                new Color32(36, 42, 37, 255),
                ShowHelpOverlay,
                20,
                EmberUI.Parchment);

            EmberUI.Label(stage, "每一次选择，都会让下一场战斗不同。", 18, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(278f, -188f), new Vector2(640f, 34f));

            var samples = new[]
            {
                CardLibrary.GetBase("flame_strike"),
                CardLibrary.GetBase("ember_armor"),
                CardLibrary.GetBase("echo")
            };
            var samplePositions = new[]
            {
                new Vector2(610f, 50f),
                new Vector2(720f, 32f),
                new Vector2(830f, 14f)
            };
            for (var i = 0; i < samples.Length; i++)
            {
                var view = CreateCardView(samples[i], stage, samplePositions[i], new Vector2(218f, 300f), false, null, false);
                view.transform.localEulerAngles = new Vector3(0f, 0f, (i - 1) * -5f);
                var group = view.AddComponent<CanvasGroup>();
                group.alpha = 0.94f;
            }

            EmberUI.Label(_screenRoot, "灰烬之后，仍有人向前。", 16, new Color(0.68f, 0.65f, 0.57f, 0.7f), TextAnchor.MiddleCenter, new Vector2(0f, -495f), new Vector2(800f, 32f));
        }

        private static void AddButtonDepth(Button button)
        {
            var shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            shadow.effectDistance = new Vector2(0f, -5f);
        }

        private static Graphic CharacterVisual(
            Transform parent,
            string resourceKey,
            Vector2 position,
            Vector2 size,
            Color color,
            float turnAmplitude = 6f,
            float framing = 1.35f)
        {
            var portrait = EmberCharacterPortrait.Create(parent, resourceKey, position, size, turnAmplitude, framing);
            if (portrait != null)
            {
                portrait.color = color;
                return portrait;
            }

            return EmberUI.Picture(parent, resourceKey, position, size, color, true);
        }

        private void ShowHelpOverlay()
        {
            if (_modalRoot != null) return;
            var modal = CreateModal("旅途规则", new Vector2(1080f, 720f));

            EmberUI.LabelStretch(
                modal,
                "你是一位沿着熄灭星路前行的行者。\n\n" +
                "每回合获得 3 点能量，从抽牌堆抽取 5 张牌。打出卡牌需要消耗对应能量；结束回合后，敌人会执行头顶所示的意图。\n\n" +
                "攻击牌会积累余烬。达到 3 点时，下一张攻击牌额外造成 4 点伤害并清空余烬。\n\n" +
                "护甲可以抵挡本回合伤害，但会在你的新回合开始时散去。精英敌人会留下遗物，营火可以恢复生命或升级卡牌。\n\n" +
                "穿越三幕，抵达熄灭王座，并在无光星骸前活下来。",
                21,
                EmberUI.Parchment,
                TextAnchor.UpperLeft,
                95f,
                95f,
                135f,
                105f);

            EmberUI.Label(modal, "视觉素材：Kenney · CC0", 15, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-360f, -280f), new Vector2(430f, 30f));
            EmberUI.Button(modal, "返回", new Vector2(375f, -280f), new Vector2(170f, 52f), EmberUI.PanelLight, CloseModal, 18);
        }

        private void StartNewRun()
        {
            StopAllCoroutines();
            _run = new RunState
            {
                Map = RunMap.Build(),
                MaxHp = 72,
                Hp = 72,
                Gold = 80,
                StartedAt = Time.realtimeSinceStartup
            };
            _run.Deck = CardLibrary.CreateStartingDeck();
            _run.Relics.Add("ember_lamp");
            _lastAcquiredRelicId = null;
            foreach (var act in _run.Map.Acts)
            {
                _run.Choices.Add(Enumerable.Repeat(-1, act.Rows.Count).ToList());
            }

            _audio.Play("shuffle", 0.3f, 0.92f);
            ShowActIntro(0);
        }

        private void ShowActIntro(int actIndex)
        {
            ClearScreen();
            _run.Act = actIndex;
            _run.Row = 0;
            _busy = false;

            var act = _run.Map.Acts[actIndex];
            var stage = EmberUI.Center("ActIntro", _screenRoot, Vector2.zero, new Vector2(1600f, 920f));
            EmberUI.Shape(stage, EmberUI.Glow, new Vector2(0f, 20f), new Vector2(820f, 820f), new Color(0.8f, 0.39f, 0.18f, 0.18f));
            EmberUI.Shape(stage, EmberUI.Ring, new Vector2(0f, 20f), new Vector2(500f, 500f), new Color(0.82f, 0.64f, 0.36f, 0.16f));
            EmberUI.Shape(stage, EmberUI.Ring, new Vector2(0f, 20f), new Vector2(360f, 360f), new Color(0.82f, 0.64f, 0.36f, 0.09f));

            EmberUI.Label(stage, "第 " + ChineseNumber(actIndex + 1) + " 幕", 22, EmberUI.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 290f), new Vector2(360f, 40f), FontStyle.Bold);
            EmberUI.Label(stage, act.Roman, 150, new Color(0.93f, 0.86f, 0.7f, 0.98f), TextAnchor.MiddleCenter, new Vector2(0f, 105f), new Vector2(240f, 200f), FontStyle.Bold);
            EmberUI.Label(stage, act.Name, 52, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -65f), new Vector2(800f, 80f), FontStyle.Bold);
            EmberUI.Label(stage, act.Subtitle, 24, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, -135f), new Vector2(700f, 50f));
            EmberUI.Line(stage, new Vector2(-270f, -190f), new Vector2(270f, -190f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.34f));

            EmberUI.Button(stage, "向深处前行", new Vector2(0f, -285f), new Vector2(310f, 64f), new Color32(145, 75, 45, 255), ShowMap, 21);
        }

        private void ShowMap()
        {
            ClearScreen();
            _busy = false;
            _enemyTurn = false;
            _combatOver = false;

            var act = _run.Map.Acts[_run.Act];
            CreateHeader("灰烬之路", "第 " + ChineseNumber(_run.Act + 1) + " 幕 · " + act.Name, true);

            var leftRail = EmberUI.Panel(_screenRoot, new Vector2(-650f, 25f), new Vector2(300f, 810f), new Color(0.08f, 0.1f, 0.08f, 0.92f));
            EmberUI.Label(leftRail.transform, "行者", 15, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(-74f, 355f), new Vector2(150f, 30f), FontStyle.Bold);
            EmberUI.Shape(leftRail.transform, EmberUI.Glow, new Vector2(0f, 242f), new Vector2(260f, 260f), new Color(0.7f, 0.37f, 0.17f, 0.18f));
            CharacterVisual(leftRail.transform, "hero_ember", new Vector2(0f, 255f), new Vector2(160f, 160f), Color.white, 4f, 1.28f);
            EmberUI.Label(leftRail.transform, "余烬行者", 27, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, 132f), new Vector2(240f, 38f), FontStyle.Bold);
            EmberUI.Label(leftRail.transform, "第七次循环", 16, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, 102f), new Vector2(220f, 28f));
            EmberUI.Bar(leftRail.transform, new Vector2(0f, 63f), new Vector2(230f, 22f), new Color(0.15f, 0.07f, 0.06f, 1f), EmberUI.Blood, _run.Hp / (float)_run.MaxHp);
            EmberUI.Label(leftRail.transform, string.Format("{0} / {1}", _run.Hp, _run.MaxHp), 16, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, 63f), new Vector2(230f, 24f), FontStyle.Bold);

            EmberUI.Label(leftRail.transform, "遗物", 16, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(-74f, 6f), new Vector2(150f, 28f), FontStyle.Bold);
            for (var i = 0; i < Mathf.Min(_run.Relics.Count, 5); i++)
            {
                var relic = Relics.FirstOrDefault(item => item.Id == _run.Relics[i]);
                if (relic == null) continue;
                var y = -30f - i * 40f;
                EmberUI.Shape(leftRail.transform, EmberUI.Diamond, new Vector2(-92f, y), new Vector2(14f, 14f), EmberUI.Gold);
                EmberUI.Label(leftRail.transform, relic.Name, 18, EmberUI.Parchment, TextAnchor.MiddleLeft, new Vector2(-30f, y), new Vector2(150f, 28f));
            }

            EmberUI.Button(leftRail.transform, "查看牌组", new Vector2(0f, -320f), new Vector2(220f, 56f), EmberUI.PanelLight, () => OpenDeckOverlay(false, null), 19);

            DrawMapRoute();

            var note = EmberUI.Panel(_screenRoot, new Vector2(760f, 25f), new Vector2(300f, 810f), new Color(0.07f, 0.09f, 0.08f, 0.9f));
            EmberUI.Label(note.transform, "下一步", 15, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(-112f, 355f), new Vector2(150f, 30f), FontStyle.Bold);
            var available = act.Rows[_run.Row];
            var optionsText = string.Join(
                "\n\n",
                available.Choices.Select((kind, index) =>
                    string.Format("{0}. {1}\n{2}", index + 1, NodeInfo.Label(kind), NodeInfo.Description(kind))).ToArray());
            EmberUI.Label(note.transform, optionsText, 19, EmberUI.Parchment, TextAnchor.UpperLeft, new Vector2(0f, 210f), new Vector2(235f, 220f));

            EmberUI.Line(note.transform, new Vector2(-115f, 105f), new Vector2(115f, 105f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.24f));
            EmberUI.Label(note.transform, act.Subtitle, 23, EmberUI.Parchment, TextAnchor.UpperLeft, new Vector2(0f, 50f), new Vector2(235f, 80f), FontStyle.Bold);
            EmberUI.Label(
                note.transform,
                "敌人的头顶会显示下一回合行动。\n\n护甲只抵挡当前一轮伤害。\n\n击败精英可以获得遗物。",
                17,
                EmberUI.Muted,
                TextAnchor.UpperLeft,
                new Vector2(0f, -165f),
                new Vector2(235f, 180f));

            EmberUI.Label(_screenRoot, string.Format("节点 {0} / {1}", _run.Row + 1, act.Rows.Count), 16, EmberUI.Muted, TextAnchor.MiddleRight, new Vector2(760f, -430f), new Vector2(280f, 30f));
        }

        private void ShowReturnToTitleConfirmation()
        {
            if (_modalRoot != null) return;
            var modal = CreateModal("返回主页面", new Vector2(760f, 410f));
            EmberUI.Label(
                modal,
                "当前旅途尚未结束，返回主页面将放弃本局进度。",
                21,
                EmberUI.Parchment,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 52f),
                new Vector2(610f, 72f));
            EmberUI.Label(
                modal,
                "要结束这次旅程吗？",
                18,
                EmberUI.Muted,
                TextAnchor.MiddleCenter,
                new Vector2(0f, 5f),
                new Vector2(500f, 40f));
            EmberUI.Button(modal, "继续旅程", new Vector2(-120f, -105f), new Vector2(190f, 54f), EmberUI.PanelLight, CloseModal, 18);
            EmberUI.Button(modal, "返回主页", new Vector2(120f, -105f), new Vector2(190f, 54f), new Color32(145, 75, 45, 255), () =>
            {
                CloseModal();
                ShowTitleScreen();
            }, 18);
        }

        private void DrawMapRoute()
        {
            var actXs = new[] { -355f, 45f, 445f };
            var rowYs = new[] { -170f, -10f, 150f, 310f };

            for (var actIndex = 0; actIndex < _run.Map.Acts.Count; actIndex++)
            {
                var act = _run.Map.Acts[actIndex];
                var actX = actXs[actIndex];
                var completedRows = actIndex < _run.Act
                    ? act.Rows.Count
                    : actIndex == _run.Act ? _run.Row : 0;

                for (var row = 0; row < act.Rows.Count; row++)
                {
                    var previous = row == 0
                        ? new[] { new Vector2(actX, -295f) }
                        : act.Rows[row - 1].Choices.Select((kind, index) => MapNodePosition(actX, rowYs[row - 1], act.Rows[row - 1].Choices.Length, index)).ToArray();
                    for (var choice = 0; choice < act.Rows[row].Choices.Length; choice++)
                    {
                        var target = MapNodePosition(actX, rowYs[row], act.Rows[row].Choices.Length, choice);
                        foreach (var source in previous)
                        {
                            var lineColor = actIndex < _run.Act
                                ? new Color(0.45f, 0.58f, 0.38f, 0.2f)
                                : actIndex == _run.Act
                                    ? new Color(0.76f, 0.55f, 0.31f, row <= _run.Row ? 0.28f : 0.14f)
                                    : new Color(0.45f, 0.43f, 0.35f, 0.1f);
                            EmberUI.Line(_screenRoot, source, target, 2f, lineColor);
                        }
                    }
                }

                if (completedRows > 0)
                {
                    var previous = new Vector2(actX, -295f);
                    for (var row = 0; row < completedRows; row++)
                    {
                        var selected = _run.Choices[actIndex][row];
                        if (selected < 0) continue;
                        var target = MapNodePosition(actX, rowYs[row], act.Rows[row].Choices.Length, selected);
                        EmberUI.Line(_screenRoot, previous, target, 4f, new Color(0.86f, 0.59f, 0.29f, 0.6f));
                        previous = target;
                    }
                }

                var columnColor = actIndex == _run.Act
                    ? EmberUI.Gold
                    : actIndex < _run.Act ? EmberUI.Moss : new Color(0.44f, 0.42f, 0.35f, 1f);
                EmberUI.Label(_screenRoot, act.Roman, 36, columnColor, TextAnchor.MiddleCenter, new Vector2(actX, 410f), new Vector2(80f, 46f), FontStyle.Bold);
                EmberUI.Label(_screenRoot, act.Name, 19, actIndex == _run.Act ? EmberUI.Parchment : EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(actX, 376f), new Vector2(270f, 32f));

                for (var row = 0; row < act.Rows.Count; row++)
                {
                    var choices = act.Rows[row].Choices;
                    var isPast = actIndex < _run.Act || (actIndex == _run.Act && row < _run.Row);
                    var isCurrent = actIndex == _run.Act && row == _run.Row;
                    for (var choice = 0; choice < choices.Length; choice++)
                    {
                        var choiceIndex = choice;
                        var position = MapNodePosition(actX, rowYs[row], choices.Length, choice);
                        var selected = isPast && _run.Choices[actIndex][row] == choice;
                        CreateMapNode(
                            choices[choice],
                            position,
                            isCurrent,
                            isPast,
                            selected,
                            isCurrent ? (Action)(() => ChooseNode(choiceIndex)) : null);
                    }
                }
            }
        }

        private static Vector2 MapNodePosition(float actX, float rowY, int choiceCount, int choice)
        {
            var offset = choiceCount == 1 ? 0f : choice == 0 ? -66f : 66f;
            return new Vector2(actX + offset, rowY);
        }

        private void CreateMapNode(NodeKind kind, Vector2 position, bool available, bool past, bool selected, Action onClick)
        {
            var root = EmberUI.Center("MapNode_" + NodeInfo.Label(kind), _screenRoot, position, new Vector2(110f, 110f));
            var ring = EmberUI.Shape(root, EmberUI.Circle, Vector2.zero, new Vector2(104f, 104f), new Color(0.13f, 0.16f, 0.13f, 0.95f));
            ring.raycastTarget = true;
            var button = ring.gameObject.AddComponent<Button>();
            button.targetGraphic = ring;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.16f, 1.1f, 1f, 1f),
                pressedColor = new Color(0.8f, 0.78f, 0.72f, 1f),
                selectedColor = Color.white,
                disabledColor = Color.white,
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            button.interactable = available && onClick != null;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var borderColor = selected
                ? EmberUI.Moss
                : available
                    ? EmberUI.Gold
                    : past
                        ? new Color(0.44f, 0.52f, 0.36f, 0.62f)
                        : new Color(0.38f, 0.37f, 0.32f, 0.48f);
            EmberUI.Shape(root, EmberUI.Ring, Vector2.zero, new Vector2(106f, 106f), borderColor);

            var iconColor = available || selected
                ? Color.white
                : new Color(1f, 1f, 1f, past ? 0.52f : 0.24f);
            EmberUI.Picture(root, NodeInfo.Icon(kind), Vector2.zero, new Vector2(69f, 69f), iconColor, true);
            EmberUI.Label(root, NodeInfo.Label(kind), 15, available ? EmberUI.Parchment : EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, -70f), new Vector2(116f, 28f), available ? FontStyle.Bold : FontStyle.Normal);

            if (available)
            {
                var pulse = EmberUI.Shape(root, EmberUI.Ring, Vector2.zero, new Vector2(118f, 118f), new Color(0.9f, 0.6f, 0.28f, 0.24f));
                pulse.gameObject.AddComponent<PulseLoop>().Amount = 0.06f;
            }

            if (selected)
            {
                EmberUI.Shape(root, EmberUI.Diamond, new Vector2(41f, 41f), new Vector2(17f, 17f), EmberUI.Gold);
            }
        }

        private void ChooseNode(int choiceIndex)
        {
            if (_busy || _run == null) return;
            var act = _run.Map.Acts[_run.Act];
            var row = act.Rows[_run.Row];
            if (choiceIndex < 0 || choiceIndex >= row.Choices.Length) return;
            _run.Choices[_run.Act][_run.Row] = choiceIndex;
            _run.ChosenNode = choiceIndex;
            _currentNodeKind = row.Choices[choiceIndex];
            _audio.Play("card_slide", 0.3f, 0.9f);

            switch (_currentNodeKind)
            {
                case NodeKind.Combat:
                case NodeKind.Elite:
                case NodeKind.Boss:
                    StartCombat(_currentNodeKind);
                    break;
                case NodeKind.Rest:
                    ShowCampScreen();
                    break;
                case NodeKind.Shop:
                    ShowShopScreen();
                    break;
                case NodeKind.Event:
                    ShowEventScreen();
                    break;
            }
        }

        private void CreateHeader(string kicker, string title, bool showHomeButton = false)
        {
            var header = EmberUI.Panel(_screenRoot, new Vector2(0f, 500f), new Vector2(1840f, 78f), new Color(0.055f, 0.07f, 0.06f, 0.95f));
            EmberUI.Picture(header.transform, "icon_fire", new Vector2(-832f, 0f), new Vector2(48f, 48f), Color.white, true);
            EmberUI.Label(header.transform, "余烬回响", 18, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(-730f, 0f), new Vector2(160f, 34f), FontStyle.Bold);
            EmberUI.Line(header.transform, new Vector2(-630f, -23f), new Vector2(-630f, 23f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.25f));
            EmberUI.Label(header.transform, kicker, 13, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-500f, 15f), new Vector2(220f, 20f), FontStyle.Bold);
            EmberUI.Label(header.transform, title, 21, EmberUI.Parchment, TextAnchor.MiddleLeft, new Vector2(-500f, -10f), new Vector2(340f, 30f), FontStyle.Bold);

            _headerHpText = EmberUI.Label(header.transform, string.Empty, 17, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(95f, 0f), new Vector2(170f, 32f), FontStyle.Bold);
            _headerGoldText = EmberUI.Label(header.transform, string.Empty, 17, EmberUI.Gold, TextAnchor.MiddleCenter, new Vector2(260f, 0f), new Vector2(150f, 32f), FontStyle.Bold);
            _headerRelicText = EmberUI.Label(header.transform, string.Empty, 16, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(400f, 0f), new Vector2(135f, 32f));
            if (showHomeButton)
            {
                EmberUI.Button(header.transform, "返回主页", new Vector2(645f, 0f), new Vector2(126f, 46f), new Color32(61, 40, 34, 255), ShowReturnToTitleConfirmation, 16);
            }
            EmberUI.Button(header.transform, "牌组", new Vector2(780f, 0f), new Vector2(104f, 46f), EmberUI.PanelLight, () => OpenDeckOverlay(false, null), 17);
            RefreshHeader();
        }

        private void RefreshHeader()
        {
            if (_run == null) return;
            if (_headerHpText != null) _headerHpText.text = string.Format("生命  {0}/{1}", _run.Hp, _run.MaxHp);
            if (_headerGoldText != null) _headerGoldText.text = string.Format("金币  {0}", _run.Gold);
            if (_headerRelicText != null) _headerRelicText.text = string.Format("遗物  {0}", _run.Relics.Count);
        }

        private void OpenDeckOverlay(bool upgradeMode, Action<CardDef> onSelect)
        {
            if (_run == null || _modalRoot != null) return;
            var cards = upgradeMode ? _run.Deck.Where(card => !card.Upgraded).ToList() : _run.Deck;
            var title = upgradeMode ? "选择一张牌进行锻造" : "余烬牌组";
            var modal = CreateModal(title, new Vector2(1840f, 930f));
            EmberUI.Label(modal, string.Format("{0} 张卡牌 · 平均费用 {1:0.0}", _run.Deck.Count, _run.Deck.Average(card => card.Cost)), 17, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-760f, 355f), new Vector2(400f, 30f));

            if (cards.Count == 0)
            {
                EmberUI.Label(modal, "牌组中已经没有可锻造的卡牌。", 24, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, 40f), new Vector2(700f, 60f));
            }
            else
            {
                for (var i = 0; i < cards.Count && i < 21; i++)
                {
                    var column = i % 7;
                    var row = i / 7;
                    var card = cards[i];
                    var position = new Vector2(-690f + column * 230f, 195f - row * 245f);
                    var action = onSelect == null
                        ? null
                        : new Action(() =>
                        {
                            CloseModal();
                            onSelect(card);
                        });
                    CreateCardView(card, modal, position, new Vector2(218f, 300f), action != null, action, true, 0.73f);
                }
            }

            EmberUI.Button(modal, "关闭", new Vector2(790f, 365f), new Vector2(150f, 48f), EmberUI.PanelLight, CloseModal, 17);
        }

        private RectTransform CreateModal(string title, Vector2 size)
        {
            if (_modalRoot != null)
            {
                Destroy(_modalRoot);
            }

            var blocker = EmberUI.Stretch("Modal", _screenRoot);
            _modalRoot = blocker.gameObject;
            var blockerImage = blocker.gameObject.AddComponent<Image>();
            blockerImage.color = new Color(0f, 0f, 0f, 0.76f);
            blockerImage.raycastTarget = true;

            var panel = EmberUI.Panel(blocker, Vector2.zero, size, new Color(0.075f, 0.09f, 0.075f, 0.99f));
            EmberUI.Shape(
                panel.transform,
                EmberUI.Ring,
                new Vector2(size.x * 0.41f, size.y * 0.39f),
                new Vector2(260f, 260f),
                new Color(0.82f, 0.64f, 0.36f, 0.08f));
            EmberUI.Label(panel.transform, title, 28, EmberUI.Parchment, TextAnchor.MiddleLeft, new Vector2(-size.x * 0.5f + 390f, size.y * 0.5f - 62f), new Vector2(620f, 42f), FontStyle.Bold);
            EmberUI.Line(panel.transform, new Vector2(-size.x * 0.5f + 70f, size.y * 0.5f - 94f), new Vector2(size.x * 0.5f - 70f, size.y * 0.5f - 94f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.26f));
            return panel.rectTransform;
        }

        private void CloseModal()
        {
            if (_modalRoot == null) return;
            Destroy(_modalRoot);
            _modalRoot = null;
        }

        private void ShowCampScreen()
        {
            ClearScreen();
            _busy = false;
            CreateHeader("路线事件", "营火");

            var stage = EmberUI.Center("Camp", _screenRoot, new Vector2(0f, -15f), new Vector2(1200f, 760f));
            EmberUI.Shape(stage, EmberUI.Glow, new Vector2(0f, 115f), new Vector2(600f, 600f), new Color(0.84f, 0.39f, 0.14f, 0.24f));
            EmberUI.Shape(stage, EmberUI.Ring, new Vector2(0f, 110f), new Vector2(360f, 360f), new Color(0.82f, 0.64f, 0.36f, 0.16f));
            var fire = EmberUI.Picture(stage, "icon_fire", new Vector2(0f, 120f), new Vector2(230f, 230f), Color.white, true);
            fire.gameObject.AddComponent<FloatBob>().Distance = 6f;
            fire.gameObject.AddComponent<PulseLoop>().Amount = 0.025f;
            EmberUI.Label(stage, "短暂的安宁", 45, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -60f), new Vector2(700f, 70f), FontStyle.Bold);
            EmberUI.Label(stage, "火焰替你记住来路，也照亮下一段黑暗。", 21, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, -118f), new Vector2(760f, 40f));

            var restButton = EmberUI.Button(
                stage,
                string.Format("休整 · 回复 {0} 生命", Mathf.CeilToInt(_run.MaxHp * 0.3f)),
                new Vector2(-155f, -230f),
                new Vector2(285f, 65f),
                new Color32(72, 99, 71, 255),
                () =>
                {
                    if (_busy) return;
                    _busy = true;
                    var heal = Mathf.CeilToInt(_run.MaxHp * 0.3f);
                    _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + heal);
                    _audio.Play("card_play_1", 0.35f, 0.82f);
                    AdvanceAfterNode();
                },
                20);
            AddButtonDepth(restButton);

            var forgeButton = EmberUI.Button(
                stage,
                "锻造 · 升级一张牌",
                new Vector2(155f, -230f),
                new Vector2(285f, 65f),
                new Color32(74, 96, 94, 255),
                () =>
                {
                    if (_busy) return;
                    if (_run.Deck.All(card => card.Upgraded)) return;
                    OpenDeckOverlay(true, card =>
                    {
                        _busy = true;
                        var index = _run.Deck.IndexOf(card);
                        if (index >= 0) _run.Deck[index] = card.Upgrade();
                        _audio.Play("card_play_2", 0.35f, 1.08f);
                        AdvanceAfterNode();
                    });
                },
                20);
            AddButtonDepth(forgeButton);
        }

        private void ShowShopScreen()
        {
            ClearScreen();
            _busy = false;
            CreateHeader("路线事件", "流浪商队");

            var stage = EmberUI.Center("Shop", _screenRoot, new Vector2(0f, -10f), new Vector2(1640f, 780f));
            CharacterVisual(stage, "npc_guard", new Vector2(-660f, 120f), new Vector2(190f, 190f), Color.white, 5f, 1.32f);
            EmberUI.Label(stage, "“火不会赊账，但我可以。”", 32, EmberUI.Parchment, TextAnchor.MiddleLeft, new Vector2(-70f, 290f), new Vector2(1000f, 56f), FontStyle.Bold);
            EmberUI.Label(stage, "挑选一件能带你走得更远的东西。", 19, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-70f, 240f), new Vector2(850f, 36f));

            var pool = BuildCardRewards(3);
            var entries = new List<ShopEntry>();
            for (var i = 0; i < pool.Count; i++)
            {
                var card = pool[i];
                var price = card.Rarity == CardRarity.Rare ? 85 : card.Rarity == CardRarity.Uncommon ? 65 : 48;
                var entry = new ShopEntry { Card = card, Price = price };
                entries.Add(entry);
                var position = new Vector2(-340f + i * 340f, 65f);
                entry.CardView = CreateCardView(card, stage, position, new Vector2(218f, 300f), false, null, false, 1.1f);
                entry.BuyButton = EmberUI.Button(
                    stage,
                    price + " 金",
                    position + new Vector2(0f, -195f),
                    new Vector2(176f, 48f),
                    new Color32(112, 78, 44, 255),
                    () =>
                    {
                        if (entry.Sold || _run.Gold < entry.Price) return;
                        _run.Gold -= entry.Price;
                        _run.Deck.Add(entry.Card.Clone());
                        entry.Sold = true;
                        entry.BuyButton.interactable = false;
                        entry.BuyText.text = "已售";
                        entry.BuyText.color = EmberUI.Muted;
                        var cardGroup = entry.CardView.GetComponent<CanvasGroup>();
                        if (cardGroup == null) cardGroup = entry.CardView.AddComponent<CanvasGroup>();
                        cardGroup.alpha = 0.5f;
                        RefreshHeader();
                        _audio.Play("card_play_1", 0.32f, 1.05f);
                    },
                    17);
                entry.BuyText = entry.BuyButton.GetComponentInChildren<Text>();
            }

            var healButton = EmberUI.Button(
                stage,
                "草药汤 · 35 金 · 回复 15 生命",
                new Vector2(-225f, -270f),
                new Vector2(330f, 54f),
                new Color32(69, 96, 68, 255),
                () =>
                {
                    if (_run.Gold < 35) return;
                    _run.Gold -= 35;
                    _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + 15);
                    RefreshHeader();
                    _audio.Play("card_play_1", 0.28f, 0.85f);
                },
                17);

            EmberUI.Button(
                stage,
                "净化牌组 · 45 金",
                new Vector2(170f, -270f),
                new Vector2(250f, 54f),
                new Color32(78, 84, 76, 255),
                () =>
                {
                    if (_run.Gold < 45 || _run.Deck.Count <= 5) return;
                    OpenDeckOverlay(false, card =>
                    {
                        if (_run.Gold < 45) return;
                        _run.Gold -= 45;
                        _run.Deck.Remove(card);
                        RefreshHeader();
                        _audio.Play("shuffle", 0.25f, 0.8f);
                    });
                },
                17);

            EmberUI.Button(
                stage,
                "离开商队",
                new Vector2(590f, -270f),
                new Vector2(220f, 54f),
                new Color32(130, 75, 48, 255),
                () =>
                {
                    if (_busy) return;
                    _busy = true;
                    AdvanceAfterNode();
                },
                18);

            EmberUI.Label(stage, "被选中的卡牌会直接加入牌组。", 15, EmberUI.Muted, TextAnchor.MiddleRight, new Vector2(620f, 90f), new Vector2(300f, 30f));
        }

        private void ShowEventScreen()
        {
            ClearScreen();
            _busy = false;
            CreateHeader("路线事件", "异闻");

            var events = BuildEvents();
            var content = events[_rng.Next(events.Count)];
            var title = content.Item1;
            var body = content.Item2;
            var choices = content.Item3;

            var stage = EmberUI.Panel(_screenRoot, new Vector2(0f, -10f), new Vector2(1260f, 710f), new Color(0.075f, 0.085f, 0.07f, 0.96f));
            EmberUI.Shape(stage.transform, EmberUI.Glow, new Vector2(400f, 230f), new Vector2(520f, 520f), new Color(0.72f, 0.36f, 0.17f, 0.18f));
            EmberUI.Picture(stage.transform, "icon_rune", new Vector2(405f, 220f), new Vector2(180f, 180f), Color.white, true);
            EmberUI.Label(stage.transform, title, 42, EmberUI.Parchment, TextAnchor.MiddleLeft, new Vector2(-410f, 255f), new Vector2(600f, 64f), FontStyle.Bold);
            EmberUI.Line(stage.transform, new Vector2(-540f, 200f), new Vector2(540f, 200f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.25f));
            EmberUI.Label(stage.transform, body, 23, EmberUI.Parchment, TextAnchor.UpperLeft, new Vector2(-45f, 88f), new Vector2(970f, 175f));

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var y = -155f - i * 83f;
                var button = EmberUI.Button(
                    stage.transform,
                    choice.Label,
                    new Vector2(-130f, y),
                    new Vector2(930f, 58f),
                    new Color32(39, 46, 39, 255),
                    () =>
                    {
                        if (_busy) return;
                        choice.Apply();
                    },
                    19);
                button.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                EmberUI.Label(stage.transform, choice.Detail, 15, EmberUI.Muted, TextAnchor.MiddleRight, new Vector2(410f, y), new Vector2(380f, 32f));
            }
        }

        private List<Tuple<string, string, List<EventChoice>>> BuildEvents()
        {
            var events = new List<Tuple<string, string, List<EventChoice>>>();

            events.Add(Tuple.Create<string, string, List<EventChoice>>(
                "断桥下的火",
                "桥已经烧断。下方却有一簇火没有熄灭，火星间浮着几枚古旧金币。\n你可以伸手取走它，也可以把仅剩的力气留给前路。",
                new List<EventChoice>
                {
                    EventOption("伸手取火", "失去 5 生命，获得 30 金", () =>
                    {
                        _run.Hp = Mathf.Max(1, _run.Hp - 5);
                        _run.Gold += 30;
                        ResolveEvent("火种仍在", "金币已经冷却，掌心却留下了一道新伤。");
                    }),
                    EventOption("踏过断桥", "回复 6 生命", () =>
                    {
                        _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + 6);
                        ResolveEvent("没有回头", "你没有带走任何东西，只听见身后传来轻微的爆裂声。");
                    })
                }));

            events.Add(Tuple.Create<string, string, List<EventChoice>>(
                "盲眼占卜师",
                "一个披着旧布的占卜师坐在路中央。她看不见你，却准确地说出了你牌组中最锋利的那张牌。\n“命运的边角可以磨亮，只要你付得起价。”",
                new List<EventChoice>
                {
                    EventOption("支付 40 金", "随机升级一张未升级卡牌", () =>
                    {
                        if (_run.Gold < 40)
                        {
                            ResolveEvent("行囊太轻", "你翻遍口袋，最终只能向她道别。");
                            return;
                        }

                        _run.Gold -= 40;
                        var candidates = _run.Deck.Where(card => !card.Upgraded).ToList();
                        if (candidates.Count == 0)
                        {
                            _run.Gold += 40;
                            ResolveEvent("命运已足够锋利", "她笑了笑，把金币推回你的脚边。");
                            return;
                        }

                        var selected = candidates[_rng.Next(candidates.Count)];
                        var index = _run.Deck.IndexOf(selected);
                        _run.Deck[index] = selected.Upgrade();
                        ResolveEvent("边角被磨亮", string.Format("「{0}」已经完成升级。", _run.Deck[index].DisplayName));
                    }),
                    EventOption("婉拒预言", "获得 12 金", () =>
                    {
                        _run.Gold += 12;
                        ResolveEvent("你的路自己写", "她把手边的一小袋金币递给你，说这算作见证费。");
                    })
                }));

            events.Add(Tuple.Create<string, string, List<EventChoice>>(
                "无主祭坛",
                "祭坛上没有神像，只有一个刚好能容下掌心的凹槽。周围的石壁刻满名字，最后一行却仍是空白。\n你可以留下自己的血，或留下自己的名字。",
                new List<EventChoice>
                {
                    EventOption("以血立誓", "最大生命 +8，并回复 8 生命", () =>
                    {
                        _run.MaxHp += 8;
                        _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + 8);
                        ResolveEvent("名字之下", "凹槽里的血没有凝固，它开始发出微弱的暖光。");
                    }),
                    EventOption("刻下名字", "失去 8 生命，获得一件遗物", () =>
                    {
                        _run.Hp = Mathf.Max(1, _run.Hp - 8);
                        AcquireRandomRelic();
                        ResolveEvent("祭坛记住了你", "石壁的空行上出现了一个新名字。你感到某件旧物正在口袋里发热。");
                    })
                }));

            return events;
        }

        private EventChoice EventOption(string label, string detail, Action apply)
        {
            return new EventChoice { Label = label, Detail = detail, Apply = apply };
        }

        private void ResolveEvent(string title, string body)
        {
            _busy = true;
            RefreshHeader();
            var modal = CreateModal(title, new Vector2(820f, 430f));
            EmberUI.Label(modal, body, 23, EmberUI.Parchment, TextAnchor.UpperLeft, new Vector2(0f, 30f), new Vector2(640f, 170f));
            EmberUI.Button(modal, "继续前行", new Vector2(0f, -145f), new Vector2(240f, 56f), new Color32(128, 75, 46, 255), () =>
            {
                CloseModal();
                AdvanceAfterNode();
            }, 19);
        }

        private void AdvanceAfterNode()
        {
            if (_run == null) return;
            _busy = false;
            _enemyTurn = false;
            _run.ChosenNode = -1;

            var act = _run.Map.Acts[_run.Act];
            if (_currentNodeKind == NodeKind.Boss)
            {
                ShowRunEnd(true);
                return;
            }

            if (_run.Row + 1 < act.Rows.Count)
            {
                _run.Row++;
                ShowMap();
                return;
            }

            if (_run.Act + 1 < _run.Map.Acts.Count)
            {
                var nextAct = _run.Act + 1;
                ShowActIntro(nextAct);
                return;
            }

            ShowRunEnd(true);
        }

        private void AcquireRandomRelic()
        {
            var available = Relics.Where(relic => !_run.Relics.Contains(relic.Id)).ToList();
            if (available.Count == 0)
            {
                _run.Gold += 30;
                return;
            }

            var relic = available[_rng.Next(available.Count)];
            _run.Relics.Add(relic.Id);
            _lastAcquiredRelicId = relic.Id;
            if (relic.Id == "wayfarer_compass")
            {
                _run.MaxHp += 10;
                _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + 10);
            }
        }

        private List<CardDef> BuildCardRewards(int count)
        {
            var pool = CardLibrary.CreateRewardPool()
                .GroupBy(card => card.Id)
                .Select(group => group.First())
                .ToList();
            var selected = new List<CardDef>();
            while (selected.Count < count && pool.Count > 0)
            {
                var totalWeight = 0f;
                foreach (var card in pool)
                {
                    totalWeight += RewardWeight(card, _run != null ? _run.Act : 0);
                }

                var roll = (float)_rng.NextDouble() * totalWeight;
                CardDef picked = pool[pool.Count - 1];
                foreach (var card in pool)
                {
                    roll -= RewardWeight(card, _run != null ? _run.Act : 0);
                    if (roll <= 0f)
                    {
                        picked = card;
                        break;
                    }
                }

                selected.Add(picked.Clone());
                pool.Remove(picked);
            }

            return selected;
        }

        private static float RewardWeight(CardDef card, int act)
        {
            switch (card.Rarity)
            {
                case CardRarity.Common:
                    return 1f;
                case CardRarity.Uncommon:
                    return 0.66f + act * 0.06f;
                case CardRarity.Rare:
                    return 0.13f + act * 0.055f;
                default:
                    return 0.2f;
            }
        }

        private void ShowRunEnd(bool victory)
        {
            ClearScreen();
            _busy = false;
            _enemyTurn = false;
            _combatOver = true;

            if (_run == null)
            {
                ShowTitleScreen();
                return;
            }

            var elapsed = Time.realtimeSinceStartup - _run.StartedAt;
            var glowColor = victory ? new Color(0.88f, 0.52f, 0.2f, 0.28f) : new Color(0.54f, 0.12f, 0.1f, 0.28f);
            var title = victory ? "余烬未熄" : "灯火熄灭";
            var subtitle = victory
                ? "你穿过了三幕黑暗，星路在你的脚下重新亮起。"
                : "这条路记住了你的名字。下一次，火会烧得更远。";

            EmberUI.Shape(_screenRoot, EmberUI.Glow, new Vector2(0f, 80f), new Vector2(1100f, 1100f), glowColor);
            EmberUI.Shape(_screenRoot, EmberUI.Ring, new Vector2(0f, 100f), new Vector2(580f, 580f), new Color(0.82f, 0.64f, 0.36f, 0.13f));
            if (victory)
            {
                CharacterVisual(_screenRoot, "enemy_demon", new Vector2(0f, 105f), new Vector2(310f, 310f), new Color(0.92f, 0.8f, 0.6f, 0.86f), 5f, 1.15f);
            }
            else
            {
                CharacterVisual(_screenRoot, "hero_ember", new Vector2(0f, 105f), new Vector2(300f, 300f), new Color(0.54f, 0.45f, 0.4f, 0.72f), 2f, 1.3f);
            }

            EmberUI.Label(_screenRoot, title, 76, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -140f), new Vector2(960f, 100f), FontStyle.Bold);
            EmberUI.Label(_screenRoot, subtitle, 23, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, -220f), new Vector2(900f, 58f));

            var stats = EmberUI.Panel(_screenRoot, new Vector2(0f, -340f), new Vector2(880f, 110f), new Color(0.07f, 0.085f, 0.07f, 0.94f));
            EmberUI.Label(stats.transform, string.Format("用时  {0}", FormatDuration(elapsed)), 20, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(-285f, 8f), new Vector2(210f, 34f), FontStyle.Bold);
            EmberUI.Label(stats.transform, string.Format("生命  {0}/{1}", _run.Hp, _run.MaxHp), 20, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(-75f, 8f), new Vector2(190f, 34f), FontStyle.Bold);
            EmberUI.Label(stats.transform, string.Format("金币  {0}", _run.Gold), 20, EmberUI.Gold, TextAnchor.MiddleCenter, new Vector2(130f, 8f), new Vector2(170f, 34f), FontStyle.Bold);
            EmberUI.Label(stats.transform, string.Format("牌组  {0} 张", _run.Deck.Count), 20, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(320f, 8f), new Vector2(170f, 34f), FontStyle.Bold);

            EmberUI.Button(_screenRoot, "再次点燃", new Vector2(-130f, -465f), new Vector2(230f, 58f), new Color32(151, 81, 47, 255), StartNewRun, 19);
            EmberUI.Button(_screenRoot, "返回主菜单", new Vector2(130f, -465f), new Vector2(230f, 58f), EmberUI.PanelLight, ShowTitleScreen, 19);
        }

        private static string FormatDuration(float seconds)
        {
            var total = Mathf.Max(1, Mathf.RoundToInt(seconds));
            return string.Format("{0:00}:{1:00}", total / 60, total % 60);
        }

        private static string ChineseNumber(int number)
        {
            switch (number)
            {
                case 1: return "一";
                case 2: return "二";
                case 3: return "三";
                case 4: return "四";
                default: return number.ToString();
            }
        }
    }
}
