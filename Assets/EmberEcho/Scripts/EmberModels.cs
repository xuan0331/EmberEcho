using System;
using System.Collections.Generic;
using System.Linq;

namespace EmberEcho
{
    public enum CardKind
    {
        Attack,
        Skill,
        Power
    }

    public enum CardRarity
    {
        Starter,
        Common,
        Uncommon,
        Rare
    }

    public enum NodeKind
    {
        Combat,
        Elite,
        Rest,
        Shop,
        Event,
        Boss
    }

    [Serializable]
    public class CardDef
    {
        public string Id;
        public string Name;
        public string Flavor;
        public CardKind Kind;
        public CardRarity Rarity;
        public int Cost;
        public int Damage;
        public int Hits = 1;
        public int Block;
        public int Draw;
        public int Heal;
        public int Weak;
        public int Vulnerable;
        public int Strength;
        public int Dexterity;
        public int Burn;
        public int Energy;
        public bool DamageAll;
        public bool Exhaust;
        public bool Upgraded;

        public string DisplayName
        {
            get { return Upgraded ? Name + "+" : Name; }
        }

        public string IconKey
        {
            get
            {
                switch (Id)
                {
                    case "strike":
                    case "twin_cut":
                    case "guard_break":
                    case "flame_strike":
                    case "shield_bash":
                    case "echo":
                        return "icon_sword";
                    case "defend":
                    case "counter":
                    case "ember_armor":
                        return "icon_shield";
                    case "ember_step":
                    case "meditate":
                        return "icon_rune";
                    case "spark":
                    case "meteor":
                    case "cinder_brand":
                    case "hush":
                        return "icon_fire";
                    case "drain":
                    case "mend":
                        return "icon_potion";
                    case "burning_will":
                        return "icon_urn";
                    default:
                        return Kind == CardKind.Attack ? "icon_sword" : "icon_rune";
                }
            }
        }

        public string Description
        {
            get
            {
                var parts = new List<string>();
                if (Damage > 0)
                {
                    if (DamageAll)
                    {
                        parts.Add(Hits > 1
                            ? string.Format("对所有敌人造成 {0} 点伤害 {1} 次。", Damage, Hits)
                            : string.Format("对所有敌人造成 {0} 点伤害。", Damage));
                    }
                    else
                    {
                        parts.Add(Hits > 1
                            ? string.Format("造成 {0} 点伤害 {1} 次。", Damage, Hits)
                            : string.Format("造成 {0} 点伤害。", Damage));
                    }
                }

                if (Block > 0) parts.Add(string.Format("获得 {0} 点护甲。", Block));
                if (Draw > 0) parts.Add(string.Format("抽 {0} 张牌。", Draw));
                if (Heal > 0) parts.Add(string.Format("回复 {0} 点生命。", Heal));
                if (Energy > 0) parts.Add(string.Format("获得 {0} 点能量。", Energy));
                if (Strength > 0) parts.Add(string.Format("获得 {0} 点力量。", Strength));
                if (Dexterity > 0) parts.Add(string.Format("获得 {0} 点灵巧。", Dexterity));
                if (Weak > 0) parts.Add(string.Format("施加 {0} 层虚弱。", Weak));
                if (Vulnerable > 0) parts.Add(string.Format("施加 {0} 层易伤。", Vulnerable));
                if (Burn > 0) parts.Add(string.Format("施加 {0} 层灼烧。", Burn));
                if (Exhaust) parts.Add("消耗。");
                return string.Join(" ", parts);
            }
        }

        public CardDef Clone()
        {
            return (CardDef)MemberwiseClone();
        }

