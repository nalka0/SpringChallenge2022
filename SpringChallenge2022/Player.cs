using System.Collections.Generic;
using System.Drawing;

namespace SpringChallenge2022
{
    public class Player<THero> where THero : Hero
    {
        public int Mana { get; set; }

        public int Health { get; set; }

        public Point BaseCoords { get; set; }

        public List<THero> Heroes { get; set; }

        public Player()
        {
            Heroes = new List<THero>();
        }
    }
}
