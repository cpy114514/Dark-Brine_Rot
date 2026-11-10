namespace Mavis
{
    /// <summary>Converts authored world distances to metres using Sahur's standing height.</summary>
    public static class WorldMeasurements
    {
        public const float SahurHeightMetres = 2f;
        // The production character stands about 5.3 world units tall at its authored scale.
        // This fixed reference also applies while he is swimming, airborne or temporarily small.
        public const float SahurStandingHeightWorldUnits = 5.3f;

        public static float ToMetres(float worldUnits) =>
            worldUnits * (SahurHeightMetres / SahurStandingHeightWorldUnits);
    }
}