        public CardDef Upgrade()
        {
            var card = Clone();
            if (card.Upgraded)
            {
                return card;
            }

            card.Upgraded = true;
            switch (card.Id)
            {
                case "strike":
                    card.Damage = 9;
                    break;
                case "defend":
                    card.Block = 8;
                    break;
                case "ember_step":
                    card.Block = 6;
                    card.Draw = 2;
                    break;
                case "drain":
                    card.Damage = 8;
                    card.Heal = 3;
                    break;
                case "twin_cut":
                    card.Damage = 5;
                    break;
                case "guard_break":
                    card.Damage = 10;
                    card.Vulnerable = 3;
                    break;
                case "hush":
                    card.Damage = 6;
                    card.Weak = 3;
                    break;
                case "meditate":
                    card.Draw = 3;
                    break;
                case "spark":
                    card.Damage = 8;
                    break;
                case "counter":
                    card.Block = 11;
                    card.Strength = 2;
                    break;
                case "flame_strike":
                    card.Damage = 21;
                    break;
                case "shield_bash":
                    card.Damage = 10;
                    card.Block = 10;
                    break;
                case "mend":
                    card.Heal = 12;
                    break;
                case "meteor":
                    card.Damage = 20;
                    break;
                case "burning_will":
                    card.Strength = 3;
                    break;
                case "ember_armor":
                    card.Block = 13;
                    card.Dexterity = 2;
                    break;
                case "cinder_brand":
                    card.Damage = 7;
                    card.Burn = 3;
                    break;
                case "echo":
                    card.Damage = 4;
                    break;
            }

            return card;
        }
    }

    public static class CardLibrary
    {
        public static List<CardDef> CreateStartingDeck()
        {
            var deck = new List<CardDef>();
            for (var i = 0; i < 4; i++) deck.Add(GetBase("strike"));
            for (var i = 0; i < 4; i++) deck.Add(GetBase("defend"));
            deck.Add(GetBase("ember_step"));
            deck.Add(GetBase("drain"));
            return deck;
        }

        public static List<CardDef> CreateRewardPool()
        {
            return BaseCards()
                .Where(card => card.Rarity != CardRarity.Starter)
                .Select(card => card.Clone())
                .ToList();
        }

        public static CardDef GetBase(string id)
        {
            var card = BaseCards().FirstOrDefault(item => item.Id == id);
            return card != null ? card.Clone() : null;
        }

        public static string RarityLabel(CardRarity rarity)
        {
            switch (rarity)
            {
                case CardRarity.Starter:
                    return "初始";
                case CardRarity.Common:
                    return "普通";
                case CardRarity.Uncommon:
                    return "罕见";
                case CardRarity.Rare:
                    return "稀有";
                default:
                    return "未知";
            }
        }

        private static List<CardDef> BaseCards()
        {
            return new List<CardDef>
            {
                Card("strike", "刻火", "每一次挥刃，都在黑暗里留下短暂的光。", CardKind.Attack, CardRarity.Starter, 1, damage: 6),
                Card("defend", "守势", "把呼吸收进盾后，等待敌人先露出破绽。", CardKind.Skill, CardRarity.Starter, 1, block: 5),
                Card("ember_step", "余烬步", "火星掠过脚边，下一步已经抵达。", CardKind.Skill, CardRarity.Common, 1, block: 4, draw: 1),
                Card("drain", "灰烬汲取", "吞下尚未冷却的残焰。", CardKind.Attack, CardRarity.Uncommon, 1, damage: 6, heal: 2),
                Card("twin_cut", "双重刻痕", "两道伤口，共用同一个名字。", CardKind.Attack, CardRarity.Common, 1, damage: 4, hits: 2),
                Card("guard_break", "裂甲击", "先打碎护甲，再打碎沉默。", CardKind.Attack, CardRarity.Uncommon, 2, damage: 7, vulnerable: 2),
                Card("hush", "缄默印", "被夺去声音的人，也挥不动刀。", CardKind.Attack, CardRarity.Common, 1, damage: 4, weak: 2),
                Card("meditate", "静观", "只看一次。之后，世界会自己给出答案。", CardKind.Skill, CardRarity.Common, 0, draw: 2, exhaust: true),
                Card("spark", "星火飞溅", "一簇火，足以让整座洞窟睁眼。", CardKind.Attack, CardRarity.Uncommon, 1, damage: 5, damageAll: true),
                Card("counter", "反击姿态", "把迎面的力量折回骨血深处。", CardKind.Skill, CardRarity.Uncommon, 1, block: 8, strength: 1),
                Card("flame_strike", "熔火重击", "星盘倾斜时，火会从天上落下来。", CardKind.Attack, CardRarity.Uncommon, 2, damage: 16),
                Card("shield_bash", "盾击", "守到最后的人，才有资格还手。", CardKind.Attack, CardRarity.Common, 2, damage: 7, block: 7),
                Card("mend", "命线缝合", "伤口被烧焦，也就暂时不再流血。", CardKind.Skill, CardRarity.Uncommon, 1, heal: 8, exhaust: true),
                Card("meteor", "陨星", "远处的天空裂开，落下一条燃烧的河。", CardKind.Attack, CardRarity.Rare, 3, damage: 14, damageAll: true),
                Card("burning_will", "燃烧意志", "只要还愿意向前，灰烬就仍是火。", CardKind.Power, CardRarity.Rare, 2, strength: 2),
                Card("ember_armor", "烬甲", "余烬在皮肤上结成薄薄的甲胄。", CardKind.Skill, CardRarity.Uncommon, 2, block: 9, dexterity: 1),
                Card("cinder_brand", "余烬烙印", "烙印不会消失，它只会在夜里更亮。", CardKind.Attack, CardRarity.Uncommon, 1, damage: 5, burn: 2),
                Card("echo", "回响", "你听见自己的动作，先于自己发生。", CardKind.Attack, CardRarity.Rare, 1, damage: 3, hits: 3)
            };
        }

