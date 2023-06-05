using System;
using System.Collections.Generic;
using System.Drawing;

namespace SpringChallenge2022;
public static class Game
{
    public static Point VerticalCorner { get; set; }

    public static bool IsTopLeft { get; set; }

    public static Player<FriendlyHero> Me { get; set; }

    public static Player<EnnemyHero> Ennemy { get; set; }

    public static int CurrentTurn { get; set; }

    public static bool EnnemyHeroInSight { get; set; }

    public static int TurnsLeft => 220 - CurrentTurn;

    public static readonly int PredictedTurns = 6;

    public static void Main()
    {
        Ennemy = new Player<EnnemyHero>();
        Me = new Player<FriendlyHero>();
        string[] inputs = Console.ReadLine().Split(' ');
        Console.ReadLine(); // Always 3 (number of heroes)
        Me.BaseCoords = new Point(int.Parse(inputs[0]), int.Parse(inputs[1]));
        VerticalCorner = new Point(Me.BaseCoords.X, IsTopLeft ? 9000 : 0);
        IsTopLeft = Me.BaseCoords.X == 0;
        Ennemy.BaseCoords = IsTopLeft ? new Point(17630, 9000) : new Point(0, 0);
        CurrentTurn = 1;
        while (true)
        {
            EnnemyHeroInSight = false;
            List<Monster> monsters = new();
            Me.Heroes.Clear();
            Ennemy.Heroes.Clear();
            inputs = Console.ReadLine().Split(' ');
            Me.Health = int.Parse(inputs[0]);
            Me.Mana = int.Parse(inputs[1]);
            inputs = Console.ReadLine().Split(' ');
            Ennemy.Health = int.Parse(inputs[0]);
            Ennemy.Mana = int.Parse(inputs[1]);
            int entityCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < entityCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                bool nearBase = int.Parse(inputs[9]) == 1;
                int id = int.Parse(inputs[0]);
                int type = int.Parse(inputs[1]);
                Point position = new(int.Parse(inputs[2]), int.Parse(inputs[3]));
                int shieldDuration = int.Parse(inputs[4]);
                bool isControlled = int.Parse(inputs[5]) == 1;
                if (type == 0)
                {
                    Threat threat = int.Parse(inputs[10]) switch
                    {
                        0 => Threat.None,
                        1 => Threat.Me,
                        2 => Threat.Ennemy,
                        _ => throw new NotSupportedException("Not supported")
                    };
                    Point speed = new(int.Parse(inputs[7]), int.Parse(inputs[8]));
                    monsters.Add(new Monster
                    {
                        Id = id,
                        Position = position,
                        Health = int.Parse(inputs[6]),
                        IsNearBase = nearBase,
                        ThreatFor = threat,
                        IsControlled = isControlled,
                        ShieldDuration = shieldDuration,
                        Speed = speed,
                        AngleToBase = (Helpers.AngleFromPoint(Me.BaseCoords, position) + (IsTopLeft ? 0 : 180)) % 360
                    });
                }
                else if (type == 1)
                {
                    Me.Heroes.Add(new BorderDefendingHero(Me.Heroes.Count)
                    {
                        Id = id,
                        Position = position,
                        IsControlled = isControlled,
                        ShieldDuration = shieldDuration,
                        DistanceToBase = Helpers.GetCircleRadius(position, Me.BaseCoords),
                    });
                }
                else
                {
                    EnnemyHeroInSight = true;
                    Ennemy.Heroes.Add(new EnnemyHero
                    {
                        Id = id,
                        Position = position,
                        IsControlled = isControlled,
                        ShieldDuration = shieldDuration,
                        DistanceToBase = Helpers.GetCircleRadius(Me.BaseCoords, position),
                        AngleToBase = (Helpers.AngleFromPoint(Me.BaseCoords, position) + (IsTopLeft ? 0 : 180)) % 360
                    });
                }
            }
            foreach (FriendlyHero hero in Me.Heroes)
            {
                if (hero is BorderDefendingHero borderDefendingHero)
                {
                    borderDefendingHero.Prepare();
                }
            }
            foreach (Monster monster in monsters)
            {
                monster.ComputeNextPositions(PredictedTurns);
            }

            for (int i = 0; i < Me.Heroes.Count; i++)
            {
                FriendlyHero hero = Me.Heroes[i];
                if (hero is BorderDefendingHero borderDef)
                {
                    foreach (Monster monster in monsters)
                    {
                        if (monster.IsDangerous ||
                            (Helpers.IsInRange(monster.Position, borderDef.RallyPoint, 4000) &&
                            (borderDef.Angle != 45 || Math.Abs(monster.AngleToBase - 45) <= 20) &&
                            (!borderDef.PlayCarefully || Math.Abs(monster.AngleToBase - borderDef.Angle) <= 15)))
                        {
                            borderDef.ConsideredMonsters.Add(monster);
                        }
                    }
                }

                hero.Play();
            }

            CurrentTurn++;
        }
    }
}