using System;

namespace DefenseShields.Support
{
    public class EllipsoidSA
    {
        private double _a;
        private double _b;
        private double _c;

        public EllipsoidSA(double a, double b, double c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public void Update(double a, double b, double c)
        {
            _a = a;
            _b = b;
            _c = c;
        }

        public double Volume
        {
            get
            {
                return (4 / 3 * 3.14 * _a * _b * _c);
            }
        }

        public double Surface
        {
            get { return (4 * Math.PI * Math.Pow(((Math.Pow(_a * _b, 1.6) + Math.Pow(_a * _c, 1.6) + Math.Pow(_b * _c, 1.6)) / 3), 1 / 1.6)); }
        }
    }
}