        private static CardDef Card(
            string id,
            string name,
            string flavor,
            CardKind kind,
            CardRarity rarity,
            int cost,
            int damage = 0,
            int hits = 1,
            int block = 0,
            int draw = 0,
            int heal = 0,
            int weak = 0,
            int vulnerable = 0,
            int strength = 0,
            int dexterity = 0,
            int burn = 0,
            int energy = 0,
            bool damageAll = false,
            bool exhaust = false)
        {
            return new CardDef
            {
                Id = id,
                Name = name,
                Flavor = flavor,
                Kind = kind,
                Rarity = rarity,
                Cost = cost,
                Damage = damage,
                Hits = hits,
                Block = block,
                Draw = draw,
                Heal = heal,
                Weak = weak,
                Vulnerable = vulnerable,
                Strength = strength,
                Dexterity = dexterity,
                Burn = burn,
                Energy = energy,
                DamageAll = damageAll,
                Exhaust = exhaust
            };
        }
    }

    [Serializable]
    public class EnemyMove
    {
        public string Name;
        public int Damage;
        public int Hits = 1;
        public int Block;
        public int Strength;
        public int PlayerWeak;
        public int PlayerVulnerable;
        public int Heal;

        public string Summary
        {
            get
            {
                var parts = new List<string>();
                if (Damage > 0) parts.Add(Hits > 1 ? string.Format("{0}×{1}", Damage, Hits) : Damage.ToString());
                if (Block > 0) parts.Add(string.Format("甲 {0}", Block));
                if (Strength > 0) parts.Add(string.Format("力 +{0}", Strength));
                if (PlayerWeak > 0) parts.Add(string.Format("弱 {0}", PlayerWeak));
                if (PlayerVulnerable > 0) parts.Add(string.Format("易伤 {0}", PlayerVulnerable));
                if (Heal > 0) parts.Add(string.Format("愈 {0}", Heal));
                return string.Join(" · ", parts);
            }
        }
    }

    [Serializable]
    public class EnemyDef
    {
        public string Id;
        public string Name;
        public string Subtitle;
        public string Art;
        public int MaxHp;
        public EnemyMove[] Pattern;

        public EnemyDef Clone()
        {
            var clone = (EnemyDef)MemberwiseClone();
            clone.Pattern = Pattern.Select(move => new EnemyMove
            {
                Name = move.Name,
                Damage = move.Damage,
                Hits = move.Hits,
                Block = move.Block,
                Strength = move.Strength,
                PlayerWeak = move.PlayerWeak,
                PlayerVulnerable = move.PlayerVulnerable,
                Heal = move.Heal
            }).ToArray();
            return clone;
        }
    }

