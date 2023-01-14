// Critically damped spring based transition
// As it takes time to get to speed it removed noise for rapidly swithing targets
namespace Noodles
{
    public struct FloatSpring
    {
        public float pos;
        public float vel;

        public FloatSpring(float pos)
        {
            this.pos = pos;
            this.vel = 0;
        }
        public void Set(float pos)
        {
            this.pos = pos;
            this.vel = 0;
        }

        public float Step(float target, float period, float dt)
        {
            if (dt == 0) return pos;
            // calculate critically damped spring rates for given period
            var mass = 1f;
            var freq = 1 / period;
            var k = mass * freq * freq;
            var d = 2 * mass * freq;
            d *= .7f;

            // calculate soft constraint values
            var gamma = 1 / (dt * (d + dt * k));
            var beta = dt * k * gamma;
            var sofmass = 1 / (1 / mass + gamma);

            // use implicit euler to calculate impulse, velocity and position
            var C = pos - target;
            if (C * vel > 0) vel = 0; // stop on opposite sign
            var impulse = -sofmass * (vel + beta * C);
            var dv = impulse / mass;
            vel += dv;
            pos += vel * dt;
            return pos;
        }

    }
}