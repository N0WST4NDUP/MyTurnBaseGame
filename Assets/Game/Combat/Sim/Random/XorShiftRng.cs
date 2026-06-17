namespace MyTurnBase.Combat.Sim
{
    public sealed class XorShiftRng : IRng
    {
        uint _s;

        public XorShiftRng(int seed)
        {
            unchecked
            {
                _s = (uint)seed;
                if (_s == 0)
                {
                    _s = 0x9E3779B9u;
                }
            }
        }

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new System.ArgumentOutOfRangeException(nameof(maxExclusive), "must be > 0");

            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;

            return (int)(_s % (uint)maxExclusive);
        }
    }
}