    public class EncounterDef
    {
        public string Title;
        public List<EnemyDef> Enemies = new List<EnemyDef>();
        public NodeKind Kind;
    }

    public static class EnemyCatalog
    {
        public static EncounterDef BuildEncounter(NodeKind kind, int act, Random random)
        {
            if (kind == NodeKind.Boss)
            {
                return new EncounterDef
                {
                    Title = "无光星骸",
                    Kind = kind,
                    Enemies = new List<EnemyDef> { Boss().Clone() }
                };
            }

            if (kind == NodeKind.Elite)
            {
                if (act <= 0)
                {
                    var brute = Get("brute").Clone();
                    brute.MaxHp = 92;
                    return Encounter(kind, "裂盔蛮兽", brute);
                }

                if (act == 1)
                {
                    var knight = Get("knight").Clone();
                    knight.MaxHp = 132;
                    return Encounter(kind, "失落守门人", knight);
                }

                var finalKnight = Get("knight").Clone();
                finalKnight.MaxHp = 158;
                finalKnight.Name = "星骸骑士";
                finalKnight.Pattern = new[]
                {
                    Move("群星斩", damage: 14, hits: 2),
                    Move("王座之盾", block: 18, strength: 3),
                    Move("坠落礼赞", damage: 21, playerVulnerable: 2)
                };
                return Encounter(kind, "星骸骑士", finalKnight);
            }

            var roll = random.NextDouble();
            if (act <= 0)
            {
                if (roll < 0.46)
                {
                    var cultist = Get("cultist").Clone();
                    cultist.MaxHp = 42;
                    return Encounter(kind, "烬风地窟", cultist);
                }

                if (roll < 0.78)
                {
                    var slime = Get("slime").Clone();
                    slime.MaxHp = 34;
                    var spider = Get("spider").Clone();
                    spider.MaxHp = 24;
                    return Encounter(kind, "黑暗中的窸窣", slime, spider);
                }

                var demon = Get("demon").Clone();
                demon.MaxHp = 48;
                return Encounter(kind, "残翼巡游者", demon);
            }

            if (act == 1)
            {
                if (roll < 0.36)
                {
                    var cultist = Get("cultist").Clone();
                    cultist.MaxHp = 58;
                    var wraith = Get("wraith").Clone();
                    wraith.MaxHp = 45;
                    return Encounter(kind, "无声祷团", cultist, wraith);
                }

                if (roll < 0.72)
                {
                    var demon = Get("demon").Clone();
                    demon.MaxHp = 68;
                    return Encounter(kind, "熔火回廊", demon);
                }

                var knight = Get("knight").Clone();
                knight.MaxHp = 72;
                return Encounter(kind, "旧王朝的哨兵", knight);
            }

            if (roll < 0.5)
            {
                var brute = Get("brute").Clone();
                brute.MaxHp = 88;
                var demon = Get("demon").Clone();
                demon.MaxHp = 58;
                return Encounter(kind, "最后的守望", brute, demon);
            }

            var knightFinal = Get("knight").Clone();
            knightFinal.MaxHp = 96;
            var wraithFinal = Get("wraith").Clone();
            wraithFinal.MaxHp = 54;
            return Encounter(kind, "王座前厅", knightFinal, wraithFinal);
        }

        private static EncounterDef Encounter(NodeKind kind, string title, params EnemyDef[] enemies)
        {
            return new EncounterDef
            {
                Title = title,
                Kind = kind,
                Enemies = enemies.ToList()
            };
        }

        private static EnemyDef Get(string id)
        {
            switch (id)
            {
                case "slime": return Slime();
                case "spider": return Spider();
                case "cultist": return Cultist();
                case "demon": return Demon();
                case "wraith": return Wraith();
                case "brute": return Brute();
                case "knight": return Knight();
                default: throw new ArgumentOutOfRangeException("id", id, "Unknown enemy");
            }
        }

        private static EnemyMove Move(
            string name,
            int damage = 0,
            int hits = 1,
            int block = 0,
            int strength = 0,
            int playerWeak = 0,
            int playerVulnerable = 0,
            int heal = 0)
        {
            return new EnemyMove
            {
                Name = name,
                Damage = damage,
                Hits = hits,
                Block = block,
                Strength = strength,
                PlayerWeak = playerWeak,
                PlayerVulnerable = playerVulnerable,
                Heal = heal
            };
        }

