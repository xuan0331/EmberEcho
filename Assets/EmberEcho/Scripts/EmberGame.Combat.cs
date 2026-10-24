using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EmberEcho
{
    public sealed partial class EmberGame
    {
        private bool _hunterMarkUsed;
        private bool _ironButterflyUsed;

        private void StartCombat(NodeKind kind)
        {
            ClearScreen();
            _inCombat = true;
            _currentNodeKind = kind;
            _busy = false;
            _enemyTurn = false;
            _combatOver = false;
            _hunterMarkUsed = false;
            _ironButterflyUsed = false;
            _lastAcquiredRelicId = null;
            _encounter = EnemyCatalog.BuildEncounter(kind, _run.Act, _rng);

            _enemies.Clear();
            foreach (var enemy in _encounter.Enemies)
            {
                _enemies.Add(EnemyCombatant.FromDef(enemy));
            }

            _selectedEnemyIndex = 0;
            _energy = 3;
            _playerBlock = 0;
            _playerStrength = 0;
            _playerDexterity = 0;
            _playerWeak = 0;
            _playerVulnerable = 0;
            _ember = _run.Relics.Contains("ember_lamp") ? 1 : 0;
            _turn = 1;
            _hand.Clear();
            _drawPile.Clear();
            _discardPile.Clear();
            _exhaustPile.Clear();
            _drawPile.AddRange(_run.Deck);
            Shuffle(_drawPile);
            _combatLog.Clear();

            AddLog(string.Format("你进入了「{0}」。", _encounter.Title));
            _audio.Play("shuffle", 0.32f, 0.95f);
            BuildBattleScreen();
            DrawCards(5);
            AddLog("第 1 回合：能量恢复，抽取 5 张牌。");
            RefreshCombatUI();
            StartCoroutine(CombatIntroRoutine());
        }

        private void BuildBattleScreen()
        {
            CreateHeader("交锋 · " + ChineseNumber(_run.Act + 1) + "幕", _encounter.Title);

            var stage = EmberUI.Center("BattleStage", _screenRoot, new Vector2(-40f, 25f), new Vector2(1760f, 820f));
            EmberUI.Shape(stage, EmberUI.Glow, new Vector2(80f, -110f), new Vector2(1100f, 450f), new Color(0.55f, 0.28f, 0.12f, 0.17f));
            EmberUI.Shape(stage, EmberUI.Ring, new Vector2(60f, -198f), new Vector2(980f, 210f), new Color(0.76f, 0.58f, 0.35f, 0.1f));
            EmberUI.Line(stage, new Vector2(-780f, -188f), new Vector2(610f, -188f), 2f, new Color(0.77f, 0.61f, 0.38f, 0.23f));

            _turnText = EmberUI.Label(stage, string.Empty, 16, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(-730f, 373f), new Vector2(280f, 30f), FontStyle.Bold);
            _targetText = EmberUI.Label(stage, string.Empty, 15, EmberUI.Muted, TextAnchor.MiddleRight, new Vector2(300f, 373f), new Vector2(430f, 30f));
            EmberUI.Line(stage, new Vector2(-790f, 350f), new Vector2(560f, 350f), 1f, new Color(0.76f, 0.58f, 0.35f, 0.18f));

            BuildPlayerAvatar(stage);
            BuildEnemyViews(stage);
            BuildBattleLog();
            BuildEnergyCounter();
            BuildPileCounters();

            _handRoot = EmberUI.Center("Hand", _screenRoot, new Vector2(-80f, -330f), new Vector2(1320f, 330f));

            _endTurnButton = EmberUI.Button(
                _screenRoot,
                "结束回合",
                new Vector2(760f, -335f),
                new Vector2(240f, 72f),
                new Color32(144, 79, 48, 255),
                EndPlayerTurn,
                22,
                EmberUI.Parchment);
            AddButtonDepth(_endTurnButton);

            EmberUI.Label(_screenRoot, "抽牌堆", 13, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(600f, -211f), new Vector2(100f, 22f));
            EmberUI.Label(_screenRoot, "弃牌堆", 13, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(710f, -211f), new Vector2(100f, 22f));
            _drawCountText = EmberUI.Label(_screenRoot, string.Empty, 24, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(600f, -238f), new Vector2(90f, 32f), FontStyle.Bold);
            _discardCountText = EmberUI.Label(_screenRoot, string.Empty, 24, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(710f, -238f), new Vector2(90f, 32f), FontStyle.Bold);

            var flashRect = EmberUI.Stretch("DamageFlash", _screenRoot);
            var flashImage = flashRect.gameObject.AddComponent<Image>();
            flashImage.color = new Color(0.55f, 0.03f, 0.02f, 0f);
            flashImage.raycastTarget = false;
            _damageFlash = flashRect.gameObject.AddComponent<CanvasGroup>();
            _damageFlash.alpha = 0f;
        }

        private void BuildPlayerAvatar(RectTransform stage)
        {
            var root = EmberUI.Center("PlayerAvatar", stage, new Vector2(-570f, 58f), new Vector2(380f, 500f));
            EmberUI.Shape(root, EmberUI.Glow, new Vector2(0f, 40f), new Vector2(360f, 360f), new Color(0.7f, 0.42f, 0.19f, 0.18f));
            EmberUI.Shape(root, EmberUI.Ring, new Vector2(0f, 40f), new Vector2(286f, 286f), new Color(0.8f, 0.61f, 0.35f, 0.18f));
            EmberUI.Shape(root, EmberUI.Circle, new Vector2(0f, -102f), new Vector2(250f, 44f), new Color(0f, 0f, 0f, 0.35f));
            var hero = CharacterVisual(root, "hero_ember", new Vector2(0f, 50f), new Vector2(220f, 220f), Color.white, 5f, 1.25f);
            hero.gameObject.AddComponent<FloatBob>().Distance = 7f;
            EmberUI.Label(root, "余烬行者", 25, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -142f), new Vector2(250f, 36f), FontStyle.Bold);
            _playerHpFill = EmberUI.Bar(root, new Vector2(0f, -184f), new Vector2(250f, 22f), new Color(0.13f, 0.07f, 0.06f, 1f), EmberUI.Blood, 1f);
            _playerHpText = EmberUI.Label(root, string.Empty, 15, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -184f), new Vector2(250f, 24f), FontStyle.Bold);
            _playerBlockText = EmberUI.Label(root, string.Empty, 18, EmberUI.PaleBlue, TextAnchor.MiddleCenter, new Vector2(0f, -220f), new Vector2(250f, 28f), FontStyle.Bold);
            _playerStatusText = EmberUI.Label(root, string.Empty, 14, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, -250f), new Vector2(300f, 28f));
        }

        private void BuildEnemyViews(RectTransform stage)
        {
            var positions = _enemies.Count == 1
                ? new[] { new Vector2(160f, 55f) }
                : new[] { new Vector2(35f, 55f), new Vector2(355f, 55f) };

            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                var root = EmberUI.Center("Enemy_" + enemy.Def.Name, stage, positions[i], new Vector2(292f, 500f));
                EmberUI.Shape(root, EmberUI.Glow, new Vector2(0f, 50f), new Vector2(330f, 330f), new Color(0.72f, 0.27f, 0.13f, 0.15f));
                var targetRing = EmberUI.Shape(root, EmberUI.Ring, new Vector2(0f, 45f), new Vector2(270f, 270f), new Color(0.86f, 0.57f, 0.27f, 0.32f));
                targetRing.transform.localScale = Vector3.one;
                var ringPulse = targetRing.gameObject.AddComponent<PulseLoop>();
                ringPulse.Amount = 0.026f;
                EmberUI.Shape(root, EmberUI.Circle, new Vector2(0f, -100f), new Vector2(220f, 38f), new Color(0f, 0f, 0f, 0.32f));

                var intentText = EmberUI.Label(root, string.Empty, 16, EmberUI.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 222f), new Vector2(280f, 64f), FontStyle.Bold);
                var intentPanel = EmberUI.Panel(root, new Vector2(0f, 218f), new Vector2(280f, 68f), new Color(0.055f, 0.065f, 0.055f, 0.9f));
                intentPanel.transform.SetAsFirstSibling();

                var artRect = EmberUI.Center("Art", root, new Vector2(0f, 44f), new Vector2(190f, 190f));
                var artFraming = enemy.Def.Id == "star_husk" ? 1.08f : 1.28f;
                var art = CharacterVisual(artRect, enemy.Def.Art, Vector2.zero, new Vector2(190f, 190f), Color.white, 5f, artFraming);
                art.raycastTarget = true;
                var artButton = artRect.gameObject.AddComponent<Button>();
                artButton.targetGraphic = art;
                artButton.transition = Selectable.Transition.None;
                var enemyIndex = i;
                artButton.onClick.AddListener(() => SelectEnemy(enemyIndex));
                artRect.gameObject.AddComponent<FloatBob>().Distance = 7f;

                var nameText = EmberUI.Label(root, enemy.Def.Name, 22, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -88f), new Vector2(280f, 32f), FontStyle.Bold);
                var hpFill = EmberUI.Bar(root, new Vector2(0f, -123f), new Vector2(238f, 22f), new Color(0.13f, 0.07f, 0.06f, 1f), EmberUI.Blood, 1f);
                var hpText = EmberUI.Label(root, string.Empty, 14, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -123f), new Vector2(238f, 24f), FontStyle.Bold);
                var blockText = EmberUI.Label(root, string.Empty, 17, EmberUI.PaleBlue, TextAnchor.MiddleCenter, new Vector2(0f, -157f), new Vector2(230f, 28f), FontStyle.Bold);
                var statusText = EmberUI.Label(root, string.Empty, 13, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, -187f), new Vector2(270f, 30f));

                var view = new EnemyView
                {
                    Data = enemy,
                    Root = root,
                    ArtRect = artRect,
                    Art = art,
                    HpFill = hpFill,
                    TargetRing = targetRing,
                    IntentText = intentText,
                    HpText = hpText,
                    BlockText = blockText,
                    StatusText = statusText,
                    NameText = nameText
                };
                _enemyViews.Add(view);
            }
        }

        private void BuildBattleLog()
        {
            var logPanel = EmberUI.Panel(_screenRoot, new Vector2(760f, 145f), new Vector2(320f, 570f), new Color(0.055f, 0.068f, 0.058f, 0.94f));
            EmberUI.Label(logPanel.transform, "战斗记录", 16, EmberUI.Gold, TextAnchor.MiddleLeft, new Vector2(-112f, 250f), new Vector2(170f, 28f), FontStyle.Bold);
            EmberUI.Line(logPanel.transform, new Vector2(-125f, 227f), new Vector2(125f, 227f), 1f, new Color(0.82f, 0.64f, 0.36f, 0.2f));
            _logText = EmberUI.Label(logPanel.transform, string.Empty, 15, EmberUI.Parchment, TextAnchor.UpperLeft, new Vector2(0f, 82f), new Vector2(260f, 255f));
            EmberUI.Label(logPanel.transform, "目标", 13, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-112f, -250f), new Vector2(80f, 24f), FontStyle.Bold);
            EmberUI.Label(logPanel.transform, "点击敌人可切换攻击目标", 13, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-18f, -250f), new Vector2(220f, 24f));
        }

        private void BuildEnergyCounter()
        {
            var root = EmberUI.Center("Energy", _screenRoot, new Vector2(-785f, -335f), new Vector2(190f, 190f));
            EmberUI.Shape(root, EmberUI.Glow, Vector2.zero, new Vector2(260f, 260f), new Color(0.89f, 0.48f, 0.17f, 0.22f));
            EmberUI.Shape(root, EmberUI.Ring, Vector2.zero, new Vector2(150f, 150f), new Color(0.9f, 0.63f, 0.33f, 0.45f));
            EmberUI.Shape(root, EmberUI.Circle, Vector2.zero, new Vector2(124f, 124f), new Color(0.07f, 0.08f, 0.07f, 0.96f));
            EmberUI.Label(root, "能量", 14, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, 31f), new Vector2(90f, 22f));
            _energyText = EmberUI.Label(root, string.Empty, 42, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, -4f), new Vector2(110f, 52f), FontStyle.Bold);
            _emberText = EmberUI.Label(root, string.Empty, 14, EmberUI.Ember, TextAnchor.MiddleCenter, new Vector2(0f, -48f), new Vector2(118f, 24f), FontStyle.Bold);
        }

        private void BuildPileCounters()
        {
            EmberUI.Picture(_screenRoot, "icon_urn", new Vector2(600f, -272f), new Vector2(44f, 44f), new Color(1f, 1f, 1f, 0.48f), true);
            EmberUI.Picture(_screenRoot, "icon_urn", new Vector2(710f, -272f), new Vector2(44f, 44f), new Color(1f, 1f, 1f, 0.32f), true);
        }

        private GameObject CreateCardView(
            CardDef card,
            Transform parent,
            Vector2 position,
            Vector2 size,
            bool interactive,
            System.Action onClick,
            bool showHover,
            float scale = 1f)
        {
            var root = EmberUI.Center("Card_" + card.Id, parent, position, size);
            var outer = root.gameObject.AddComponent<Image>();
            outer.sprite = EmberUI.Rounded;
            outer.type = Image.Type.Sliced;
            outer.color = Color.Lerp(EmberUI.CardColor(card.Kind), EmberUI.Ink, 0.34f);
            outer.raycastTarget = interactive;

            var shadow = root.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
            shadow.effectDistance = new Vector2(0f, -7f);

            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = outer;
            button.interactable = interactive;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = new ColorBlock
            {
                normalColor = Color.white,
                highlightedColor = new Color(1.11f, 1.1f, 1.05f, 1f),
                pressedColor = new Color(0.83f, 0.82f, 0.78f, 1f),
                selectedColor = Color.white,
                disabledColor = new Color(0.8f, 0.8f, 0.78f, 1f),
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            if (interactive && onClick != null) button.onClick.AddListener(() => onClick());

            var body = EmberUI.Stretch("Body", root, 4f, 4f, 4f, 4f);
            var bodyImage = body.gameObject.AddComponent<Image>();
            bodyImage.sprite = EmberUI.Rounded;
            bodyImage.type = Image.Type.Sliced;
            bodyImage.color = new Color(0.075f, 0.085f, 0.075f, 1f);
            bodyImage.raycastTarget = false;

            var topBand = EmberUI.Center("TopBand", body, new Vector2(0f, 118f), new Vector2(196f, 42f));
            var topImage = topBand.gameObject.AddComponent<Image>();
            topImage.sprite = EmberUI.Rounded;
            topImage.type = Image.Type.Sliced;
            topImage.color = EmberUI.WithAlpha(EmberUI.CardColor(card.Kind), 0.54f);
            topImage.raycastTarget = false;

            var costCircle = EmberUI.Shape(body, EmberUI.Circle, new Vector2(-80f, 116f), new Vector2(42f, 42f), new Color(0.07f, 0.075f, 0.065f, 1f));
            costCircle.raycastTarget = false;
            EmberUI.Shape(body, EmberUI.Ring, new Vector2(-80f, 116f), new Vector2(44f, 44f), EmberUI.RarityColor(card.Rarity));
            EmberUI.Label(body, card.Cost.ToString(), 20, card.Cost == 0 ? EmberUI.Gold : EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(-80f, 116f), new Vector2(38f, 32f), FontStyle.Bold);

            var title = EmberUI.Label(body, card.DisplayName, 19, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(18f, 116f), new Vector2(142f, 36f), FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            EmberUI.Shape(body, EmberUI.Diamond, new Vector2(74f, 118f), new Vector2(15f, 15f), EmberUI.RarityColor(card.Rarity));

            var artBack = EmberUI.Panel(body, new Vector2(0f, 31f), new Vector2(190f, 106f), new Color(0.085f, 0.095f, 0.08f, 0.96f));
            EmberUI.Shape(
                artBack.transform,
                EmberUI.Glow,
                Vector2.zero,
                new Vector2(150f, 150f),
                EmberUI.WithAlpha(EmberUI.CardColor(card.Kind), 0.28f));
            EmberUI.Line(body, new Vector2(-95f, -26f), new Vector2(95f, -26f), 1f, EmberUI.WithAlpha(EmberUI.RarityColor(card.Rarity), 0.4f));
            EmberUI.Picture(body, card.IconKey, new Vector2(0f, 32f), new Vector2(91f, 91f), Color.white, true);

            var description = EmberUI.Label(
                body,
                card.Description,
                14,
                EmberUI.Parchment,
                TextAnchor.UpperCenter,
                new Vector2(0f, -75f),
                new Vector2(184f, 84f));
            description.lineSpacing = 1.05f;
            description.resizeTextForBestFit = true;
            description.resizeTextMinSize = 11;
            description.resizeTextMaxSize = 14;

            EmberUI.Label(body, KindLabel(card.Kind), 13, EmberUI.Muted, TextAnchor.MiddleLeft, new Vector2(-68f, -127f), new Vector2(78f, 22f));
            EmberUI.Label(body, CardLibrary.RarityLabel(card.Rarity), 12, EmberUI.RarityColor(card.Rarity), TextAnchor.MiddleRight, new Vector2(66f, -127f), new Vector2(80f, 22f));

            if (showHover)
            {
                var hover = root.gameObject.AddComponent<CardHover>();
                hover.Configure(position, 0f, interactive, scale);
            }
            else
            {
                root.localScale = Vector3.one * scale;
            }

            if (interactive && _inCombat && _energy < card.Cost)
            {
                outer.color = new Color(0.25f, 0.25f, 0.23f, 1f);
                bodyImage.color = new Color(0.11f, 0.11f, 0.1f, 1f);
            }

            return root.gameObject;
        }

        private static string KindLabel(CardKind kind)
        {
            switch (kind)
            {
                case CardKind.Attack: return "攻击";
                case CardKind.Skill: return "技能";
                case CardKind.Power: return "能力";
                default: return "卡牌";
            }
        }

        private void RefreshCombatUI()
        {
            if (_run == null) return;
            RefreshHeader();

            if (_playerHpText != null) _playerHpText.text = string.Format("{0} / {1}", _run.Hp, _run.MaxHp);
            if (_playerHpFill != null) SetBarFill(_playerHpFill, _run.Hp / (float)_run.MaxHp);
            if (_playerBlockText != null) _playerBlockText.text = _playerBlock > 0 ? string.Format("护甲  {0}", _playerBlock) : "护甲  0";
            if (_playerStatusText != null)
            {
                var statuses = new List<string>();
                if (_playerStrength > 0) statuses.Add("力量 +" + _playerStrength);
                if (_playerDexterity > 0) statuses.Add("灵巧 +" + _playerDexterity);
                if (_playerWeak > 0) statuses.Add("虚弱 " + _playerWeak);
                if (_playerVulnerable > 0) statuses.Add("易伤 " + _playerVulnerable);
                _playerStatusText.text = statuses.Count == 0 ? "状态稳定" : string.Join("   ", statuses);
            }

            if (_energyText != null) _energyText.text = _energy + " / 3";
            if (_emberText != null) _emberText.text = "余烬 " + _ember + " / 3";
            if (_turnText != null) _turnText.text = string.Format("第 {0} 回合   ·   {1}", _turn, _enemyTurn ? "敌方行动" : "你的行动");
            if (_drawCountText != null) _drawCountText.text = _drawPile.Count.ToString();
            if (_discardCountText != null) _discardCountText.text = _discardPile.Count.ToString();
            if (_targetText != null)
            {
                var selected = SelectedEnemy();
                _targetText.text = selected == null
                    ? "尚未锁定目标"
                    : string.Format("目标 · {0}   {1}/{2}", selected.Def.Name, selected.Hp, selected.Def.MaxHp);
            }

            foreach (var view in _enemyViews)
            {
                var data = view.Data;
                var selected = !data.IsDead && _enemies.IndexOf(data) == _selectedEnemyIndex;
                view.TargetRing.color = data.IsDead
                    ? new Color(0.3f, 0.3f, 0.28f, 0.08f)
                    : selected
                        ? new Color(0.96f, 0.59f, 0.26f, 0.56f)
                        : new Color(0.55f, 0.42f, 0.28f, 0.12f);
                view.Art.color = data.IsDead ? new Color(0.44f, 0.43f, 0.4f, 0.28f) : Color.white;
                view.IntentText.color = data.IsDead ? EmberUI.Muted : EmberUI.Gold;
                view.IntentText.text = data.IsDead ? string.Empty : IntentText(data);
                view.HpText.text = string.Format("{0} / {1}", Mathf.Max(0, data.Hp), data.Def.MaxHp);
                SetBarFill(view.HpFill, data.Hp / (float)data.Def.MaxHp);
                view.BlockText.text = data.Block > 0 ? string.Format("护甲  {0}", data.Block) : string.Empty;
                var statuses = new List<string>();
                if (data.Strength > 0) statuses.Add("力量 +" + data.Strength);
                if (data.Weak > 0) statuses.Add("虚弱 " + data.Weak);
                if (data.Vulnerable > 0) statuses.Add("易伤 " + data.Vulnerable);
                if (data.Burn > 0) statuses.Add("灼烧 " + data.Burn);
                view.StatusText.text = data.IsDead ? "已消散" : string.Join("  ", statuses);
                view.NameText.text = data.IsDead ? data.Def.Name + " · 消散" : data.Def.Name;
                var artButton = view.ArtRect.GetComponent<Button>();
                if (artButton != null) artButton.interactable = !data.IsDead;
            }

            if (_endTurnButton != null)
            {
                _endTurnButton.interactable = !_enemyTurn && !_combatOver && !_busy;
                var endLabel = _endTurnButton.GetComponentInChildren<Text>();
                if (endLabel != null) endLabel.text = _enemyTurn ? "敌人行动中" : "结束回合";
            }

            if (_logText != null)
            {
                var visible = _combatLog.Count > 10 ? _combatLog.Skip(_combatLog.Count - 10).ToList() : _combatLog;
                _logText.text = string.Join("\n\n", visible);
            }

            BuildHand();
        }

        private void BuildHand()
        {
            if (_handRoot == null) return;
            EmberUI.Clear(_handRoot);
            var count = _hand.Count;
            if (count == 0) return;

            var spacing = count <= 1 ? 0f : Mathf.Min(205f, 1210f / (count - 1));
            for (var i = 0; i < count; i++)
            {
                var offset = (i - (count - 1) * 0.5f) * spacing;
                var angle = -offset / 135f;
                var card = _hand[i];
                var canPlay = !_enemyTurn && !_busy && !_combatOver && _energy >= card.Cost;
                var index = i;
                CreateCardView(
                    card,
                    _handRoot,
                    new Vector2(offset, Mathf.Abs(offset) * 0.012f),
                    new Vector2(218f, 300f),
                    canPlay,
                    () => PlayCard(_hand[index]),
                    true);
            }
        }

        private static void SetBarFill(Image fill, float ratio)
        {
            if (fill == null) return;
            var rect = fill.rectTransform;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            rect.offsetMax = new Vector2(0f, -3f);
            rect.offsetMin = new Vector2(3f, 3f);
        }

        private string IntentText(EnemyCombatant enemy)
        {
            var move = enemy.CurrentMove;
            var parts = new List<string>();
            if (move.Damage > 0)
            {
                var damage = move.Damage + enemy.Strength;
                if (enemy.Weak > 0) damage = Mathf.FloorToInt(damage * 0.75f);
                parts.Add(move.Hits > 1 ? string.Format("攻击 {0}×{1}", damage, move.Hits) : string.Format("攻击 {0}", damage));
            }
            if (move.Block > 0) parts.Add("护甲 " + move.Block);
            if (move.Strength > 0) parts.Add("力量 +" + move.Strength);
            if (move.PlayerWeak > 0) parts.Add("施加虚弱");
            if (move.PlayerVulnerable > 0) parts.Add("施加易伤");
            if (move.Heal > 0) parts.Add("回复 " + move.Heal);
            return move.Name + "\n" + string.Join(" · ", parts);
        }

        private void SelectEnemy(int index)
        {
            if (_enemyTurn || _combatOver || index < 0 || index >= _enemies.Count || _enemies[index].IsDead) return;
            _selectedEnemyIndex = index;
            RefreshCombatUI();
        }

        private EnemyCombatant SelectedEnemy()
        {
            if (_selectedEnemyIndex < 0 || _selectedEnemyIndex >= _enemies.Count) return null;
            var selected = _enemies[_selectedEnemyIndex];
            if (selected != null && !selected.IsDead) return selected;
            SelectFirstLivingEnemy();
            return _selectedEnemyIndex >= 0 && _selectedEnemyIndex < _enemies.Count ? _enemies[_selectedEnemyIndex] : null;
        }

        private void SelectFirstLivingEnemy()
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].IsDead) continue;
                _selectedEnemyIndex = i;
                return;
            }
            _selectedEnemyIndex = -1;
        }

        private void PlayCard(CardDef card)
        {
            if (card == null || _enemyTurn || _busy || _combatOver) return;
            if (!_hand.Contains(card) || card.Cost > _energy) return;

            _hand.Remove(card);
            _energy -= card.Cost;
            _audio.Play("card_play_" + (_rng.Next(2) + 1), 0.34f, UnityEngine.Random.Range(0.94f, 1.05f));

            var resonance = false;
            if (card.Kind == CardKind.Attack)
            {
                if (_ember >= 3)
                {
                    _ember = 0;
                    resonance = true;
                }
                else
                {
                    _ember++;
                }
            }

            var bonus = resonance ? 4 : 0;
            if (card.Kind == CardKind.Attack && !_hunterMarkUsed && _run.Relics.Contains("hunter_mark"))
            {
                bonus += 5;
                _hunterMarkUsed = true;
            }

            AddLog(string.Format("你打出「{0}」。", card.DisplayName));
            if (resonance) AddLog("余烬共鸣爆发，额外伤害 +4。");
            if (bonus > 4 && _hunterMarkUsed) AddLog("猎手刻印被唤醒，额外伤害 +5。");

            if (card.Block > 0)
            {
                var block = card.Block + _playerDexterity;
                _playerBlock += block;
                AddLog(string.Format("获得 {0} 点护甲。", block));
            }
            if (card.Heal > 0)
            {
                _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + card.Heal);
                AddLog(string.Format("回复 {0} 点生命。", card.Heal));
            }
            if (card.Strength > 0)
            {
                _playerStrength += card.Strength;
                AddLog(string.Format("力量提高 {0}。", card.Strength));
            }
            if (card.Dexterity > 0)
            {
                _playerDexterity += card.Dexterity;
                AddLog(string.Format("灵巧提高 {0}。", card.Dexterity));
            }
            if (card.Energy > 0)
            {
                _energy += card.Energy;
                AddLog(string.Format("获得 {0} 点能量。", card.Energy));
            }
            if (card.Draw > 0)
            {
                DrawCards(card.Draw);
                AddLog(string.Format("抽取 {0} 张牌。", card.Draw));
            }

            var selected = SelectedEnemy();
            var bonusUsed = false;
            if (card.Damage > 0)
            {
                if (card.DamageAll)
                {
                    for (var enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
                    {
                        var enemy = _enemies[enemyIndex];
                        if (enemy.IsDead) continue;
                        for (var hit = 0; hit < card.Hits; hit++)
                        {
                            var hitBonus = !bonusUsed ? bonus : 0;
                            bonusUsed = true;
                            DealDamageToEnemy(enemy, card.Damage, hitBonus);
                            if (enemy.IsDead) break;
                        }
                        if (enemy.IsDead) continue;
                    }
                }
                else if (selected != null)
                {
                    for (var hit = 0; hit < card.Hits; hit++)
                    {
                        if (selected.IsDead) break;
                        var hitBonus = !bonusUsed ? bonus : 0;
                        bonusUsed = true;
                        DealDamageToEnemy(selected, card.Damage, hitBonus);
                    }
                }
            }

            var statusTarget = card.DamageAll ? null : selected;
            if (statusTarget != null && !statusTarget.IsDead)
            {
                if (card.Weak > 0)
                {
                    statusTarget.Weak += card.Weak;
                    AddLog(string.Format("{0}获得 {1} 层虚弱。", statusTarget.Def.Name, card.Weak));
                }
                if (card.Vulnerable > 0)
                {
                    statusTarget.Vulnerable += card.Vulnerable;
                    AddLog(string.Format("{0}获得 {1} 层易伤。", statusTarget.Def.Name, card.Vulnerable));
                }
                if (card.Burn > 0)
                {
                    statusTarget.Burn += card.Burn;
                    AddLog(string.Format("{0}获得 {1} 层灼烧。", statusTarget.Def.Name, card.Burn));
                }
            }

            if (card.Exhaust)
            {
                _exhaustPile.Add(card);
                AddLog("卡牌被消耗。");
            }
            else
            {
                _discardPile.Add(card);
            }

            if (_enemies.All(enemy => enemy.IsDead))
            {
                _combatOver = true;
                StartCoroutine(WinCombatAfterDelay());
            }
            else
            {
                SelectFirstLivingEnemy();
            }

            RefreshCombatUI();
        }

        private void DealDamageToEnemy(EnemyCombatant enemy, int baseDamage, int bonusDamage)
        {
            if (enemy == null || enemy.IsDead) return;
            var damage = baseDamage + bonusDamage + _playerStrength;
            if (_playerWeak > 0) damage = Mathf.FloorToInt(damage * 0.75f);
            if (enemy.Vulnerable > 0) damage = Mathf.FloorToInt(damage * 1.25f);
            damage = Mathf.Max(0, damage);
            var blocked = Mathf.Min(enemy.Block, damage);
            enemy.Block -= blocked;
            var healthDamage = damage - blocked;
            enemy.Hp = Mathf.Max(0, enemy.Hp - healthDamage);

            if (blocked > 0) AddLog(string.Format("{0}的护甲抵挡了 {1} 点伤害。", enemy.Def.Name, blocked));
            if (healthDamage > 0) AddLog(string.Format("{0}受到 {1} 点伤害。", enemy.Def.Name, healthDamage));
            ShowFloatingText(enemy, healthDamage.ToString(), healthDamage > 0 ? EmberUI.Parchment : EmberUI.PaleBlue);
            ShakeEnemy(enemy);
            _audio.Play("hit_" + (_rng.Next(2) + 1), 0.35f, UnityEngine.Random.Range(0.88f, 1.12f));
            if (enemy.IsDead) AddLog(string.Format("{0}消散了。", enemy.Def.Name));
        }

        private void ShakeEnemy(EnemyCombatant enemy)
        {
            var view = _enemyViews.FirstOrDefault(item => item.Data == enemy);
            if (view == null) return;
            StartCoroutine(ShakeRoutine(view.ArtRect, 12f, 0.22f));
        }

        private IEnumerator ShakeRoutine(RectTransform rect, float strength, float duration)
        {
            var basePosition = rect.anchoredPosition;
            var timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                var falloff = 1f - timer / duration;
                rect.anchoredPosition = basePosition + new Vector2(
                    Mathf.Sin(timer * 96f) * strength * falloff,
                    Mathf.Cos(timer * 73f) * strength * 0.35f * falloff);
                yield return null;
            }
            rect.anchoredPosition = basePosition;
        }

        private void ShowFloatingText(EnemyCombatant enemy, string text, Color color)
        {
            var view = _enemyViews.FirstOrDefault(item => item.Data == enemy);
            if (view == null) return;
            var label = EmberUI.Label(view.Root, text, 31, color, TextAnchor.MiddleCenter, new Vector2(0f, 95f), new Vector2(130f, 50f), FontStyle.Bold);
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(1f, -2f);
            StartCoroutine(FloatLabelRoutine(label));
        }

        private IEnumerator FloatLabelRoutine(Text label)
        {
            var rect = label.rectTransform;
            var basePosition = rect.anchoredPosition;
            var color = label.color;
            var timer = 0f;
            const float duration = 0.8f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(timer / duration);
                rect.anchoredPosition = basePosition + Vector2.up * (55f * EmberUI.EaseOutCubic(t));
                color.a = 1f - t;
                label.color = color;
                yield return null;
            }
            Destroy(label.gameObject);
        }

        private void EndPlayerTurn()
        {
            if (_enemyTurn || _combatOver || _busy) return;
            if (_turnRoutine != null) return;
            _enemyTurn = true;
            foreach (var card in _hand)
            {
                _discardPile.Add(card);
            }
            _hand.Clear();
            _playerWeak = Mathf.Max(0, _playerWeak - 1);
            foreach (var enemy in _enemies)
            {
                enemy.Vulnerable = Mathf.Max(0, enemy.Vulnerable - 1);
            }
            AddLog("你结束了回合。");
            RefreshCombatUI();
            _turnRoutine = StartCoroutine(EnemyTurnRoutine());
        }

        private IEnumerator EnemyTurnRoutine()
        {
            yield return new WaitForSecondsRealtime(0.35f);
            var vulnerableAppliedThisTurn = false;
            for (var enemyIndex = 0; enemyIndex < _enemies.Count; enemyIndex++)
            {
                var enemy = _enemies[enemyIndex];
                if (enemy.IsDead) continue;

                enemy.Block = 0;
                if (enemy.Burn > 0)
                {
                    var burnDamage = enemy.Burn;
                    enemy.Hp = Mathf.Max(0, enemy.Hp - burnDamage);
                    enemy.Burn--;
                    AddLog(string.Format("灼烧灼伤了{0}，造成 {1} 点伤害。", enemy.Def.Name, burnDamage));
                    ShowFloatingText(enemy, burnDamage.ToString(), EmberUI.Ember);
                    RefreshCombatUI();
                    yield return new WaitForSecondsRealtime(0.3f);
                    if (enemy.IsDead)
                    {
                        AddLog(string.Format("{0}消散了。", enemy.Def.Name));
                        if (_enemies.All(item => item.IsDead))
                        {
                            _combatOver = true;
                            _enemyTurn = false;
                            yield return WinCombatAfterDelay();
                            yield break;
                        }
                        continue;
                    }
                }

                var move = enemy.CurrentMove;
                AddLog(string.Format("{0}使用「{1}」。", enemy.Def.Name, move.Name));
                if (move.Block > 0)
                {
                    enemy.Block += move.Block;
                    AddLog(string.Format("{0}获得 {1} 点护甲。", enemy.Def.Name, move.Block));
                }
                if (move.Strength > 0)
                {
                    enemy.Strength += move.Strength;
                    AddLog(string.Format("{0}的力量提高 {1}。", enemy.Def.Name, move.Strength));
                }
                if (move.Heal > 0)
                {
                    enemy.Hp = Mathf.Min(enemy.Def.MaxHp, enemy.Hp + move.Heal);
                    AddLog(string.Format("{0}回复了 {1} 点生命。", enemy.Def.Name, move.Heal));
                }
                if (move.Damage > 0)
                {
                    for (var hit = 0; hit < move.Hits; hit++)
                    {
                        DamagePlayer(move.Damage, enemy);
                        if (_run.Hp <= 0) break;
                        yield return new WaitForSecondsRealtime(0.18f);
                    }
                }
                if (move.PlayerWeak > 0)
                {
                    _playerWeak += move.PlayerWeak;
                    AddLog(string.Format("你获得 {0} 层虚弱。", move.PlayerWeak));
                }
                if (move.PlayerVulnerable > 0)
                {
                    _playerVulnerable += move.PlayerVulnerable;
                    vulnerableAppliedThisTurn = true;
                    AddLog(string.Format("你获得 {0} 层易伤。", move.PlayerVulnerable));
                }
                enemy.PatternIndex = (enemy.PatternIndex + 1) % enemy.Def.Pattern.Length;

                if (_run.Hp <= 0)
                {
                    _enemyTurn = false;
                    _combatOver = true;
                    AddLog("你的生命降到了 0。");
                    RefreshCombatUI();
                    yield return new WaitForSecondsRealtime(0.8f);
                    ShowRunEnd(false);
                    yield break;
                }

                if (_enemies.All(item => item.IsDead))
                {
                    _combatOver = true;
                    yield return WinCombatAfterDelay();
                    yield break;
                }

                RefreshCombatUI();
                yield return new WaitForSecondsRealtime(0.5f);
            }

            foreach (var enemy in _enemies)
            {
                enemy.Weak = Mathf.Max(0, enemy.Weak - 1);
            }
            if (!vulnerableAppliedThisTurn)
            {
                _playerVulnerable = Mathf.Max(0, _playerVulnerable - 1);
            }
            _playerBlock = 0;
            _energy = 3;
            _turn++;
            DrawCards(5);
            AddLog(string.Format("第 {0} 回合：能量恢复，抽取 5 张牌。", _turn));
            _enemyTurn = false;
            _turnRoutine = null;
            RefreshCombatUI();
        }

        private void DamagePlayer(int baseDamage, EnemyCombatant source)
        {
            var damage = baseDamage + source.Strength;
            if (source.Weak > 0) damage = Mathf.FloorToInt(damage * 0.75f);
            if (_playerVulnerable > 0) damage = Mathf.FloorToInt(damage * 1.25f);
            damage = Mathf.Max(0, damage);
            var blocked = Mathf.Min(_playerBlock, damage);
            _playerBlock -= blocked;
            var healthDamage = damage - blocked;
            _run.Hp = Mathf.Max(0, _run.Hp - healthDamage);
            if (blocked > 0) AddLog(string.Format("护甲抵挡了 {0} 点伤害。", blocked));
            if (healthDamage > 0)
            {
                AddLog(string.Format("{0}造成 {1} 点伤害。", source.Def.Name, healthDamage));
                StartCoroutine(DamageFlashRoutine());
            }
            else
            {
                _audio.Play("hit_1", 0.22f, 0.7f);
            }

            if (_run.Hp <= _run.MaxHp / 2 && !_ironButterflyUsed && _run.Relics.Contains("iron_butterfly"))
            {
                _ironButterflyUsed = true;
                _playerBlock += 10;
                AddLog("铁蝶振翅，获得 10 点护甲。");
            }
            RefreshCombatUI();
        }

        private IEnumerator DamageFlashRoutine()
        {
            if (_damageFlash == null) yield break;
            var timer = 0f;
            const float duration = 0.3f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(timer / duration);
                _damageFlash.alpha = Mathf.Sin(t * Mathf.PI) * 0.14f;
                yield return null;
            }
            _damageFlash.alpha = 0f;
        }

        private void DrawCards(int count)
        {
            for (var i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0) break;
                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile);
                    AddLog("弃牌堆重新洗入抽牌堆。");
                    _audio.Play("shuffle", 0.22f, 1.06f);
                }

                var index = _drawPile.Count - 1;
                _hand.Add(_drawPile[index]);
                _drawPile.RemoveAt(index);
            }
        }

        private void Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = _rng.Next(i + 1);
                var item = list[i];
                list[i] = list[j];
                list[j] = item;
            }
        }

        private void AddLog(string text)
        {
            _combatLog.Add(text);
            if (_combatLog.Count > 40) _combatLog.RemoveAt(0);
        }

        private IEnumerator CombatIntroRoutine()
        {
            var panel = EmberUI.Panel(_screenRoot, new Vector2(0f, 120f), new Vector2(540f, 96f), new Color(0.04f, 0.05f, 0.04f, 0.92f));
            var group = panel.gameObject.AddComponent<CanvasGroup>();
            EmberUI.Label(panel.transform, _encounter.Title, 28, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, 8f), new Vector2(470f, 40f), FontStyle.Bold);
            EmberUI.Label(panel.transform, "交锋开始", 14, EmberUI.Gold, TextAnchor.MiddleCenter, new Vector2(0f, -27f), new Vector2(300f, 22f));
            var rect = panel.rectTransform;
            var timer = 0f;
            const float duration = 1.2f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(timer / duration);
                group.alpha = t < 0.15f ? t / 0.15f : 1f - Mathf.Clamp01((t - 0.72f) / 0.28f);
                rect.anchoredPosition = new Vector2(0f, 120f + Mathf.Lerp(0f, 34f, EmberUI.EaseOutCubic(t)));
                yield return null;
            }
            Destroy(panel.gameObject);
        }

        private IEnumerator WinCombatAfterDelay()
        {
            yield return new WaitForSecondsRealtime(0.65f);
            var gold = _successfulMovesThisCombat();
            _run.Gold += gold;
            if (_run.Relics.Contains("blood_fang"))
            {
                _run.Hp = Mathf.Min(_run.MaxHp, _run.Hp + 3);
            }

            if (_currentNodeKind == NodeKind.Boss)
            {
                ShowRunEnd(true);
                yield break;
            }

            if (_currentNodeKind == NodeKind.Elite)
            {
                AcquireRandomRelic();
            }

            ShowCombatReward(gold);
        }

        private int _successfulMovesThisCombat()
        {
            switch (_currentNodeKind)
            {
                case NodeKind.Elite:
                    return 65 + _run.Act * 10;
                case NodeKind.Boss:
                    return 0;
                default:
                    return 30 + _run.Act * 8;
            }
        }

        private void ShowCombatReward(int gold)
        {
            ClearScreen();
            _combatOver = true;
            _busy = false;
            CreateHeader("战斗结束", "余烬战利品");

            var stage = EmberUI.Center("Reward", _screenRoot, new Vector2(0f, -20f), new Vector2(1540f, 790f));
            EmberUI.Shape(stage, EmberUI.Glow, new Vector2(0f, 80f), new Vector2(850f, 850f), new Color(0.85f, 0.48f, 0.17f, 0.17f));
            EmberUI.Label(stage, "战斗结束", 48, EmberUI.Parchment, TextAnchor.MiddleCenter, new Vector2(0f, 330f), new Vector2(800f, 60f), FontStyle.Bold);
            EmberUI.Label(stage, string.Format("获得 {0} 金币   ·   选择一张牌加入牌组", gold), 20, EmberUI.Muted, TextAnchor.MiddleCenter, new Vector2(0f, 280f), new Vector2(900f, 38f));
            if (_currentNodeKind == NodeKind.Elite)
            {
                var relic = Relics.FirstOrDefault(item => item.Id == _lastAcquiredRelicId);
                if (relic != null)
                {
                    EmberUI.Label(stage, "遗物 · " + relic.Name + "   " + relic.Description, 17, EmberUI.Gold, TextAnchor.MiddleCenter, new Vector2(0f, 240f), new Vector2(1000f, 32f));
                }
            }

            var cards = BuildCardRewards(3);
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var position = new Vector2(-330f + i * 330f, 0f);
                CreateCardView(card, stage, position, new Vector2(218f, 300f), true, () =>
                {
                    if (_busy) return;
                    _busy = true;
                    _run.Deck.Add(card.Clone());
                    _audio.Play("card_play_2", 0.35f, 1.04f);
                    AdvanceAfterNode();
                }, true, 1.1f);
            }

            EmberUI.Button(stage, "跳过奖励", new Vector2(0f, -265f), new Vector2(210f, 52f), EmberUI.PanelLight, () =>
            {
                if (_busy) return;
                _busy = true;
                AdvanceAfterNode();
            }, 17);
        }
    }
}