        private static EnemyDef Slime()
        {
            return new EnemyDef
            {
                Id = "slime",
                Name = "灰质黏体",
                Subtitle = "从石缝里渗出的旧日残渣",
                Art = "enemy_slime",
                MaxHp = 34,
                Pattern = new[]
                {
                    Move("黏液冲撞", damage: 5),
                    Move("凝固", block: 6),
                    Move("吞没", damage: 8)
                }
            };
        }

        private static EnemyDef Spider()
        {
            return new EnemyDef
            {
                Id = "spider",
                Name = "烬网蛛",
                Subtitle = "它的网由烧焦的预言织成",
                Art = "enemy_spider",
                MaxHp = 26,
                Pattern = new[]
                {
                    Move("双螯", damage: 3, hits: 2),
                    Move("缚足", playerWeak: 1),
                    Move("毒刺", damage: 7)
                }
            };
        }

        private static EnemyDef Cultist()
        {
            return new EnemyDef
            {
                Id = "cultist",
                Name = "无面侍僧",
                Subtitle = "向熄灭的星许愿",
                Art = "enemy_cultist",
                MaxHp = 42,
                Pattern = new[]
                {
                    Move("灰烬祷言", damage: 7),
                    Move("献祭", strength: 2),
                    Move("定罪", damage: 6, playerWeak: 1)
                }
            };
        }

        private static EnemyDef Demon()
        {
            return new EnemyDef
            {
                Id = "demon",
                Name = "残翼魔",
                Subtitle = "仍记得天空，因此更加愤怒",
                Art = "enemy_demon",
                MaxHp = 50,
                Pattern = new[]
                {
                    Move("俯冲", damage: 10),
                    Move("焦土", block: 7),
                    Move("裂爪", damage: 6, hits: 2)
                }
            };
        }

        private static EnemyDef Wraith()
        {
            return new EnemyDef
            {
                Id = "wraith",
                Name = "无声亡魂",
                Subtitle = "它的名字已被火从世上抹去",
                Art = "enemy_wraith",
                MaxHp = 46,
                Pattern = new[]
                {
                    Move("寒意", damage: 8, heal: 4),
                    Move("裂隙凝视", playerVulnerable: 1),
                    Move("穿魂", damage: 12)
                }
            };
        }

        private static EnemyDef Brute()
        {
            return new EnemyDef
            {
                Id = "brute",
                Name = "裂盔蛮兽",
                Subtitle = "它把撞碎的墙当作披风",
                Art = "enemy_brute",
                MaxHp = 92,
                Pattern = new[]
                {
                    Move("重锤", damage: 14),
                    Move("咆哮", strength: 3),
                    Move("横冲", damage: 8, block: 10)
                }
            };
        }

        private static EnemyDef Knight()
        {
            return new EnemyDef
            {
                Id = "knight",
                Name = "失落守门人",
                Subtitle = "它守着一扇早已不存在的门",
                Art = "enemy_knight",
                MaxHp = 132,
                Pattern = new[]
                {
                    Move("城门斩", damage: 10, block: 10),
                    Move("双子锋", damage: 7, hits: 2),
                    Move("王者余威", strength: 4)
                }
            };
        }

        private static EnemyDef Boss()
        {
            return new EnemyDef
            {
                Id = "star_husk",
                Name = "无光星骸",
                Subtitle = "世界熄灭后，唯一还亮着的伤口",
                Art = "enemy_demon",
                MaxHp = 220,
                Pattern = new[]
                {
                    Move("群星坠落", damage: 8, hits: 3),
                    Move("熄灭之壁", block: 16, strength: 2),
                    Move("无光判决", playerVulnerable: 2, playerWeak: 2),
                    Move("最后的心跳", damage: 18, heal: 8)
                }
            };
        }
    }

    public class MapRow
    {
        public NodeKind[] Choices;
    }

    public class MapAct
    {
        public string Name;
        public string Subtitle;
        public string Roman;
        public List<MapRow> Rows;
    }

    public class RunMap
    {
        public List<MapAct> Acts = new List<MapAct>();

        public static RunMap Build()
        {
            return new RunMap
            {
                Acts = new List<MapAct>
                {
                    new MapAct
                    {
                        Name = "灰烬地窟",
                        Subtitle = "火从地底醒来",
                        Roman = "I",
                        Rows = Rows(
                            Row(NodeKind.Combat, NodeKind.Event),
                            Row(NodeKind.Combat, NodeKind.Rest),
                            Row(NodeKind.Elite),
                            Row(NodeKind.Rest, NodeKind.Event))
                    },
                    new MapAct
                    {
                        Name = "无声回廊",
                        Subtitle = "所有誓言都被吞没",
                        Roman = "II",
                        Rows = Rows(
                            Row(NodeKind.Combat, NodeKind.Event),
                            Row(NodeKind.Combat, NodeKind.Shop),
                            Row(NodeKind.Elite),
                            Row(NodeKind.Rest, NodeKind.Event))
                    },
                    new MapAct
                    {
                        Name = "熄灭王座",
                        Subtitle = "尽头没有王，只有伤口",
                        Roman = "III",
                        Rows = Rows(
                            Row(NodeKind.Combat, NodeKind.Event),
                            Row(NodeKind.Elite),
                            Row(NodeKind.Rest, NodeKind.Combat),
                            Row(NodeKind.Boss))
                    }
                }
            };
        }

        private static MapRow Row(params NodeKind[] choices)
        {
            return new MapRow { Choices = choices };
        }

        private static List<MapRow> Rows(params MapRow[] rows)
        {
            return rows.ToList();
        }
    }

    public class RunState
    {
        public int MaxHp = 72;
        public int Hp = 72;
        public int Gold = 80;
        public int Act;
        public int Row;
        public int ChosenNode = -1;
        public int LastAct = -1;
        public int LastRow = -1;
        public List<CardDef> Deck = new List<CardDef>();
        public List<string> Relics = new List<string>();
        public List<List<int>> Choices = new List<List<int>>();
        public RunMap Map;
        public float StartedAt;
    }

    public class EnemyCombatant
    {
        public EnemyDef Def;
        public int Hp;
        public int Block;
        public int Weak;
        public int Vulnerable;
        public int Strength;
        public int Burn;
        public int PatternIndex;
        public bool IsDead { get { return Hp <= 0; } }

        public EnemyMove CurrentMove
        {
            get
            {
                if (Def.Pattern == null || Def.Pattern.Length == 0) return new EnemyMove { Name = "等待" };
                return Def.Pattern[PatternIndex % Def.Pattern.Length];
            }
        }

        public static EnemyCombatant FromDef(EnemyDef def)
        {
            return new EnemyCombatant
            {
                Def = def,
                Hp = def.MaxHp,
                PatternIndex = 0
            };
        }
    }

    public static class NodeInfo
    {
        public static string Label(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Combat: return "战斗";
                case NodeKind.Elite: return "精英";
                case NodeKind.Rest: return "营火";
                case NodeKind.Shop: return "商店";
                case NodeKind.Event: return "异闻";
                case NodeKind.Boss: return "首领";
                default: return "未知";
            }
        }

        public static string Description(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Combat: return "敌人挡住了去路";
                case NodeKind.Elite: return "危险，但会留下遗物";
                case NodeKind.Rest: return "恢复生命或锻造卡牌";
                case NodeKind.Shop: return "用金币换取力量";
                case NodeKind.Event: return "选择会改变命运";
                case NodeKind.Boss: return "这一幕的最后一道门";
                default: return string.Empty;
            }
        }

        public static string Icon(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Combat: return "icon_sword";
                case NodeKind.Elite: return "enemy_knight";
                case NodeKind.Rest: return "icon_fire";
                case NodeKind.Shop: return "icon_potion";
                case NodeKind.Event: return "icon_rune";
                case NodeKind.Boss: return "enemy_demon";
                default: return "icon_rune";
            }
        }
    }
}
